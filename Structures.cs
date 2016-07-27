using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Org.Reddragonit.MultiDomain
{
    [Serializable()]
    internal class sRoute : ISerializable
    {
        private string _domainName;
        public string DomainName{get{return _domainName;}}

        private string _typeName;
        public string TypeName{get{return _typeName;}}

        private string _messageName;
        public string MessageName { get { return _messageName; } }

        internal sRoute(string domainName, string typeName,string messageName)
        {
            _domainName = domainName;
            _typeName = typeName;
            _messageName = messageName;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}:{2}", (_domainName == null ? "*" : _domainName), (_typeName == null ? "*" : _typeName),(_messageName==null ? "*" : _messageName));
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj.ToString() == ToString();
        }

        protected sRoute(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            _domainName = info.GetString("domainName");
            _typeName = info.GetString("typeName");
            _messageName = info.GetString("messageName");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            info.AddValue("domainName", _domainName);
            info.AddValue("typeName", _typeName);
            info.AddValue("messageName", _messageName);
        }
    }

}
