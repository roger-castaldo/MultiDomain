using Org.Reddragonit.MultiDomain.Interfaces.DataSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Searchers
{
    public class NotCondition : MarshalByRefObject, ISearchCondition
    {
        private ISearchCondition _condition;

        public NotCondition(ISearchCondition condition)
        {
            _condition = condition;
        }

        public bool IsValidMatch(IDataObject obj)
        {
            return !_condition.IsValidMatch(obj);
        }
    }
}
