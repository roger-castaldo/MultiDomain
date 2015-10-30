using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Interfaces.Logging
{
    public interface ILogWriter : IShutdown
    {
        void AppendEntry(string sourceDomainName, AssemblyName sourceAssembly, string sourceTypeName, string sourceMethodName, LogLevels level, DateTime timestamp, string message);
    }
}
