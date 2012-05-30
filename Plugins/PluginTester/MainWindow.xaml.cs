using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Manga.Archive;
using Manga.Core;
using Manga.Info;
using Manga.Plugin;
using BakaBox.MVVM;
using BakaBox.DLL;
using BakaBox.Extensions;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.IO;
using Manga.Manager;
using System.Diagnostics;

namespace PluginTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Variables
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(String Name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        
        private IMangaPlugin _SelectedPlugin;
        public IMangaPlugin SelectedPlugin
        {
            get { return _SelectedPlugin; }
            set
            {
                if (_SelectedPlugin != null)
                    _SelectedPlugin.ProgressChanged -= _SelectedPlugin_ProgressChanged;
                _SelectedPlugin = value;
                _SelectedPlugin.ProgressChanged += _SelectedPlugin_ProgressChanged;
                OnPropertyChanged("SelectedPlugin");
            }
        }

        void _SelectedPlugin_ProgressChanged(object Sender, int _Progress, object Data)
        {
            Progress = _Progress;
        }
        private Int32 _Progress;
        public Int32 Progress
        {
            get { return _Progress; }
            set
            {
                _Progress = value;
                OnPropertyChanged("Progress");
            }
        }

        private String _TestURL;
        public String TestURL
        {
            get { return _TestURL; }
            set
            {
                _TestURL = value;
                OnPropertyChanged("TestURL");
            }
        }

        private Int32 _SelectedTab;
        public Int32 SelectedTab
        {
            get { return _SelectedTab; }
            set
            {
                _SelectedTab = value;
                OnPropertyChanged("SelectedTab");
            }
        }

        private MangaInfo _MangInfo;
        public MangaInfo MangInfo
        {
            get { return _MangInfo; }
            set
            {
                _MangInfo = value;
                OnPropertyChanged("MangInfo");
            }
        }

        private MangaArchiveInfo _MangArch;
        public MangaArchiveInfo MangArch
        {
            get { return _MangArch; }
            set
            {
                _MangArch = value;
                OnPropertyChanged("MangArch");
            }
        }
        
        private SearchInfoCollection _SearchResults;
        public SearchInfoCollection SearchResults
        {
            get { return _SearchResults; }
            set
            {
                _SearchResults = value;
                OnPropertyChanged("SearchResults");
            }
        }
        #endregion

        private readonly BackgroundWorker _Downloader;

        public MainWindow()
        {
            LoadPlugins();
            _Downloader = new BackgroundWorker();
            _Downloader.DoWork += _Downloader_DoWork;
            _Downloader.RunWorkerCompleted += _Downloader_RunWorkerCompleted;

            InitializeComponent();
        }

        void _Downloader_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                String Text = (e.Argument as String) ?? String.Empty;
                switch (SelectedTab)
                {
                    default: break;

                    case 0:
                        SelectedPlugin = Global_IMangaPluginCollection.Instance.Plugins.PluginToUse_SiteUrl(Text);
                        if (SelectedPlugin.SupportedMethods.Has(SupportedMethods.MangaInfo))
                        {
                            MangaInfo MangaInfo = SelectedPlugin.LoadMangaInformation(Text);
                            Stream CoverImage = default(Stream);
                            if (SelectedPlugin.SupportedMethods.Has(SupportedMethods.CoverImage))
                                CoverImage = SelectedPlugin.GetCoverImage(MangaInfo).CoverStream;
                            e.Result = new Object[] { 0, MangaInfo, CoverImage };
                        }
                        else
                            e.Result = "Downloading MangaInfo not supported.";
                        break;

                    case 1:
                        SelectedPlugin = Global_IMangaPluginCollection.Instance.Plugins.PluginToUse_SiteUrl(Text);
                        if (SelectedPlugin.SupportedMethods.Has(SupportedMethods.ChapterInfo))
                            e.Result = new Object[] { 1, SelectedPlugin.LoadChapterInformation(Text) };
                        else
                            e.Result = "Downloading ChapterInfo not supported.";
                        break;

                    case 2:
                        if (SelectedPlugin.SupportedMethods.Has(SupportedMethods.Search))
                            e.Result = new Object[] { 2, SelectedPlugin.Search(Text, 25) };
                        else
                            e.Result = "Search not supported.";
                        break;
                }
            }
            catch(Exception ex) { MessageBox.Show(ex.Message); }
        }

        void _Downloader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (e.Result is Object[])
                {
                    Object[] Data = e.Result as Object[];
                    switch ((Int32)Data[0])
                    {
                        default: break;

                        case 0:
                            MangInfo = Data[1] as MangaInfo;
                            Cover.SourceStream = Data[2] as Stream;
                            break;

                        case 1:
                            MangArch = Data[1] as MangaArchiveInfo;
                            break;

                        case 2:
                            SearchResults = Data[1] as SearchInfoCollection;
                            break;
                    }
                }
                else if (e.Result is String)
                {
                    MessageBox.Show(e.Result as String);
                }
            }
            catch { }
            Progress = 100;
        }

        private void LoadPlugins()
        {
            try
            {
                foreach (IMangaPlugin _Plugin in PluginLoader<IMangaPlugin>.LoadPluginDirectory(
                     System.IO.Path.Combine(Environment.CurrentDirectory, "Plugins"),
                     "*.manga.dll"))
                {
                    if (!Global_IMangaPluginCollection.Instance.Plugins.Contains(_Plugin.SiteName))
                        Global_IMangaPluginCollection.Instance.Plugins.Add(_Plugin);
                }
            }
            catch { }
            if (Global_IMangaPluginCollection.Instance.Plugins.Count == 0)
            {
                MessageBox.Show("ERROR! No plugins detected.\n\nFile name format:\n*.manga.dll", "Err!", MessageBoxButton.OK, MessageBoxImage.Error);
                App.Current.Shutdown();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Content as String)
            {
                default: break;

                case "Test":
                    TextBoxDoWork(TestURL);
                    break;
            }
        }

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Object Item = (sender as ListBox).SelectedItem;
            if (Item is ChapterEntry)
            {
                TestURL = (Item as ChapterEntry).UrlLink;
                SelectedTab = 1;
                if (!_Downloader.IsBusy)
                    _Downloader.RunWorkerAsync(TestURL);
            }
            else if (Item is SearchInfo)
            {
                TestURL = (Item as SearchInfo).InformationLocation;
                SelectedTab = 0;
                if (!_Downloader.IsBusy)
                    _Downloader.RunWorkerAsync(TestURL);
            }
            else if(Item is PageEntry)
            {
                Process.Start((Item as PageEntry).LocationInfo.FullOnlinePath);
            }
        }

        private DelegateCommand<String> _TextBoxEnterKey;
        public ICommand TextBoxEnterKey
        {
            get
            {
                if (_TextBoxEnterKey == null)
                    _TextBoxEnterKey = new DelegateCommand<String>(TextBoxDoWork);
                return _TextBoxEnterKey;
            }
        }
        private void TextBoxDoWork(String Text)
        {
            SelectedPlugin = SelectedPlugin ?? Global_IMangaPluginCollection.Instance.Plugins[0];
            if (!Regex.IsMatch(Text, @"(?<Protocol>\w+):\/\/(?<Domain>[\w@][\w.:@]+)\/?[\w\.?=%&=\-@/$,]*"))
                SelectedTab = 2;
            if (!_Downloader.IsBusy)
                _Downloader.RunWorkerAsync(Text);
        }
    }
}
