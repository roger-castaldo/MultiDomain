using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.EventSystem
{
    public interface ISecurredEvent : IEvent
    {
        bool IsHandlerAllowed(string appDomainName, string handlerTypeFullName);
    }
}
