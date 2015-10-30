using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.EventSystem
{
    public interface IEvent
    {
        object this[string name] { get; }
        string[] Properties { get; }
    }
}
