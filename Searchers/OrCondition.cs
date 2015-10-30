using Org.Reddragonit.MultiDomain.Interfaces.DataSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Searchers
{
    public class OrCondition : MarshalByRefObject, ISearchCondition
    {
        private ISearchCondition[] _conditions;
        public ISearchCondition[] Conditions { get { return _conditions; } }

        public OrCondition(ISearchCondition[] conditions)
        {
            _conditions = conditions;
        }

        public bool IsValidMatch(IDataObject obj)
        {
            foreach (ISearchCondition cond in _conditions)
            {
                if (cond.IsValidMatch(obj))
                    return true;
            }
            return false;
        }
    }
}
