using Org.Reddragonit.MultiDomain.Controllers;
using Org.Reddragonit.MultiDomain.Interfaces;
using Org.Reddragonit.MultiDomain.Interfaces.DataSystem;
using Org.Reddragonit.MultiDomain.Interfaces.EventSystem;
using Org.Reddragonit.MultiDomain.Interfaces.Logging;
using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.MultiDomain
{
    internal class Core : MarshalByRefObject
    {
        private static EventController _eventController;
        private static DataObjectController _dataObjectController;
        private static DataObjectWrapperController _dataObjectWrapperController;
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

        public Core() {
            if (_eventController == null)
            {
                _eventController = new EventController();
                _processEvent = new delProcessEvent(_eventController.ProcessEvent);
                _processEventInChildren = new delProcessEvent(_ProcessEventInChildren);
                _logController = new LogController();
                _dataObjectController = new DataObjectController();
                _dataObjectController.Start();
                _dataObjectWrapperController = new DataObjectWrapperController();
                _dataObjectWrapperController.Start();
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
        #region DataObjects
        public bool ProvidesType(string type) { return _dataObjectController.ProvidesType(type); }
        public IDataObject ProduceNewObjectInstance(string type) {
            IDataObject ret=null;
            if (ProvidesType(type))
            {
                ret = _dataObjectController.ProduceNewObjectInstance(type);
                if (ret != null)
                    ret = new DataObject(ret, type);
            }
            else
            {
                System.sDomain[] doms = System.Domains;
                foreach (System.sDomain dom in doms)
                {
                    ret = dom.Core.ProduceNewObjectInstance(type);
                    if (ret != null)
                        break;
                }
            }
            if (!HasParent)
            {
                Queue<string> types = new Queue<string>();
                types.Enqueue(type);
                while (types.Count > 0)
                {
                    string tmp = types.Dequeue();
                    if (_dataObjectWrapperController.WrapsType(tmp))
                        ret = _dataObjectWrapperController.WrapDataObject(tmp, ret, ref types);
                    System.sDomain[] doms = System.Domains;
                    foreach (System.sDomain dom in doms)
                        ret = dom.Core.WrapDataObject(ret, tmp, ref types);
                }
            }
            return ret;
        }
        public IDataObject[] SearchForObject(string type, ISearchCondition condition)
        {
            IDataObject[] ret = null;
            if (ProvidesType(type))
            {
                ret = _dataObjectController.SearchForObject(type,condition);
                if (ret != null)
                {
                    for (int x = 0; x < ret.Length; x++)
                        ret[x] = new DataObject(ret[x], type);
                }
            }
            else
            {
                System.sDomain[] doms = System.Domains;
                foreach (System.sDomain dom in doms)
                {
                    ret = dom.Core.SearchForObject(type,condition);
                    if (ret != null)
                        break;
                }
            }
            if (!HasParent)
            {
                for (int x = 0; x < ret.Length; x++)
                {
                    Queue<string> types = new Queue<string>();
                    types.Enqueue(type);
                    while (types.Count > 0)
                    {
                        string tmp = types.Dequeue();
                        if (_dataObjectWrapperController.WrapsType(tmp))
                            ret[x] = _dataObjectWrapperController.WrapDataObject(tmp, ret[x], ref types);
                        System.sDomain[] doms = System.Domains;
                        foreach (System.sDomain dom in doms)
                            ret[x] = dom.Core.WrapDataObject(ret[x], tmp, ref types);
                    }
                }
                List<IDataObject> tret = new List<IDataObject>(ret);
                for (int x = 0; x < tret.Count; x++)
                {
                    if (!condition.IsValidMatch(tret[x]))
                    {
                        tret.RemoveAt(x);
                        x--;
                    }
                }
                ret = tret.ToArray();
            }
            return ret;
        }
        public IDataObject WrapDataObject(IDataObject obj, string type, ref Queue<string> types)
        {
            IDataObject ret = obj;
            if (_dataObjectWrapperController.WrapsType(type))
                ret = _dataObjectWrapperController.WrapDataObject(type, ret, ref types);
            System.sDomain[] doms = System.Domains;
            foreach (System.sDomain dom in doms)
                ret = dom.Core.WrapDataObject(ret, type, ref types);
            return ret;
        }
        public bool DestroyObject(Messages.DataObject obj)
        {
            if (ProvidesType(obj.Type))
            {
                bool ret = _dataObjectController.DestroyObject(obj);
                if (obj.Object is DataObject)
                    ret = AbsoluteParent.DestroyObject((Messages.DataObject)obj.Object);
                return ret;
            }
            else { 
                System.sDomain[] doms = System.Domains;
                foreach (System.sDomain dom in doms)
                {
                    if (dom.Core.ProvidesType(obj.Type))
                        return dom.Core.DestroyObject(obj);
                }
            }
            return false;
        }
        public bool StoreObject(Messages.DataObject obj)
        {
            bool ret = false;
            if (ProvidesType(obj.Type))
            {
                ret = _dataObjectController.StoreObject(obj);
                if (obj.Object is DataObject)
                    ret |= AbsoluteParent.StoreObject((Messages.DataObject)obj.Object);
            }
            else
            {
                System.sDomain[] doms = System.Domains;
                foreach (System.sDomain dom in doms)
                    ret |= dom.Core.StoreObject(obj);
            }
            return ret;
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
                if (start is DataObjectController)
                    _dataObjectController = (DataObjectController)start;
                else if (start is DataObjectWrapperController)
                    _dataObjectWrapperController = (DataObjectWrapperController)start;
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
