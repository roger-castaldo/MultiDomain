using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Attributes.Messaging
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true,Inherited=true)]
    public class AllowedRequestIntercept : Attribute
    {
        private InterceptDirections _direction;
        public InterceptDirections Direction { get { return _direction; } }

        private string _domainName;
        public string DomainName { get { return _domainName; } }

        private string _handlerTypeFullName;
        public string HandlerTypeFullName { get { return _handlerTypeFullName; } }

        public AllowedRequestIntercept(string domainName, string handlerTypeFullName, InterceptDirections direction)
        {
            _direction = direction;
            _domainName = domainName;
            _handlerTypeFullName = handlerTypeFullName;
        }
    }
}
