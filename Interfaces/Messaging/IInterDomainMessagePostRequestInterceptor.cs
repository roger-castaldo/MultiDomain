using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.Messaging
{
    public interface IInterDomainMessagePostRequestInterceptor
    {
        void InterceptResponse(InterDomainMessageResponse response,out object newResponseObject);
    }
}
