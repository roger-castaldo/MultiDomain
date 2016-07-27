using Org.Reddragonit.MultiDomain.Attributes.Messaging;
using Org.Reddragonit.MultiDomain.Interfaces.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Messages
{
    internal class RoutedInterDomainMessage : MarshalByRefObject,IInterDomainMessage
    {
        private List<sRoute> _handlerRoutes;
        public sRoute[] HandlerRoutes { get { return _handlerRoutes.ToArray(); } }

        private List<sRoute> _preInterceptRoutes;
        public sRoute[] PreInterceptRoutes { get { return _preInterceptRoutes.ToArray(); } }

        private List<sRoute> _postInterceptRoutes;
        public sRoute[] PostInterceptRoutes { get { return _postInterceptRoutes.ToArray(); } }

        private IInterDomainMessage _message;

        public RoutedInterDomainMessage(RoutedInterDomainMessage ridm, IInterDomainMessage message)
        {
            _handlerRoutes = ridm._handlerRoutes;
            _preInterceptRoutes = ridm._preInterceptRoutes;
            _postInterceptRoutes = ridm._postInterceptRoutes;
            _message = message;
        }

        public RoutedInterDomainMessage(IInterDomainMessage message)
        {
            _message = message;
            _handlerRoutes = new List<sRoute>();
            foreach (AllowedRequestHandler arh in message.GetType().GetCustomAttributes(typeof(AllowedRequestHandler), true))
                _handlerRoutes.Add(new sRoute(arh.DomainName, arh.HandlerTypeFullName,message.Name));
            if (_handlerRoutes.Count == 0)
                _handlerRoutes.Add(new sRoute(null, null,message.Name));
            bool addPreIntercepts = true;
            bool addPostIntercepts = true;
            foreach (BlockRequestIntercept bri in message.GetType().GetCustomAttributes(typeof(BlockRequestIntercept),true))
            {
                switch (bri.Direction)
                {
                    case InterceptDirections.Both:
                        addPostIntercepts = false;
                        addPreIntercepts = false;
                        break; 
                    case InterceptDirections.Post:
                        addPostIntercepts = false;
                        break;
                    case InterceptDirections.Pre:
                        addPreIntercepts = false;
                        break;
                }
            }
            _preInterceptRoutes = new List<sRoute>();
            _postInterceptRoutes = new List<sRoute>();
            foreach (AllowedRequestIntercept ari in message.GetType().GetCustomAttributes(typeof(AllowedRequestIntercept), true))
            {
                if ((ari.Direction == InterceptDirections.Both || ari.Direction == InterceptDirections.Pre) && addPreIntercepts)
                    _preInterceptRoutes.Add(new sRoute(ari.DomainName, ari.HandlerTypeFullName,message.Name));
                if ((ari.Direction == InterceptDirections.Both || ari.Direction == InterceptDirections.Post) && addPostIntercepts)
                    _postInterceptRoutes.Add(new sRoute(ari.DomainName, ari.HandlerTypeFullName, message.Name));
            }            
            if (_preInterceptRoutes.Count == 0 && addPreIntercepts)
                _preInterceptRoutes.Add(new sRoute(null, null, message.Name));
            if (_postInterceptRoutes.Count == 0 && addPostIntercepts)
                _postInterceptRoutes.Add(new sRoute(null, null, message.Name));
        }

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

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
