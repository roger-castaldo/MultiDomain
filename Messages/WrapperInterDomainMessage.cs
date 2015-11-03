using Org.Reddragonit.MultiDomain.Interfaces.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Messages
{
    internal class WrapperInterDomainMessage : MarshalByRefObject,IInterDomainMessage
    {
        private IInterDomainMessage _message;

        public string Name
        {
            get { return _message.Name; }
        }

        public object this[string name]
        {
            get { return _message[name]; }
        }

        public string[] Properties
        {
            get { return _message.Properties; }
        }

        internal WrapperInterDomainMessage(IInterDomainMessage message) { _message = message; }
    }
}
