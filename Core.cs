using Org.Reddragonit.MultiDomain.Controllers;
using Org.Reddragonit.MultiDomain.Interfaces;
using Org.Reddragonit.MultiDomain.Interfaces.EventSystem;
using Org.Reddragonit.MultiDomain.Interfaces.Logging;
using Org.Reddragonit.MultiDomain.Interfaces.Messaging;
using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Org.Reddragonit.MultiDomain
{
    internal class Core : MarshalByRefObject
    {
        private static Dictionary<string, Assembly> _loadedAssemblies;
        private static EventController _eventController;
        private static MessageController _messageController;
        private static LogController _logController;
        private static Core _parent;
        private static delProcessEvent _processEvent;
        private static delProcessEvent _processEventInChildren;
        private Core Parent { get { return _parent; } }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public Core AbsoluteParent
        {
            get
            {
                Core ret = this;
                while (ret.HasParent)
                    ret = ret.Parent;
                return ret;
            }
        }

        public void LoadAssemblies(object[] assemblies)
        {
            foreach (object obj in assemblies)
            {
                Assembly ass = null;
                if (obj is string)
                {
                    if (new FileInfo((string)obj).Exists)
                        ass = Assembly.LoadFile((string)obj);
                    else
                        ass = Assembly.Load((string)obj);
                }
                else if (obj is byte[])
                    ass = Assembly.Load((byte[])obj);
                else
                    throw new Exception("Unable to load assembly in new domain unless it is a string or raw byte data.");
                _loadedAssemblies.Add(ass.FullName, ass);
            }
        }

        public Core() {
            if (_eventController == null)
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                _loadedAssemblies = new Dictionary<string, Assembly>();
                _eventController = new EventController();
                _processEvent = new delProcessEvent(_eventController.ProcessEvent);
                _processEventInChildren = new delProcessEvent(_ProcessEventInChildren);
                _logController = new LogController();
                _logController.Start();
                _messageController = new MessageController();
                _messageController.Start();
            }
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            System.Debug("Attempting to resolve assembly {0} in Domain {1}", args.Name, AppDomain.CurrentDomain.FriendlyName);
            return (_loadedAssemblies.ContainsKey(args.Name) ? _loadedAssemblies[args.Name] : null);
        }

        public bool HasParent { get { return _parent != null; } }

        #region Events
        public string RegisterHandler(IEventHandler handler) { return _eventController.RegisterHandler(handler); }
        public void UnRegistereventHandler(string id) { _eventController.UnRegistereventHandler(id); }
        public void ProcessEvent(Messages.Event Event) { 
            if (Event.IsSynchronous)
            {
                _processEvent.Invoke(Event);
                _processEventInChildren.Invoke(Event);
            }
            else
            {
                _processEvent.BeginInvoke(Event, null, null);
                _processEventInChildren.BeginInvoke(Event, null, null);
            }
        }
        private static void _ProcessEventInChildren(Messages.Event Event)
        {
            System.sDomain[] doms = System.Domains;
            foreach (System.sDomain dom in doms)
                dom.ProcessEvent(Event);
        }
        public void EstablishParent(Core parent) { _parent=parent; }
        #endregion
        #region InterDomainMessages
        public bool HandlesMessage(IInterDomainMessage message)
        {
            if (_messageController.HandlesMessage(message))
                return true;
            System.sDomain[] doms = System.Domains;
            foreach (System.sDomain dom in doms)
            {
                if (dom.Core.HandlesMessage(message))
                    return true;
            }
            return false;
        }
        public IInterDomainMessage InterceptMessage(IInterDomainMessage message)
        {
            IInterDomainMessage ret = message;
            if (_messageController.InterceptsMessage(message))
                ret = _messageController.InterceptMessage(message);
            System.sDomain[] doms = System.Domains;
            foreach (System.sDomain dom in doms)
                ret = dom.Core.InterceptMessage(message);
            if (!ret.GetType().IsMarshalByRef)
                ret = (ret is ISecuredInterDomainMessage ? new SecurredWrapperInterDomainMessage((ISecuredInterDomainMessage)ret) : new WrapperInterDomainMessage(ret));
            return ret;
        }
        public InterDomainMessageResponse ProcessMessage(IInterDomainMessage message)
        {
            if (_messageController.HandlesMessage(message))
                return new InterDomainMessageResponse(message,_messageController.ProcessMessage(message));
            System.sDomain[] doms = System.Domains;
            foreach (System.sDomain dom in doms)
            {
                if (dom.Core.HandlesMessage(message))
                    return dom.Core.ProcessMessage(message);
            }
            return null;
        }
        public void InterceptResponse(ref InterDomainMessageResponse response)
        {
            if (_messageController.InterceptsResponse(response))
                _messageController.InterceptResponse(ref response);
            System.sDomain[] doms = System.Domains;
            foreach (System.sDomain dom in doms)
                dom.Core.InterceptResponse(ref response);
        }
        #endregion
        #region Logging
        public void AppendEntry(string sourceDomainName, global::System.Reflection.AssemblyName sourceAssembly, string sourceTypeName, string sourceMethodName, LogLevels level, DateTime timestamp, string message) { 
            _logController.AppendEntry(sourceDomainName, sourceAssembly, sourceTypeName, sourceMethodName, level, timestamp, message); 
            System.sDomain[] doms = System.Domains;
            foreach (System.sDomain dom in doms)
                dom.Core.AppendEntry(sourceDomainName, sourceAssembly, sourceTypeName, sourceMethodName, level, timestamp, message);
        }
        #endregion

        public void Startup()
        {
            List<string> loadedTypes = new List<string>();
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            Type parent = typeof(IStartup);
            List<IStartup> starts = new List<IStartup>();
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                    {
                        foreach (Type t in ass.GetTypes())
                        {
                            if (!t.IsInterface)
                            {
                                if (new List<Type>(t.GetInterfaces()).Contains(parent))
                                {
                                    if (!loadedTypes.Contains(t.FullName))
                                    {
                                        if (t.FullName == typeof(MessageController).FullName)
                                            _messageController = (MessageController)Activator.CreateInstance(t);
                                        else if (t.FullName == typeof(LogController).FullName)
                                            _logController = (LogController)Activator.CreateInstance(t);
                                        else
                                            starts.Add((IStartup)Activator.CreateInstance(t));
                                        loadedTypes.Add(t.FullName);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.Message != "The invoked member is not supported in a dynamic assembly."
                        && e.Message != "Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.")
                    {
                        throw e;
                    }
                }
            }
            if (_logController != null)
                _logController.Start();
            if (_messageController != null)
                _messageController.Start();
            foreach (IStartup start in starts)
                start.Start();
        }

        void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            Shutdown();
        }

        public void Shutdown()
        {
            System.sDomain[] doms = System.Domains;
            foreach (System.sDomain dom in doms)
            {
                try { AppDomain.Unload(dom.Domain); }
                catch (Exception e) { }
            }
            Type parent = typeof(IShutdown);
            List<IShutdown> stops = new List<IShutdown>();
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                    {
                        foreach (Type t in ass.GetTypes())
                        {
                            if (!t.IsInterface)
                            {
                                if (new List<Type>(t.GetInterfaces()).Contains(parent))
                                    stops.Add((IShutdown)Activator.CreateInstance(t));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.Message != "The invoked member is not supported in a dynamic assembly."
                        && e.Message != "Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.")
                    {
                        throw e;
                    }
                }
            }
            foreach (IShutdown stop in stops)
                stop.Shutdown();
        }
    }
}
