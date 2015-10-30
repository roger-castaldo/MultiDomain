using Org.Reddragonit.MultiDomain.Interfaces.DataSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Messages
{
    public sealed class DataObject : MarshalByRefObject,IDataObject
    {
        private string _type;
        public string Type { get { return _type; } }
        private IDataObject _object;
        internal IDataObject Object { get { return _object; } }

        internal DataObject(IDataObject obj,string type){
            _object = obj;
            _type = type;
        }

        public string[] Properties
        {
            get { return _object.Properties; }
        }

        public object this[string name]
        {
            get
            {
                return _object[name];
            }
            set
            {
                _object[name] = value;
            }
        }

        public bool IsValid
        {
            get { return _object.IsValid; }
        }

        public object InvokeMethod(string name, object[] pars)
        {
            return _object.InvokeMethod(name, pars);
        }

        public string[] Methods
        {
            get { return _object.Methods; }
        }
    }
}
