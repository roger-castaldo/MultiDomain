using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Attributes.Messaging
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true,Inherited=true)]
    public class AllowedRequestHandler : Attribute
    {
        private string _domainName;
        public string DomainName { get { return _domainName; } }

        private string _handlerTypeFullName;
        public string HandlerTypeFullName { get { return _handlerTypeFullName; } }

        public AllowedRequestHandler(string domainName, string handlerTypeFullName)
        {
            _domainName = domainName;
            _handlerTypeFullName = handlerTypeFullName;
        }
    }
}
