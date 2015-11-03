using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.Messaging
{
    public interface IInterDomainMessage
    {
        string Name { get; }
        object this[string name] { get; }
        string[] Properties { get; }
    }
}
