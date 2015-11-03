using Org.Reddragonit.MultiDomain.Interfaces.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Messages
{
    internal class SecurredWrapperInterDomainMessage : WrapperInterDomainMessage, ISecuredInterDomainMessage
    {
        private ISecuredInterDomainMessage _message;

        public SecurredWrapperInterDomainMessage(ISecuredInterDomainMessage message)
            : base((IInterDomainMessage)message)
        {
            _message = message;
        }

        public bool IsHandlerAllowed(string appDomainName, Type handlerType)
        {
            return _message.IsHandlerAllowed(appDomainName, handlerType);
        }

        public bool IsPreRequestInterceptorAllowed(string appDomainName, Type handlerType)
        {
            return _message.IsPreRequestInterceptorAllowed(appDomainName, handlerType);
        }

        public bool IsPostRequestInterceptorAllowed(string appDomainName, Type handlerType)
        {
            return _message.IsPostRequestInterceptorAllowed(appDomainName, handlerType);
        }
    }
}
