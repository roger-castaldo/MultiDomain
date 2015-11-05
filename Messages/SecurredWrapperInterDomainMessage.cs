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

        public bool IsHandlerAllowed(string appDomainName, string handlerTypeFullName)
        {
            return _message.IsHandlerAllowed(appDomainName, handlerTypeFullName);
        }

        public bool IsPreRequestInterceptorAllowed(string appDomainName, string handlerTypeFullName)
        {
            return _message.IsPreRequestInterceptorAllowed(appDomainName, handlerTypeFullName);
        }

        public bool IsPostRequestInterceptorAllowed(string appDomainName, string handlerTypeFullName)
        {
            return _message.IsPostRequestInterceptorAllowed(appDomainName, handlerTypeFullName);
        }
    }
}
