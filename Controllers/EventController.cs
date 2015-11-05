using Org.Reddragonit.MultiDomain.Interfaces;
using Org.Reddragonit.MultiDomain.Interfaces.EventSystem;
using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Org.Reddragonit.MultiDomain.Controllers
{
    internal sealed class EventController
    {
        private struct sEventHandler
        {
            private string _id;
            public string ID { get { return _id; } }

            private IEventHandler _handler;
            public IEventHandler Handler { get { return _handler; } }

            public sEventHandler(IEventHandler handler,string id)
            {
                _id=id;
                _handler = handler;
            }
        }

        private const int _ID_LENGTH = 32;

        private Mutex _mut;
        private MT19937 _rand;
        private List<sEventHandler> _handlers;

        public EventController()
        {
            _mut = new Mutex(false);
            _rand = new MT19937(DateTime.Now.Ticks);
            _handlers = new List<sEventHandler>();
        }

        public string RegisterHandler(IEventHandler handler)
        {
            _mut.WaitOne();
            string ret = _rand.NextString(_ID_LENGTH);
            _handlers.Add(new sEventHandler(handler, ret));
            _mut.ReleaseMutex();
            return ret;
        }

        public void UnRegistereventHandler(string id)
        {
            _mut.WaitOne();
            for (int x = 0; x < _handlers.Count; x++)
            {
                if (_handlers[x].ID == id)
                {
                    _handlers.RemoveAt(x);
                    break;
                }
            }
            _mut.ReleaseMutex();
        }

        public void ProcessEvent(Event Event)
        {
            if (Event.NeedsToBeProcessed(AppDomain.CurrentDomain))
            {
                Event.MarkInvokedInDomain(AppDomain.CurrentDomain);
                sEventHandler[] tmp;
                _mut.WaitOne();
                tmp = new sEventHandler[_handlers.Count];
                _handlers.CopyTo(tmp, 0);
                _mut.ReleaseMutex();
                if (Event.IsSecurred)
                {
                    List<sEventHandler> handlers = new List<sEventHandler>();
                    foreach (sEventHandler hndlr in tmp)
                    {
                        if (Event.IsHandlerAllowed(AppDomain.CurrentDomain.FriendlyName, hndlr.Handler.GetType().FullName))
                            handlers.Add(hndlr);
                    }
                }else
                    _ProcessEvent(Event, new List<sEventHandler>(tmp));
            }
        }

        private void _ProcessEvent(Event Event, List<sEventHandler> handlers)
        {
            if (Event.IsSynchronous)
            {
                foreach (sEventHandler handler in handlers)
                {
                    try
                    {
                        handler.Handler.ProcessEvent(Event);
                    }
                    catch (Exception e)
                    {

                    }
                }
            }
            else
            {
                foreach (sEventHandler handler in handlers)
                {
                    Thread th = new Thread(new ParameterizedThreadStart(_ProcessEventAsync));
                    th.IsBackground = true;
                    th.Start(new object[]{
                        Event,
                        handler
                    });
                }
            }
        }

        private void _ProcessEventAsync(object obj)
        {
            Event Event = (Event)((object[])obj)[0];
            sEventHandler handler = (sEventHandler)((object[])obj)[1];
            try
            {
                handler.Handler.ProcessEvent(Event);
            }
            catch (Exception e)
            {

            }
        }
    }
}
