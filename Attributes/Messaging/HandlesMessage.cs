using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Attributes.Messaging
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true,Inherited=false)]
    public class HandlesMessage : Attribute
    {
        private string _messageName;
        public string MessageName { get { return _messageName; } }

        public HandlesMessage(string messageName)
        {
            _messageName = messageName;
        }
    }
}
