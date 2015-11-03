using Org.Reddragonit.MultiDomain.Interfaces.Messaging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Messages
{
    public class InterDomainMessageResponse : MarshalByRefObject
    {
        private InterDomainMessageResponse _parent;

        private IInterDomainMessage _message;
        public IInterDomainMessage Message { get { return (_parent==null ? _message : _parent.Message); } }

        private object _response;
        public object Response { get { return _response; } }

        private List<sInterceptor> _interceptors;
        public sInterceptor[] Interceptors { 
            get {
                List<sInterceptor> tmp = new List<sInterceptor>();
                if (_parent != null)
                    tmp.AddRange(_parent.Interceptors);
                tmp.AddRange(_interceptors.ToArray());
                return tmp.ToArray(); 
            } 
        }

        public PropertyInfo[] ResponseObjectProperties
        {
            get
            {
                List<PropertyInfo> ret = new List<PropertyInfo>();
                if (_parent != null)
                    ret.AddRange(_parent.ResponseObjectProperties);
                foreach (PropertyInfo pi in _response.GetType().GetProperties(BindingFlags.DeclaredOnly|BindingFlags.Public|BindingFlags.Instance))
                    ret.Add(pi);
                return ret.ToArray();
            }
        }

        public MethodInfo[] ResponseObjectMethods
        {
            get
            {
                List<MethodInfo> ret = new List<MethodInfo>();
                if (_parent != null)
                    ret.AddRange(_parent.ResponseObjectMethods);
                foreach (MethodInfo mi in _response.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
                    ret.Add(mi);
                return ret.ToArray();
            }
        }

        private bool HandlesProperty(string name)
        {
            foreach (PropertyInfo pi in _response.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (pi.Name == name)
                    return true;
            }
            return false;
        }

        public object this[string name] { 
            get { 
                if (HandlesProperty(name))
                    return this[_response.GetType().GetProperty(name)];
                return (_parent == null ? null : _parent[name]);
            } 
            set {
                if (HandlesProperty(name))
                    this[_response.GetType().GetProperty(name)] = value;
                if (_parent != null)
                    _parent[name] = value;
            } 
        }

        public object this[PropertyInfo prop] { 
            get { 
                if (HandlesProperty(prop.Name))
                    return prop.GetValue(_response, new object[0]);
                return (_parent == null ? null : _parent[prop]);
            } 
            set {
                if (HandlesProperty(prop.Name))
                    prop.SetValue(_response, value, new object[0]);
                if (_parent != null)
                    _parent[prop] = value;
            } 
        }

        private bool HandlesMethod(MethodInfo method)
        {
            foreach (MethodInfo mi in _response.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (mi.Equals(method))
                    return true;
            }
            return false;
        }

        public object InvokeMethod(MethodInfo mi, object[] parameters) { 
            if (HandlesMethod(mi))
                return mi.Invoke(_response, parameters);
            return (_parent == null ? null : _parent.InvokeMethod(mi, parameters));
        }

        internal InterDomainMessageResponse(IInterDomainMessage message, object response)
        {
            _message = message;
            _response = response;
            _interceptors = new List<sInterceptor>();
        }

        internal InterDomainMessageResponse(InterDomainMessageResponse response, object result)
        {
            _parent = response;
            _response = result;
            _interceptors = new List<sInterceptor>();
        }

        internal void MarkInterceptor(Type type)
        {
            _interceptors.Add(new sInterceptor(AppDomain.CurrentDomain.FriendlyName, type));
        }

        internal static InterDomainMessageResponse SwapResponse(InterDomainMessageResponse response, object result)
        {
            InterDomainMessageResponse ret = new InterDomainMessageResponse(response, result);
            return ret;
        }
    }
}
