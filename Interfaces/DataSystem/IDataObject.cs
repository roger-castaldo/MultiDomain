using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.DataSystem
{
    public interface IDataObject
    {
        string[] Properties { get; }
        object this[string name] { get; set; }
        bool IsValid { get; }
        string[] Methods { get; }
        object InvokeMethod(string name, object[] pars);
    }
}
