using System;
using BakaBox.MVVM;
using Manga.Plugin;

namespace myManga.Models
{
    public class PluginInfoModel : ModelBase
    {
        public PluginInfoModel() { }
        public PluginInfoModel(String Name) 
        { this.Name = Name; }
        public PluginInfoModel(String Name, String Author)
        {
            this.Name = Name;
            this.Author = Author;
        }
        public PluginInfoModel(String Name, String Author, SupportedMethods SupportedMethods)
        {
            this.Name = Name;
            this.Author = Author;
            this.SupportedMethods = SupportedMethods;
        }
        public PluginInfoModel(PluginInfoModel PluginInfoModel)
        {
            this.Name = PluginInfoModel.Name;
            this.Author = PluginInfoModel.Author;
            this.SupportedMethods = PluginInfoModel.SupportedMethods;
        }

        private String _Name;
        public String Name
        {
            get { return _Name; }
            set { _Name = value; OnPropertyChanged("Name"); }
        }

        private String _Author;
        public String Author
        {
            get { return _Author; }
            set { _Author = value; OnPropertyChanged("Author"); }
        }

        private SupportedMethods _SupportedMethods;
        public SupportedMethods SupportedMethods
        {
            get { return _SupportedMethods; }
            set { _SupportedMethods = value; OnPropertyChanged("SupportedMethods"); }
        }
    }
}
