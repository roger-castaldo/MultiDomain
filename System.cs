using Org.Reddragonit.MultiDomain.Controllers;
using Org.Reddragonit.MultiDomain.Interfaces.DataSystem;
using Org.Reddragonit.MultiDomain.Interfaces.EventSystem;
using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Org.Reddragonit.MultiDomain
{
    public static class System
    {
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
        }

        #region Events
        public static string RegisterHandler(IEventHandler handler) { return _core.RegisterHandler(handler); }
        public static void UnRegistereventHandler(string id) { _core.UnRegistereventHandler(id); }
        public static void TriggerEvent(IEvent Event) { 
            Messages.Event evnt = new Messages.Event(Event);
            _core.AbsoluteParent.ProcessEvent(evnt);
        }
        #endregion
        #region DataObjects
        public static DataObject ProduceNewObjectInstance(string type) { 
            IDataObject obj = _core.AbsoluteParent.ProduceNewObjectInstance(type);
            return (obj == null ? null : new DataObject(obj,type));
        }
        public static bool StoreObject(Messages.DataObject obj) { return _core.AbsoluteParent.StoreObject(obj); }
        public static bool DestroyObject(Messages.DataObject obj) { return _core.AbsoluteParent.DestroyObject(obj); }
        public static IDataObject[] SearchForObject(string type, ISearchCondition condition) { return _core.AbsoluteParent.SearchForObject(type, condition); }
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
                foreach (object obj in assemblies)
                {
                    if (obj is string)
                        dom.Value.Domain.Load((string)obj);
                    else if (obj is byte[])
                        dom.Value.Domain.Load((byte[])obj);
                    else
                        throw new Exception("Unable to load assembly in new domain unless it is a string or raw byte data.");
                }
                dom.Value.Core.EstablishParent(_core);
                dom.Value.Core.Startup();
                _domains.Add(dom.Value);
            }
            catch (Exception e)
            {
                if (dom.HasValue)
                {
                    try { AppDomain.Unload(dom.Value.Domain); }
                    catch (Exception ex) { }
                }
                dom=null;
            }
            _mut.ReleaseMutex();
            return dom.HasValue;
        }
    }
}
