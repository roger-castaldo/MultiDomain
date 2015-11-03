using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.Messaging
{
    public interface ISecuredInterDomainMessage : IInterDomainMessage
    {
        bool IsHandlerAllowed(string appDomainName, Type handlerType);
        bool IsPreRequestInterceptorAllowed(string appDomainName, Type handlerType);
        bool IsPostRequestInterceptorAllowed(string appDomainName, Type handlerType);
    }
}
