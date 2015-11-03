using Org.Reddragonit.MultiDomain.Interfaces;
using Org.Reddragonit.MultiDomain.Interfaces.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.MultiDomain.Controllers
{
    internal sealed class LogController : IStartup,ILogWriter,IShutdown
    {
        private delegate void delAppendEntry(string sourceDomainName, AssemblyName sourceAssembly, string sourceTypeName, string sourceMethodName, LogLevels level, DateTime timestamp, string message);
        private static List<ILogWriter> _loggers;
        private static List<delAppendEntry> _delegates = new List<delAppendEntry>();

        static LogController()
        {
            _loggers = new List<ILogWriter>();
            _delegates = new List<delAppendEntry>();
        }

        public LogController()
        {
        }

        public void Start()
        {
            Type parent = typeof(ILogWriter);
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
                                    _loggers.Add((ILogWriter)Activator.CreateInstance(t));
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
            foreach (ILogWriter il in _loggers)
                _delegates.Add(new delAppendEntry(il.AppendEntry));
        }

        public void AppendEntry(string sourceDomainName, global::System.Reflection.AssemblyName sourceAssembly, string sourceTypeName, string sourceMethodName, LogLevels level, DateTime timestamp, string message)
        {
            lock (_loggers)
            {
                for (int x = 0; x < _loggers.Count; x++)
                {
                    try
                    {
                        _delegates[x].BeginInvoke(sourceDomainName, sourceAssembly, sourceTypeName, sourceMethodName, level, timestamp, message, null, _loggers[x]);
                    }
                    catch (Exception ex) { }
                }
            }
        }

        public void Shutdown()
        {
            lock (_loggers)
            {
                _loggers.Clear();
                _delegates.Clear();
            }
        }
    }
}
