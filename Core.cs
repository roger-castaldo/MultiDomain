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
        private static Dictionary<sRoute,List<Core>> _subRoutes;
        private static List<sRoute> _myRoutes;

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
            Init();
            foreach (object obj in assemblies)
            {
                Assembly ass = null;
                if (obj is string)
                {
                    if (!((string)obj).EndsWith(".config"))
                    {
                        if (new FileInfo((string)obj).Exists)
                            ass = Assembly.LoadFile((string)obj);
                        else
                            ass = Assembly.Load((string)obj);
                    }
                }
                else if (obj is byte[])
                    ass = Assembly.Load((byte[])obj);
                else
                    throw new Exception("Unable to load assembly in new domain unless it is a string or raw byte data.");
                if (ass!=null)
                    _loadedAssemblies.Add(ass.FullName, ass);
            }
        }

        public Core() {
        }

        internal void Init()
        {
            if (_eventController == null)
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                _subRoutes = new Dictionary<sRoute, List<Core>>();
                _myRoutes = new List<sRoute>();
                _eventController = new EventController();
                _loadedAssemblies = new Dictionary<string, Assembly>();
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

        internal void _RegisterRoute(sRoute route,Core core)
        {
            lock (_subRoutes)
            {
                List<Core> cores = new List<Core>();
                if (_subRoutes.ContainsKey(route))
                {
                    cores = _subRoutes[route];
                    _subRoutes.Remove(route);
                }
                cores.Add(core);
                _subRoutes.Add(route, cores);
            }
            if (_parent != null)
                _parent._RegisterRoute(route, this);
        }

        internal void _UnRegisterRoute(sRoute route, Core core)
        {
            lock (_subRoutes)
            {
                List<Core> cores = new List<Core>();
                if (_subRoutes.ContainsKey(route))
                {
                    cores = _subRoutes[route];
                    _subRoutes.Remove(route);
                }
                cores.Remove(core);
                _subRoutes.Add(route, cores);
            }
            if (_parent != null)
                _parent._UnRegisterRoute(route, this);
        }

        internal void _RegsiterMessageHandlerRoute(sRoute[] routes)
        {
            lock (_myRoutes)
            {
                _myRoutes.AddRange(routes);
            }
            if (_parent != null)
            {
                foreach (sRoute srt in routes)
                    _parent._RegisterRoute(srt, this);
            }
        }

        internal void _UnRegsiterMessageHandlerRoute(sRoute[] routes)
        {
            lock (_myRoutes)
            {
                foreach (sRoute srt in routes)
                    _myRoutes.Remove(srt);
            }
            if (_parent != null)
            {
                foreach (sRoute srt in routes)
                    _parent._UnRegisterRoute(srt, this);
            }
        }

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
        public void EstablishParent(Core parent) { 
            _parent=parent;
            if (_myRoutes != null)
            {
                lock (_myRoutes)
                {
                    foreach (sRoute srt in _myRoutes)
                        _parent._RegisterRoute(srt, this);
                }
            }
        }
        #endregion
        #region InterDomainMessages
        public bool HandlesMessage(IInterDomainMessage message)
        {
            RoutedInterDomainMessage ridm = (RoutedInterDomainMessage)message;
            bool ret = false;
            lock (_myRoutes)
            {
                foreach (sRoute srt in ridm.HandlerRoutes)
                {
                    if (_myRoutes.Contains(srt))
                    {
                        ret = true;
                        break;
                    }
                }
            }
            if (!ret)
            {
                lock (_subRoutes)
                {
                    foreach (sRoute srt in ridm.HandlerRoutes)
                    {
                        if (_subRoutes.ContainsKey(srt))
                        {
                            ret = true;
                            break;
                        }
                    }
                }
            }
            return ret;
        }
        public IInterDomainMessage InterceptMessage(IInterDomainMessage message)
        {
            IInterDomainMessage ret = message;
            RoutedInterDomainMessage ridm = (RoutedInterDomainMessage)message;
            List<Core> subCores = new List<Core>();
            lock (_myRoutes)
            {
                foreach (sRoute srt in ridm.PreInterceptRoutes)
                {
                    if (_myRoutes.Contains(srt))
                    {
                        if (_messageController.InterceptsMessage(ridm))
                            subCores.Add(this);
                        break;
                    }
                }
            }
            lock (_subRoutes)
            {
                foreach (sRoute srt in ridm.PreInterceptRoutes)
                {
                    if (_subRoutes.ContainsKey(srt))
                    {
                        foreach (Core cr in _subRoutes[srt])
                        {
                            if (!subCores.Contains(cr))
                                subCores.Add(cr);
                        }
                    }
                }
            }
            foreach (Core cr in subCores)
            {
                try
                {
                    ret = cr.InterceptMessage(ret);
                }
                catch (Exception e) { }
            }
            return ret;
        }

        public InterDomainMessageResponse ProcessMessage(IInterDomainMessage message)
        {
            InterDomainMessageResponse ret = null;
            RoutedInterDomainMessage ridm = (RoutedInterDomainMessage)message;
            lock (_myRoutes)
            {
                foreach (sRoute srt in ridm.HandlerRoutes)
                {
                    if (_myRoutes.Contains(srt))
                    {
                        if (_messageController.HandlesMessage(ridm))
                        {
                            ret = new InterDomainMessageResponse(ridm, _messageController.ProcessMessage(ridm));
                            break;
                        }
                    }
                }
            }
            if (ret == null)
            {
                lock (_subRoutes)
                {
                    foreach (sRoute srt in ridm.HandlerRoutes)
                    {
                        if (_subRoutes.ContainsKey(srt))
                        {
                            foreach (Core cr in _subRoutes[srt])
                            {
                                if (cr.HandlesMessage(ridm))
                                {
                                    ret = cr.ProcessMessage(ridm);
                                    break;
                                }
                            }
                            if (ret != null)
                                break;

                        }
                    }
                }
            }
            return ret;
        }

        public void InterceptResponse(ref InterDomainMessageResponse response)
        {
            RoutedInterDomainMessage ridm = (RoutedInterDomainMessage)response.Message;
            lock (_myRoutes)
            {
                foreach (sRoute srt in ridm.PostInterceptRoutes)
                {
                    if (_myRoutes.Contains(srt))
                    {
                        if (_messageController.InterceptsResponse(response))
                            _messageController.InterceptResponse(ref response);
                        break;
                    }
                }
            }
            List<Core> subCores = new List<Core>();
            lock (_subRoutes)
            {
                foreach (sRoute srt in ridm.PostInterceptRoutes)
                {
                    if (_subRoutes.ContainsKey(srt))
                    {
                        foreach (Core cr in _subRoutes[srt])
                        {
                            if (!subCores.Contains(cr))
                                subCores.Add(cr);
                        }
                    }
                }
                foreach (Core cr in subCores)
                    cr.InterceptResponse(ref response);
            }
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
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
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
            {
                try
                {
                    start.Start();
                }
                catch (Exception e) { System.Error(e); throw e; }
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            System.Error((Exception)e.ExceptionObject);
            throw (Exception)e.ExceptionObject;
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
