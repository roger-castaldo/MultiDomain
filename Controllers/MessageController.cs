using Org.Reddragonit.MultiDomain.Interfaces;
using Org.Reddragonit.MultiDomain.Interfaces.Messaging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Controllers
{
    internal class MessageController :  IStartup,IShutdown,IInterDomainMessageHandler,IInterDomainMessagePreRequestInterceptor
    {
        private static List<IInterDomainMessageHandler> _handlers;
        private static List<IInterDomainMessagePreRequestInterceptor> _preRequestors;
        private static List<IInterDomainMessagePostRequestInterceptor> _postRequestors;

        static MessageController()
        {
            _handlers = new List<IInterDomainMessageHandler>();
            _preRequestors = new List<IInterDomainMessagePreRequestInterceptor>();
            _postRequestors = new List<IInterDomainMessagePostRequestInterceptor>();
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
                                    if (new List<Type>(t.GetInterfaces()).Contains(parent) && t.FullName != this.GetType().FullName)
                                    {
                                        switch (parent.Name)
                                        {
                                            case "IInterDomainMessageHandler":
                                                _handlers.Add((IInterDomainMessageHandler)t.GetConstructor(Type.EmptyTypes).Invoke(new object[0]));
                                                break;
                                            case "IInterDomainMessagePostRequestInterceptor":
                                                _postRequestors.Add((IInterDomainMessagePostRequestInterceptor)t.GetConstructor(Type.EmptyTypes).Invoke(new object[0]));
                                                break;
                                            case "IInterDomainMessagePreRequestInterceptor":
                                                _preRequestors.Add((IInterDomainMessagePreRequestInterceptor)t.GetConstructor(Type.EmptyTypes).Invoke(new object[0]));
                                                break;
                                        }
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

        public void Shutdown()
        {
            lock (_preRequestors) { _preRequestors.Clear(); }
            lock (_handlers) { _handlers.Clear(); }
            lock (_postRequestors) { _postRequestors.Clear(); }
        }

        public bool InterceptsMessage(IInterDomainMessage message)
        {
            bool ret = false;
            lock (_preRequestors)
            {
                foreach (IInterDomainMessagePreRequestInterceptor idmpri in _preRequestors)
                {
                    try{
                        if (message is ISecuredInterDomainMessage)
                        {
                            if (((ISecuredInterDomainMessage)message).IsPreRequestInterceptorAllowed(AppDomain.CurrentDomain.FriendlyName, idmpri.GetType().FullName))
                            {
                                if (idmpri.InterceptsMessage(message))
                                {
                                    ret = true;
                                    break;
                                }
                            }
                        }
                        else if (idmpri.InterceptsMessage(message))
                        {
                            ret = true;
                            break;
                        }
                    }catch(Exception e){
                        System.Error(e);
                    }
                }
            }
            return ret;
        }

        public IInterDomainMessage InterceptMessage(IInterDomainMessage message)
        {
            IInterDomainMessage ret = message;
            lock (_preRequestors)
            {
                foreach (IInterDomainMessagePreRequestInterceptor idmpri in _preRequestors)
                {
                    try
                    {
                        if (ret is ISecuredInterDomainMessage)
                        {
                            if (((ISecuredInterDomainMessage)ret).IsPreRequestInterceptorAllowed(AppDomain.CurrentDomain.FriendlyName, idmpri.GetType().FullName))
                            {
                                if (idmpri.InterceptsMessage(ret))
                                    ret = idmpri.InterceptMessage(ret);
                            }
                        }
                        else if (idmpri.InterceptsMessage(ret))
                            ret = idmpri.InterceptMessage(ret);
                    }
                    catch (Exception e)
                    {
                        System.Error(e);
                    }
                }
            }
            return ret;
        }

        public bool HandlesMessage(IInterDomainMessage message)
        {
            bool ret = false;
            lock (_handlers)
            {
                foreach (IInterDomainMessageHandler idmh in _handlers)
                {
                    try
                    {
                        if (message is ISecuredInterDomainMessage)
                        {
                            if (((ISecuredInterDomainMessage)message).IsHandlerAllowed(AppDomain.CurrentDomain.FriendlyName, idmh.GetType().FullName))
                            {
                                if (idmh.HandlesMessage(message))
                                {
                                    ret = true;
                                    break;
                                }
                            }
                        }
                        else if (idmh.HandlesMessage(message))
                        {
                            ret = true;
                            break;
                        }
                    }
                    catch (Exception e) { System.Error(e); }
                }
            }
            return ret;
        }

        public object ProcessMessage(IInterDomainMessage message)
        {
            object ret = null;
            lock (_handlers)
            {
                foreach (IInterDomainMessageHandler idmh in _handlers)
                {
                    try
                    {
                        if (message is ISecuredInterDomainMessage)
                        {
                            if (((ISecuredInterDomainMessage)message).IsHandlerAllowed(AppDomain.CurrentDomain.FriendlyName, idmh.GetType().FullName))
                            {
                                if (idmh.HandlesMessage(message))
                                {
                                    ret = idmh.ProcessMessage(message);
                                    break;
                                }
                            }
                        }
                        else if (idmh.HandlesMessage(message))
                        {
                            ret = idmh.ProcessMessage(message);
                            break;
                        }
                    }
                    catch (Exception e) { System.Error(e); }
                }
            }
            return ret;
        }

        public bool InterceptsResponse(Messages.InterDomainMessageResponse response)
        {
            bool ret = false;
            lock (_postRequestors)
            {
                foreach (IInterDomainMessagePostRequestInterceptor idmpri in _postRequestors)
                {
                    try
                    {
                        if (response.Message is ISecuredInterDomainMessage)
                        {
                            if (((ISecuredInterDomainMessage)response.Message).IsPostRequestInterceptorAllowed(AppDomain.CurrentDomain.FriendlyName, idmpri.GetType().FullName))
                            {
                                if (idmpri.InterceptsResponse(response))
                                {
                                    ret = true;
                                    break;
                                }
                            }
                        }
                        else if (idmpri.InterceptsResponse(response))
                        {
                            ret = true;
                            break;
                        }
                    } catch (Exception e) { System.Error(e); }
                }
            }
            return ret;
        }

        public void InterceptResponse(ref Messages.InterDomainMessageResponse response)
        {
            lock (_postRequestors)
            {
                foreach (IInterDomainMessagePostRequestInterceptor idmpri in _postRequestors)
                {
                    object tmp = null;
                    try
                    {
                        if (response.Message is ISecuredInterDomainMessage)
                        {
                            if (((ISecuredInterDomainMessage)response.Message).IsPostRequestInterceptorAllowed(AppDomain.CurrentDomain.FriendlyName, idmpri.GetType().FullName))
                            {
                                if (idmpri.InterceptsResponse(response))
                                {
                                    idmpri.InterceptResponse(response, out tmp);
                                    if (tmp != null)
                                        response = Messages.InterDomainMessageResponse.SwapResponse(response, tmp);
                                    response.MarkInterceptor(idmpri.GetType());
                                }
                            }
                        }
                        else if (idmpri.InterceptsResponse(response))
                        {
                            idmpri.InterceptResponse(response, out tmp);
                            if (tmp != null)
                                response = Messages.InterDomainMessageResponse.SwapResponse(response, tmp);
                            response.MarkInterceptor(idmpri.GetType());
                        }
                    }
                    catch (Exception e) { System.Error(e); }
                }
            }
        }
    }
}
