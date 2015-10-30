using Org.Reddragonit.MultiDomain.Interfaces;
using Org.Reddragonit.MultiDomain.Interfaces.DataSystem;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Org.Reddragonit.MultiDomain.Controllers
{
    internal sealed class DataObjectController : IDataObjectProvider,IStartup
    {
        private List<IDataObjectProvider> _providers;

        public DataObjectController()
        {
            _providers = new List<IDataObjectProvider>();
        }

        public void Start()
        {
            Type parent = typeof(IDataObjectProvider);
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
                                if (new List<Type>(t.GetInterfaces()).Contains(parent) && t.FullName!=this.GetType().FullName)
                                    _providers.Add((IDataObjectProvider)Activator.CreateInstance(t));
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
            List<string> ret = new List<string>();
            foreach (IDataObjectProvider idop in _providers)
                ret.AddRange(idop.ProvidedObjectTypes);
            _providedObjectTypes = ret.ToArray();
        }

        public bool ProvidesType(string type)
        {
            return new List<string>(_providedObjectTypes).Contains(type);
        }

        private string[] _providedObjectTypes;
        public string[] ProvidedObjectTypes
        {
            get { return _providedObjectTypes; }
        }

        public IDataObject ProduceNewObjectInstance(string type)
        {
            foreach (IDataObjectProvider idop in _providers)
            {
                if (new List<string>(idop.ProvidedObjectTypes).Contains(type))
                    return idop.ProduceNewObjectInstance(type);
            }
            return null;
        }

        public bool DestroyObject(Messages.DataObject obj)
        {
            foreach (IDataObjectProvider idop in _providers)
            {
                if (new List<string>(idop.ProvidedObjectTypes).Contains(obj.Type))
                    return idop.DestroyObject(obj);
            }
            return false;
        }

        public bool StoreObject(Messages.DataObject obj)
        {
            foreach (IDataObjectProvider idop in _providers)
            {
                if (new List<string>(idop.ProvidedObjectTypes).Contains(obj.Type))
                    return idop.StoreObject(obj);
            }
            return false;
        }


        public IDataObject[] SearchForObject(string type, ISearchCondition condition)
        {
            foreach (IDataObjectProvider idop in _providers)
            {
                if (new List<string>(idop.ProvidedObjectTypes).Contains(type))
                    return idop.SearchForObject(type,condition);
            }
            return null;
        }
    }
}
