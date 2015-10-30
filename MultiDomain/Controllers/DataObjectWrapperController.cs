using Org.Reddragonit.MultiDomain.Interfaces;
using Org.Reddragonit.MultiDomain.Interfaces.DataSystem;
using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Controllers
{
    internal sealed class DataObjectWrapperController : IStartup
    {
        private List<IDataObjectWrapperProvider> _providers;

        public DataObjectWrapperController()
        {
            _providers = new List<IDataObjectWrapperProvider>();
        }

        public void Start()
        {
            Type parent = typeof(IDataObjectWrapperProvider);
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
                                    _providers.Add((IDataObjectWrapperProvider)Activator.CreateInstance(t));
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
            foreach (IDataObjectWrapperProvider idop in _providers)
                ret.AddRange(idop.InputObjectTypes);
            _inputObjectTypes = ret.ToArray();
        }

        private string[] _inputObjectTypes;
        public string[] InputObjectTypes
        {
            get { return _inputObjectTypes; }
        }

        public IDataObject WrapDataObject(string type, IDataObject obj,ref Queue<string> types)
        {
            IDataObject ret = obj;
            foreach (IDataObjectWrapperProvider dowp in _providers)
            {
                if (new List<string>(dowp.InputObjectTypes).Contains(type))
                {
                    string newType;
                    ret = dowp.WrapDataObject(type, obj,out newType);
                    if (newType != null)
                    {
                        types.Enqueue(ret.GetType().FullName);
                        ret = new DataObject(ret, newType);
                    }
                }
            }
            return ret;
        }

        internal bool WrapsType(string tmp)
        {
            return new List<string>(_inputObjectTypes).Contains(tmp);
        }
    }
}
