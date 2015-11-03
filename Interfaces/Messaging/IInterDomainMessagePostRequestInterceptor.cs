using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.Messaging
{
    public interface IInterDomainMessagePostRequestInterceptor
    {
        bool InterceptsResponse(InterDomainMessageResponse response);
        void InterceptResponse(InterDomainMessageResponse response,out object newResponseObject);
    }
}
