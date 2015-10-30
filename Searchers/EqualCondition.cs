using Org.Reddragonit.MultiDomain.Interfaces.DataSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Searchers
{
    public class EqualCondition : MarshalByRefObject,ISearchCondition
    {
        private string _property;
        public string Property { get { return _property; } }

        private object _value;
        public object Value { get { return _value; } }

        public EqualCondition(string property, object value)
        {
            _property = property;
            _value = value;
        }

        public bool IsValidMatch(IDataObject obj)
        {
            if (obj[Property] == null && Value == null)
                return true;
            else if (obj[Property] != null && Value != null)
                return obj[Property].Equals(Value);
            return false;
        }
    }
}
