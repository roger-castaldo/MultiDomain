using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.Messaging
{
    public interface IInterDomainMessagePreRequestInterceptor
    {
        bool InterceptsMessage(IInterDomainMessage message);
        IInterDomainMessage InterceptMessage(IInterDomainMessage message);
    }
}
