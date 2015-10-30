using Org.Reddragonit.MultiDomain.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.EventSystem
{
    public interface IEventHandler
    {
        void ProcessEvent(Event Event);
    }
}
