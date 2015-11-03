using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.Messaging
{
    public interface IInterDomainMessageHandler
    {
        bool HandlesMessage(IInterDomainMessage message);
        object ProcessMessage(IInterDomainMessage message);
    }
}
