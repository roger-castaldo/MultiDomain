using Org.Reddragonit.MultiDomain.Interfaces.EventSystem;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Org.Reddragonit.MultiDomain.Messages
{
    public sealed class Event : MarshalByRefObject,IEvent
    {
        private IEvent _event;

        internal bool IsSecurred { get { return _event is ISecurredEvent; } }
        internal bool IsHandlerAllowed(string appDomainName, string handlerTypeFullName) { return ((ISecurredEvent)_event).IsHandlerAllowed(appDomainName, handlerTypeFullName); }

        public string Name { get { return _event.Name; } }

        public object this[string name]
        {
            get { return _event[name]; }
        }

        public string[] Properties
        {
            get { return _event.Properties; }
        }

        private DateTime _createTime;
        public DateTime CreateTime { get { return _createTime; } }

        internal bool IsSynchronous { get { return new List<Type>(_event.GetType().GetInterfaces()).Contains(typeof(ISynchronousEvent)); } }

        private Mutex _mut;
        private List<string> _invokedDomains;
        internal void MarkInvokedInDomain(AppDomain domain)
        {
            _mut.WaitOne();
            if (!_invokedDomains.Contains(domain.FriendlyName))
                _invokedDomains.Add(domain.FriendlyName);
            _mut.ReleaseMutex();
        }

        internal bool NeedsToBeProcessed(AppDomain domain)
        {
            bool ret = true;
            _mut.WaitOne();
            ret = !_invokedDomains.Contains(domain.FriendlyName);
            _mut.ReleaseMutex();
            return ret;
        }

        internal Event(IEvent Event){
            _createTime = DateTime.Now;
            _event = Event;
            _mut = new Mutex(false);
            _invokedDomains = new List<string>();
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public new Type GetType()
        {
            return _event.GetType();
        }
    }
}
