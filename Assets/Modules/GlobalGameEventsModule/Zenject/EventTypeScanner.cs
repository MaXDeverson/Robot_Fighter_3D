#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Game.Events.DI
{
    /// <summary>
    /// Finds every event type in the loaded assemblies so their paths can be checked up front.
    /// </summary>
    /// <remarks>
    /// Only compiled into the editor and development builds. The scan is narrowed to assemblies that actually
    /// reference the core assembly, which keeps it to a handful out of the hundred or so Unity loads.
    /// </remarks>
    public sealed class EventTypeScanner
    {
        private readonly List<Type> _buffer = new List<Type>();

        /// <summary>
        /// Collects every non generic struct implementing <see cref="IGameEvent"/>.
        /// </summary>
        /// <returns>The scanner's own buffer, valid until the next call.</returns>
        public IReadOnlyList<Type> FindEventTypes()
        {
            _buffer.Clear();

            string coreAssemblyName = typeof(IGameEvent).Assembly.GetName().Name;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (!ReferencesCore(assembly, coreAssemblyName) || IsTestAssembly(assembly))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }
                catch (Exception)
                {
                    continue;
                }

                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    if (type == null || !type.IsValueType || type.IsGenericTypeDefinition)
                    {
                        continue;
                    }

                    if (typeof(IGameEvent).IsAssignableFrom(type))
                    {
                        _buffer.Add(type);
                    }
                }
            }

            return _buffer;
        }

        /// <summary>
        /// Test assemblies are skipped. A suite legitimately declares events with duplicated or malformed paths
        /// to prove the registry rejects them, and those must not be reported as problems in the real project.
        /// </summary>
        private bool IsTestAssembly(Assembly assembly)
        {
            AssemblyName[] referenced;
            try
            {
                referenced = assembly.GetReferencedAssemblies();
            }
            catch (Exception)
            {
                return false;
            }

            for (int i = 0; i < referenced.Length; i++)
            {
                if (referenced[i].Name == "nunit.framework")
                {
                    return true;
                }
            }

            return false;
        }

        private bool ReferencesCore(Assembly assembly, string coreAssemblyName)
        {
            if (assembly.GetName().Name == coreAssemblyName)
            {
                return true;
            }

            AssemblyName[] referenced;
            try
            {
                referenced = assembly.GetReferencedAssemblies();
            }
            catch (Exception)
            {
                return false;
            }

            for (int i = 0; i < referenced.Length; i++)
            {
                if (referenced[i].Name == coreAssemblyName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
