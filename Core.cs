using Org.Reddragonit.MultiDomain.Controllers;
using Org.Reddragonit.MultiDomain.Interfaces;
using Org.Reddragonit.MultiDomain.Interfaces.EventSystem;
using Org.Reddragonit.MultiDomain.Interfaces.Logging;
using Org.Reddragonit.MultiDomain.Interfaces.Messaging;
using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Org.Reddragonit.MultiDomain
{
    internal class Core : MarshalByRefObject
    {
        private static EventController _eventController;
        private static MessageController _messageController;
        private static LogController _logController;
        private static Core _parent;
        private static delProcessEvent _processEvent;
        private static delProcessEvent _processEventInChildren;
        private Core Parent { get { return _parent; } }

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

        public void LoadAssemblyFromFile(string path)
        {
            try { Assembly.LoadFile(path); }
            catch (Exception e) { System.Error(e); throw e; }
        }

        public Core() {
            if (_eventController == null)
            {
                _eventController = new EventController();
                _processEvent = new delProcessEvent(_eventController.ProcessEvent);
                _processEventInChildren = new delProcessEvent(_ProcessEventInChildren);
                _logController = new LogController();
                _logController.Start();
                _messageController = new MessageController();
                _messageController.Start();
            }
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
                                    starts.Add((IStartup)Activator.CreateInstance(t));
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
            foreach (IStartup start in starts)
            {
                start.Start();
                if (start is MessageController)
                    _messageController = (MessageController)start;
                else if (start is LogController)
                    _logController = (LogController)start;
            }
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
