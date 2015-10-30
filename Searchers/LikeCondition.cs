using Org.Reddragonit.MultiDomain.Interfaces.DataSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Searchers
{
    public class LikeCondition : MarshalByRefObject, ISearchCondition
    {
        private string _property;
        public string Property { get { return _property; } }

        private string _value;
        public string Value { get { return _value; } }

        public LikeCondition(string property, string value)
        {
            _property = property;
            _value = value;
        }

        public bool IsValidMatch(IDataObject obj)
        {
            if (obj[Property] == null && Value == null)
                return true;
            else if (obj[Property] != null && Value != null)
                return obj[Property].ToString().ToUpper().Contains(Value.ToUpper());
            return false;
        }
    }
}
