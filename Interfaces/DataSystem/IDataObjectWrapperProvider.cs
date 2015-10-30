using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.DataSystem
{
    public interface IDataObjectWrapperProvider
    {
        string[] InputObjectTypes { get; }
        IDataObject WrapDataObject(string type,IDataObject obj,out string newType);
    }
}
