using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.DataSystem
{
    public interface IDataObjectProvider
    {
        string[] ProvidedObjectTypes { get; }
        IDataObject ProduceNewObjectInstance(string type);
        bool DestroyObject(DataObject obj);
        bool StoreObject(DataObject obj);
        IDataObject[] SearchForObject(string type, ISearchCondition condition);
    }
}
