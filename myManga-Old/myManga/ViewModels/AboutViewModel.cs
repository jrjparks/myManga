using System;
using System.Collections.Generic;
using System.Reflection;
using Manga.Plugin;
using myManga.Models;
using Manga.Manager;

namespace myManga.ViewModels
{
    public sealed class AboutViewModel : ViewModelBase
    {
        #region Variables
        private List<PluginInfoModel> _PluginInfoModelCollection;
        public List<PluginInfoModel> PluginInfoModelCollection
        {
            get { return _PluginInfoModelCollection; }
            set { _PluginInfoModelCollection = value; OnPropertyChanged("PluginInfoModelCollection"); }
        }
        #endregion

        public AboutViewModel()
        {
            PluginInfoModelCollection = new List<PluginInfoModel>(Global_IMangaPluginCollection.Instance.Plugins.Count);
            foreach (IMangaPlugin _Plugin in Global_IMangaPluginCollection.Instance.Plugins)
            {
                MemberInfo _Info = _Plugin.GetType();
                Object[] _Attribs = _Info.GetCustomAttributes(typeof(PluginAuthorAttribute), true);
                PluginInfoModelCollection.Add(
                    new PluginInfoModel()
                    {
                        Name = _Plugin.SiteName,
                        SupportedMethods = _Plugin.SupportedMethods,
                        Author = (_Attribs.Length > 0 && _Attribs[0] is PluginAuthorAttribute) ? 
                            (_Attribs[0] as PluginAuthorAttribute).ToString() : "Unknown Author"
                    });
            }
        }

        #region Assembly Attribute Accessors
        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion
    }
}
