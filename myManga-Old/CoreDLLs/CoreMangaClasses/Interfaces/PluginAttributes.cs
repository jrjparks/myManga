using System;
using System.Diagnostics;

namespace Manga.Plugin
{
    [DebuggerStepThrough]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class MangaPluginAttribute : Attribute { }
    
    [DebuggerStepThrough]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
    public sealed class PluginSiteAttribute : Attribute
    {
        private String _Site { get; set; }

        public PluginSiteAttribute(String Site)
        {
            _Site = Site;
        }

        public override string ToString()
        {
            return _Site;
        }
    }

    [DebuggerStepThrough]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PluginAuthorAttribute : Attribute
    {
        private String _Author { get; set; }

        public PluginAuthorAttribute(String Author)
        {
            _Author = Author;
        }

        public override string ToString()
        {
            return _Author;
        }
    }

    [DebuggerStepThrough]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PluginVersionAttribute : Attribute
    {
        private String _Version { get; set; }

        public PluginVersionAttribute(String Version)
        {
            _Version = Version;
        }

        public override string ToString()
        {
            return _Version;
        }
    }
}
