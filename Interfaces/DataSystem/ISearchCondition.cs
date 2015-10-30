using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.DataSystem
{
    public interface ISearchCondition
    {
        bool IsValidMatch(IDataObject obj);
    }
}
