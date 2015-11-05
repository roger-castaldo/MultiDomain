using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.Messaging
{
    public interface ISecuredInterDomainMessage : IInterDomainMessage
    {
        bool IsHandlerAllowed(string appDomainName, string handlerTypeFullName);
        bool IsPreRequestInterceptorAllowed(string appDomainName, string handlerTypeFullName);
        bool IsPostRequestInterceptorAllowed(string appDomainName, string handlerTypeFullName);
    }
}
