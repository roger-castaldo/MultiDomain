using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Attributes.Messaging
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=false,Inherited=true)]
    public class BlockRequestIntercept : Attribute
    {
        private InterceptDirections _direction;
        public InterceptDirections Direction { get { return _direction; } }

        public BlockRequestIntercept(InterceptDirections direction)
        {
            _direction = direction;
        }
    }
}
