using Org.Reddragonit.MultiDomain.Attributes.Messaging;
using Org.Reddragonit.MultiDomain.Interfaces;
using Org.Reddragonit.MultiDomain.Interfaces.Messaging;
using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Controllers
{
    internal class MessageController :  IStartup,IShutdown,IInterDomainMessageHandler,IInterDomainMessagePreRequestInterceptor
    {
        private static Dictionary<sRoute,List<IInterDomainMessageHandler>> _handlers;
        private static Dictionary<sRoute,List<IInterDomainMessagePreRequestInterceptor>> _preRequestors;
        private static Dictionary<sRoute,List<IInterDomainMessagePostRequestInterceptor>> _postRequestors;

        static MessageController()
        {
            _handlers = new Dictionary<sRoute,List<IInterDomainMessageHandler>>();
            _preRequestors = new Dictionary<sRoute,List<IInterDomainMessagePreRequestInterceptor>>();
            _postRequestors = new Dictionary<sRoute,List<IInterDomainMessagePostRequestInterceptor>>();
        }

        public MessageController() { }

        public void Start()
        {
            foreach (Type parent in new Type[]{typeof(IInterDomainMessageHandler),typeof(IInterDomainMessagePostRequestInterceptor),typeof(IInterDomainMessagePreRequestInterceptor)})
            {
                foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        if (ass.GetName().Name != "mscorlib" && !ass.GetName().Name.StartsWith("System.") && ass.GetName().Name != "System" && !ass.GetName().Name.StartsWith("Microsoft"))
                        {
                            foreach (Type t in ass.GetTypes())
                            {
                                if (!t.IsInterface)
                                {
                                    if (new List<Type>(t.GetInterfaces()).Contains(parent) && t.FullName != this.GetType().FullName && !t.IsAbstract)
                                    {
                                        object obj = t.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                                        sRoute[] routes = _GenerateRoutes(obj);
                                        switch (parent.Name)
                                        {
                                            case "IInterDomainMessageHandler":
                                                lock (_handlers)
                                                {
                                                    foreach (sRoute srt in routes)
                                                    {
                                                        List<IInterDomainMessageHandler> handlers = new List<IInterDomainMessageHandler>();
                                                        if (_handlers.ContainsKey(srt))
                                                        {
                                                            handlers = _handlers[srt];
                                                            _handlers.Remove(srt);
                                                        }
                                                        handlers.Add((IInterDomainMessageHandler)obj);
                                                        _handlers.Add(srt, handlers);
                                                    }
                                                }
                                                break;
                                            case "IInterDomainMessagePostRequestInterceptor":
                                                lock (_postRequestors)
                                                {
                                                    foreach (sRoute srt in routes)
                                                    {
                                                        List<IInterDomainMessagePostRequestInterceptor> postInterceptors = new List<IInterDomainMessagePostRequestInterceptor>();
                                                        if (_postRequestors.ContainsKey(srt))
                                                        {
                                                            postInterceptors = _postRequestors[srt];
                                                            _postRequestors.Remove(srt);
                                                        }
                                                        postInterceptors.Add((IInterDomainMessagePostRequestInterceptor)obj);
                                                        _postRequestors.Add(srt, postInterceptors);
                                                    }
                                                }
                                                break;
                                            case "IInterDomainMessagePreRequestInterceptor":
                                                lock (_preRequestors)
                                                {
                                                    foreach (sRoute srt in routes)
                                                    {
                                                        List<IInterDomainMessagePreRequestInterceptor> preInterceptors = new List<IInterDomainMessagePreRequestInterceptor>();
                                                        if (_preRequestors.ContainsKey(srt))
                                                        {
                                                            preInterceptors = _preRequestors[srt];
                                                            _preRequestors.Remove(srt);
                                                        }
                                                        preInterceptors.Add((IInterDomainMessagePreRequestInterceptor)obj);
                                                        _preRequestors.Add(srt, preInterceptors);
                                                    }
                                                }
                                                break;
                                        }
                                        System._RegsiterMessageHandlerRoute(routes);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (e.Message != "The invoked member is not supported in a dynamic assembly."
                            && e.Message != "Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.")
                        {
                            throw e;
                        }
                    }
                }
            }
        }

        private sRoute[] _GenerateRoutes(object obj)
        {
            List<sRoute> ret = new List<sRoute>();
            foreach (HandlesMessage hm in obj.GetType().GetCustomAttributes(typeof(HandlesMessage),false)){
                ret.AddRange(new sRoute[]{
                    new sRoute(AppDomain.CurrentDomain.FriendlyName,obj.GetType().FullName,hm.MessageName),
                    new sRoute(null,obj.GetType().FullName,hm.MessageName),
                    new sRoute(AppDomain.CurrentDomain.FriendlyName,null,hm.MessageName),
                    new sRoute(null,null,hm.MessageName)
                });
            }
            if (ret.Count==0)
                throw new Exception(string.Format("Unable to create message interceptor/handler without any HandlesMessage attributes.  Type:{0},Domain:{1}",obj.GetType().FullName,AppDomain.CurrentDomain.FriendlyName));
            return ret.ToArray();
        }

        public void Shutdown()
        {
            sRoute[] keys;
            lock (_preRequestors) {
                keys = new sRoute[_preRequestors.Count];
                _preRequestors.Keys.CopyTo(keys, 0);
                System._UnRegisterMessageHandlerRoute(keys);
                _preRequestors.Clear(); 
            }
            lock (_handlers) {
                keys = new sRoute[_handlers.Count];
                _handlers.Keys.CopyTo(keys, 0);
                System._UnRegisterMessageHandlerRoute(keys);
                _handlers.Clear(); 
            }
            lock (_postRequestors) {
                keys = new sRoute[_postRequestors.Count];
                _postRequestors.Keys.CopyTo(keys, 0);
                System._UnRegisterMessageHandlerRoute(keys);
                _postRequestors.Clear(); 
            }
        }

        public bool InterceptsMessage(IInterDomainMessage message)
        {
            bool ret = false;
            RoutedInterDomainMessage ridm = (RoutedInterDomainMessage)message;
            lock (_preRequestors)
            {
                foreach (sRoute srt in ridm.PreInterceptRoutes)
                {
                    if (_preRequestors.ContainsKey(srt))
                    {
                        ret = true;
                        break;
                    }
                }
            }
            return ret;
        }

        public IInterDomainMessage InterceptMessage(IInterDomainMessage message)
        {
            IInterDomainMessage ret = message;
            RoutedInterDomainMessage ridm = (RoutedInterDomainMessage)message;
            lock (_preRequestors)
            {
                foreach (sRoute srt in ridm.PreInterceptRoutes)
                {
                    if (_preRequestors.ContainsKey(srt))
                    {
                        foreach (IInterDomainMessagePreRequestInterceptor iidmpri in _preRequestors[srt])
                            ret = iidmpri.InterceptMessage(ret);
                        break;
                    }
                }
            }
            return new RoutedInterDomainMessage(ridm,ret);
        }

        public bool HandlesMessage(IInterDomainMessage message)
        {
            bool ret = false;
            RoutedInterDomainMessage ridm = (RoutedInterDomainMessage)message;
            lock (_handlers)
            {
                foreach (sRoute srt in ridm.HandlerRoutes)
                {
                    if (_handlers.ContainsKey(srt))
                    {
                        ret = true;
                        break;
                    }
                }
            }
            return ret;
        }

        public object ProcessMessage(IInterDomainMessage message)
        {
            object ret = null;
            RoutedInterDomainMessage ridm = (RoutedInterDomainMessage)message;
            lock (_handlers)
            {
                foreach (sRoute srt in ridm.HandlerRoutes)
                {
                    if (_handlers.ContainsKey(srt))
                    {
                        foreach (IInterDomainMessageHandler idmh in _handlers[srt])
                        {
                            try
                            {
                                ret = idmh.ProcessMessage(ridm);
                                break;
                            }
                            catch (Exception e)
                            {
                                System.Error(e);
                                ret = null;
                            }
                        }
                        if (ret != null) { break; }
                    }
                }
            }
            return ret;
        }

        public bool InterceptsResponse(Messages.InterDomainMessageResponse response)
        {
            bool ret = false;
            RoutedInterDomainMessage ridm = (RoutedInterDomainMessage)response.Message;
            lock (_postRequestors)
            {
                foreach (sRoute srt in ridm.PostInterceptRoutes)
                {
                    if (_postRequestors.ContainsKey(srt))
                    {
                        ret = true;
                        break;
                    }
                }
            }
            return ret;
        }

        public void InterceptResponse(ref Messages.InterDomainMessageResponse response)
        {
            RoutedInterDomainMessage ridm = (RoutedInterDomainMessage)response.Message;
            lock (_postRequestors)
            {
                foreach (sRoute srt in ridm.PostInterceptRoutes)
                {
                    if (_postRequestors.ContainsKey(srt))
                    {
                        foreach (IInterDomainMessagePostRequestInterceptor idmpri in _postRequestors[srt])
                        {
                            if (!response.HasIntercepted(idmpri.GetType()))
                            {
                                object tmp;
                                idmpri.InterceptResponse(response, out tmp);
                                if (tmp != null)
                                {
                                    response = Messages.InterDomainMessageResponse.SwapResponse(response, tmp);
                                    response.MarkInterceptor(idmpri.GetType());
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
