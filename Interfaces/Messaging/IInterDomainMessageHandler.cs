﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.Messaging
{
    public interface IInterDomainMessageHandler
    {
        object ProcessMessage(IInterDomainMessage message);
    }
}
