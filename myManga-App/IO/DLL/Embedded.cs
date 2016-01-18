using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace myManga_App.IO.DLL
{
    [Serializable, DebuggerStepThrough]
    public sealed class Embedded
    {
        private readonly Assembly EmbeddedAssembly;
        private readonly List<String> ManifestResourceNames;

        public Dictionary<String, Assembly> EmbeddedDLLs
        { get; private set; }

        public Embedded()
            : this(Assembly.GetEntryAssembly(), false)
        { }

        public Embedded(Assembly ExecutingAssembly)
            : this(ExecutingAssembly, false)
        { }

        public Embedded(Boolean Preload)
            : this(Assembly.GetEntryAssembly(), Preload)
        { }

        public Embedded(Assembly ExecutingAssembly, Boolean Preload)
        {
            EmbeddedAssembly = ExecutingAssembly;
            ManifestResourceNames = new List<String>();
            EmbeddedDLLs = new Dictionary<String, Assembly>();

            ManifestResourceNames.AddRange(EmbeddedAssembly.GetManifestResourceNames().Where(ManifestResourceName => ManifestResourceName.EndsWith(".dll")));

            if (Preload) this.Preload();
        }

        private void Preload()
        {
            foreach (String ManifestResourceName in ManifestResourceNames)
            { Load(ManifestResourceName); }
        }

        public Assembly ResolveAssembly(Object sender, ResolveEventArgs e)
        {
            try
            {
                AssemblyName ResourceAssemblyName = new AssemblyName(e.Name);
                String ResourceName = ManifestResourceNames.FirstOrDefault(_ => _.Contains(ResourceAssemblyName.Name));

                if (!Equals(ResourceName, null))
                {
                    Load(ResourceName);
                    if (EmbeddedDLLs.ContainsKey(ResourceName))
                        return EmbeddedDLLs[ResourceName];
                }
            }
            catch (Exception ex)
            { throw new DynamicLinkLibraryException("Error loading DynamicLinkLibrary.", ex); }
            return null;
        }

        private void Load(String ResourceName)
        {
            if (!EmbeddedDLLs.ContainsKey(ResourceName))
            {
                using (Stream stream = EmbeddedAssembly.GetManifestResourceStream(ResourceName))
                {
                    if (!Equals(stream, null))
                    {
                        Byte[] rawAssembly = new Byte[stream.Length];
                        stream.Read(rawAssembly, 0, rawAssembly.Length);
                        EmbeddedDLLs.Add(ResourceName, Assembly.Load(rawAssembly));
                        rawAssembly = null;
                    }
                }
            }
        }

        [Serializable]
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
