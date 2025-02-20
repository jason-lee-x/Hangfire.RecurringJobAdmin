using Hangfire.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Hangfire.RecurringJobAdmin.Core
{
    internal sealed class StorageAssemblySingleton
    {
        private StorageAssemblySingleton()
        {
        }

        private static StorageAssemblySingleton _instance;
        private string[] prefixIgnore = new[] { "Hangfire.RecurringJobAdmin.dll", "Microsoft." };


        public List<Assembly> currentAssembly { get; private set; } = new List<Assembly>();

        internal static StorageAssemblySingleton GetInstance()
        {
            if (_instance == null)
            {
                _instance = new StorageAssemblySingleton();
            }
            return _instance;
        }

        internal void SetCurrentAssembly(bool includeReferences = false, params Assembly[] assemblies)
        {
            currentAssembly.AddRange(assemblies);

            if (includeReferences)
            {
                var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
                var toLoad = referencedPaths.Where(r => !assemblies.Any(x => x.Location.Equals(r)))
                                .Where(x => !prefixIgnore.Any(p => p.Contains(x)))
                                .ToList();

                toLoad.ForEach(path => currentAssembly.Add(Assembly.LoadFile(path)));
                LoadReferences(currentAssembly, currentAssembly);
            }
        }

        internal void LoadReferences(List<Assembly> assembliesToLoadReferences, List<Assembly> allLoadedAssemblies)
        {
            var newlyLoadedAssemblies = new List<Assembly>();
            foreach (var assembly in assembliesToLoadReferences)
            {
                foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies().Where(an => !allLoadedAssemblies.Any(a => a.FullName == an.FullName)))
                {
                    try
                    {
                        newlyLoadedAssemblies.Add(Assembly.Load(referencedAssemblyName));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            if (newlyLoadedAssemblies.Count > 0)
            {
                allLoadedAssemblies.AddRange(newlyLoadedAssemblies);
                LoadReferences(newlyLoadedAssemblies, allLoadedAssemblies);
            }
        }

        public bool IsValidType(string type) => Type.GetType(type) != null || currentAssembly.Any(x => x.GetType(type) != null);

        public bool IsValidMethod(string type, string method, Type[] argTypes) => (Type.GetType(type) ?? currentAssembly?
            .FirstOrDefault(x => x.GetType(type) != null)?.GetType(type))?.GetMethod(method, argTypes) != null;

        public bool AreValidArguments(string type, string method, IEnumerable<object> args, Type[] argTypes)
        {
            if (!IsValidMethod(type, method, argTypes))
                return false;
            //var parameters = currentAssembly
            //    .FirstOrDefault(x => x.GetType(type) != null)
            //    .GetType(type)
            //    .GetMethod(method, argTypes)
            //    .GetParameters();
            var argsList = args?.ToList();
            if (argTypes?.Count() != argsList?.Count)
            {
                return false;
            }
            for (var i = 0; i < argsList.Count; i++)
            {
                try
                {
                    _ = Convert.ChangeType(argsList[i], argTypes[i]);
                }
                catch { return false; }
            }
            return true;
        }
    }
}
