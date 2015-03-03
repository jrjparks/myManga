using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization;

namespace Core.DLL
{
    [Serializable, DebuggerStepThrough]
    public sealed class EmbeddedDLL
    {
        private readonly Assembly ExecutingAssembly;
        private readonly List<String> ManifestResourceNames = new List<String>();

        private readonly Dictionary<String, Assembly> _DLLs = new Dictionary<String, Assembly>();
        public Dictionary<String, Assembly> DLLs { get { return _DLLs; } }

        public EmbeddedDLL(Assembly ExecutingAssembly = null, Boolean PreLoadDLLs = false)
        {
            this.ExecutingAssembly = ExecutingAssembly ?? Assembly.GetEntryAssembly();
            this.ManifestResourceNames.AddRange(this.ExecutingAssembly.GetManifestResourceNames().Where((rn) => rn.EndsWith(".dll")));
            if (PreLoadDLLs) this.PreLoadDLLs();
        }

        private void PreLoadDLLs()
        {
            foreach (String ResourceName in this.ManifestResourceNames)
            { LoadDLL(ResourceName); }
        }

        public Assembly ResolveAssembly(Object Sender, ResolveEventArgs args)
        {
            try
            {
                AssemblyName ResourceAssemblyName = new AssemblyName(args.Name);
                String ResourceName = this.ManifestResourceNames.FirstOrDefault((s) => s.Contains(ResourceAssemblyName.Name));
                if (ResourceName != null)
                {
                    LoadDLL(ResourceName);
                    if (this.DLLs.ContainsKey(ResourceName))
                        return this.DLLs[ResourceName];
                }
                else { throw new Exception("ResourceName cannot be empty."); }
            }
            catch (Exception e)
            { throw new DynamicLinkLibraryException("Error loading DynamicLinkLibrary.", e); }
            return null;
        }

        private void LoadDLL(String ResourceName)
        {
            if (!this.DLLs.ContainsKey(ResourceName))
            {
                using (Stream stream = this.ExecutingAssembly.GetManifestResourceStream(ResourceName))
                {
                    if (stream != null)
                    {
                        Byte[] assemblyData = new Byte[stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        this.DLLs.Add(ResourceName, Assembly.Load(assemblyData));
                    }
                }
            }
        }

        public sealed class DynamicLinkLibraryException : Exception
        {
            public DynamicLinkLibraryException()
                : base()
            { }
            public DynamicLinkLibraryException(String message)
                : base(message)
            { }
            public DynamicLinkLibraryException(String message, Exception innerException)
                : base(message, innerException)
            { }
            public DynamicLinkLibraryException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            { }
        }
    }
}
