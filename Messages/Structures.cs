using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Messages
{
    public struct sInterceptor
    {
        private string _appDomain;
        public string AppDomain { get { return _appDomain; } }

        private Type _type;
        public Type Type { get { return _type; } }

        internal sInterceptor(string appDomain, Type type)
        {
            _appDomain = appDomain;
            _type = type;
        }
    }

        
}
