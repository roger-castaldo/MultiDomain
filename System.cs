using Org.Reddragonit.MultiDomain.Controllers;
using Org.Reddragonit.MultiDomain.Interfaces;
using Org.Reddragonit.MultiDomain.Interfaces.EventSystem;
using Org.Reddragonit.MultiDomain.Interfaces.Logging;
using Org.Reddragonit.MultiDomain.Interfaces.Messaging;
using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Org.Reddragonit.MultiDomain
{
    public static class System
    {
        private delegate void delAppendEntry(StackFrame sf,LogLevels level, string message);

        internal struct sDomain
        {
            private AppDomain _domain;
            public AppDomain Domain { get { return _domain; } }
            private Core _core;
            public Core Core { get { return _core; } }
            private delProcessEvent _processEvent;

            public void ProcessEvent(Messages.Event Event)
            {
                if (Event.IsSynchronous)
                    _processEvent.Invoke(Event);
                else
                    _processEvent.BeginInvoke(Event, null, null);
            }


            public sDomain(string name)
            {
                _domain = AppDomain.CreateDomain((name == null ? System._rand.NextString(32) : name));
                _domain.Load(typeof(System).Assembly.FullName);
                _core = (Core)_domain.CreateInstanceAndUnwrap(typeof(System).Assembly.FullName, typeof(Core).FullName);
                _processEvent = new delProcessEvent(_core.ProcessEvent);
            }

        }

        private static Core _core;
        private static Mutex _mut;
        private static MT19937 _rand;
        private static delAppendEntry _appendEntry;
        private static List<sDomain> _domains;
        internal static sDomain[] Domains
        {
            get
            {
                _mut.WaitOne();
                sDomain[] ret = new sDomain[_domains.Count];
                _domains.CopyTo(ret);
                _mut.ReleaseMutex();
                return ret;
            }
        }

        static System()
        {
            _core = new Core();
            _mut = new Mutex(false);
            _rand = new MT19937(DateTime.Now.Ticks);
            _domains = new List<sDomain>();
            _appendEntry = new delAppendEntry(_AppendEntry);
        }

        #region Events
        public static string RegisterHandler(IEventHandler handler) { return _core.RegisterHandler(handler); }
        public static void UnRegisterEventHandler(string id) { _core.UnRegistereventHandler(id); }
        public static void TriggerEvent(IEvent Event) { 
            Messages.Event evnt = new Messages.Event(Event);
            _core.AbsoluteParent.ProcessEvent(evnt);
        }
        #endregion
        #region InterDomainMessages
        public static InterDomainMessageResponse ProcessInterDomainMessage(IInterDomainMessage message)
        {
            if (!message.GetType().IsMarshalByRef)
                message = (message is ISecuredInterDomainMessage ? new SecurredWrapperInterDomainMessage((ISecuredInterDomainMessage)message) : new WrapperInterDomainMessage(message));
            message = _core.AbsoluteParent.InterceptMessage(message);
            InterDomainMessageResponse ret = _core.AbsoluteParent.ProcessMessage(message);
            if (ret!=null)
                _core.AbsoluteParent.InterceptResponse(ref ret);
            return ret;
        }
        #endregion
        #region Logging
        public static void Error(Exception e)
        {
            StackFrame sf = new StackFrame(1);
            AssemblyName assName = new AssemblyName(sf.GetMethod().DeclaringType.Assembly.FullName);
            MethodBase method = sf.GetMethod();
            Exception ex = e;
            while (ex != null)
            {

                _AppendEntry(LogLevels.Error, ex.Message);
                _AppendEntry(LogLevels.Error, ex.StackTrace);
                ex = ex.InnerException;
            }
        }

        public static void Error(string message)
        {
            _AppendEntry(LogLevels.Error, message);
        }

        public static void Error(string message, object arg0)
        {
            _AppendEntry(LogLevels.Error, string.Format(message, arg0));
        }

        public static void Error(string message, object arg0, object arg1)
        {
            _AppendEntry(LogLevels.Error, string.Format(message, arg0, arg1));
        }

        public static void Error(string message, object[] args)
        {
            _AppendEntry(LogLevels.Error, string.Format(message, args));
        }

        public static void Fatal(string message)
        {
            _AppendEntry(LogLevels.Fatal, message);
        }

        public static void Fatal(string message, object arg0)
        {
            _AppendEntry(LogLevels.Fatal, string.Format(message, arg0));
        }

        public static void Fatal(string message, object arg0, object arg1)
        {
            _AppendEntry(LogLevels.Fatal, string.Format(message, arg0, arg1));
        }

        public static void Fatal(string message, object[] args)
        {
            _AppendEntry(LogLevels.Fatal, string.Format(message, args));
        }

        public static void Warn(string message)
        {
            _AppendEntry(LogLevels.Warn, message);
        }

        public static void Warn(string message, object arg0)
        {
            _AppendEntry(LogLevels.Warn, string.Format(message, arg0));
        }

        public static void Warn(string message, object arg0, object arg1)
        {
            _AppendEntry(LogLevels.Warn, string.Format(message, arg0, arg1));
        }

        public static void Warn(string message, object[] args)
        {
            _AppendEntry(LogLevels.Warn, string.Format(message, args));
        }

        public static void Info(string message)
        {
            _AppendEntry(LogLevels.Info, message);
        }

        public static void Info(string message, object arg0)
        {
            _AppendEntry(LogLevels.Info, string.Format(message, arg0));
        }

        public static void Info(string message, object arg0, object arg1)
        {
            _AppendEntry(LogLevels.Info, string.Format(message, arg0, arg1));
        }

        public static void Info(string message, object[] args)
        {
            _AppendEntry(LogLevels.Info, string.Format(message, args));
        }

        public static void Debug(string message)
        {
            _AppendEntry(LogLevels.Debug, message);
        }

        public static void Debug(string message, object arg0)
        {
            _AppendEntry(LogLevels.Debug, string.Format(message, arg0));
        }

        public static void Debug(string message, object arg0, object arg1)
        {
            _AppendEntry(LogLevels.Debug, string.Format(message, arg0, arg1));
        }

        public static void Debug(string message, object[] args)
        {
            _AppendEntry(LogLevels.Debug, string.Format(message, args));
        }

        private static void _AppendEntry(LogLevels level, string message)
        {
            int index = 0;
            StackFrame sf = new StackFrame(index);
            while (sf.GetMethod().DeclaringType.FullName == typeof(System).FullName)
            {
                index++;
                sf = new StackFrame(index);
            }
            _appendEntry.BeginInvoke(sf, level, message, null, null);
        }

        private static void _AppendEntry(StackFrame sf, LogLevels level, string message)
        {
            MethodBase method = sf.GetMethod();
            _core.AbsoluteParent.AppendEntry(AppDomain.CurrentDomain.FriendlyName, new AssemblyName(method.DeclaringType.Assembly.FullName), method.DeclaringType.FullName, method.Name, level, DateTime.Now, message);
        }
        #endregion

        public static bool ProduceNewDomain(object[] assemblies)
        {
            return ProduceNewDomain(null, assemblies);
        }

        public static bool ProduceNewDomain(string name, object[] assemblies)
        {
            _mut.WaitOne();
            sDomain? dom = null;
            try
            {
                dom = new sDomain(name);
                dom.Value.Core.LoadAssemblies(assemblies);
                dom.Value.Core.EstablishParent(_core);
                _domains.Add(dom.Value);
                dom.Value.Core.Startup();
                _mut.ReleaseMutex();
                dom.Value.Domain.DomainUnload += Domain_DomainUnload;
            }
            catch (Exception e)
            {
                if (dom.HasValue)
                {
                    for (int x = 0; x < _domains.Count; x++)
                    {
                        if (_domains[x].Domain.FriendlyName == dom.Value.Domain.FriendlyName)
                        {
                            System.Error("Unloading {0} due to error in startup for application domain.", dom.Value.Domain.FriendlyName);
                            _domains.RemoveAt(x);
                            break;
                        }
                    }
                    _mut.ReleaseMutex();
                    try { AppDomain.Unload(dom.Value.Domain); }
                    catch (Exception ex) { }
                }
                dom=null;
            }
            return dom.HasValue;
        }

        internal static void Domain_DomainUnload(object sender, EventArgs e)
        {
            AppDomain dom = (AppDomain)sender;
            _mut.WaitOne();
            for (int x = 0; x < _domains.Count; x++)
            {
                if (_domains[x].Domain.FriendlyName == dom.FriendlyName)
                {
                    _domains.RemoveAt(x);
                    break;
                }
            }
            _mut.ReleaseMutex();
        }
    }
}
