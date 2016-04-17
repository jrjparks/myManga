using myManga_App.Objects.Cache;
using myManga_App.ViewModels.Objects.Cache.MangaCacheObjectViewModels;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace myManga_App.ViewModels.Dialog
{
    public class MangaCacheObjectDialogViewModel : DialogViewModel
    {
        #region MangaCacheObjectDetail
        public static readonly DependencyPropertyKey MangaCacheObjectDetailPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "MangaCacheObjectDetail",
            typeof(MangaCacheObjectDetailViewModel),
            typeof(MangaCacheObjectDialogViewModel),
            null);
        private static readonly DependencyProperty MangaCacheObjectDetailProperty = MangaCacheObjectDetailPropertyKey.DependencyProperty;

        public MangaCacheObjectDetailViewModel MangaCacheObjectDetail
        {
            get { return (MangaCacheObjectDetailViewModel)GetValue(MangaCacheObjectDetailProperty); }
            set { SetValue(MangaCacheObjectDetailPropertyKey, value); }
        }
        #endregion

        #region Refresh Command
        private DelegateCommand<MangaCacheObject> refreshCommand;
        public ICommand RefreshCommand
        { get { return refreshCommand ?? (refreshCommand = new DelegateCommand<MangaCacheObject>(Refresh, CanRefresh)); } }

        private Boolean CanRefresh(MangaCacheObject MangaCacheObject)
        {
            if (Equals(MangaCacheObject, null)) return false;
            return true;
        }

        private void Refresh(MangaCacheObject MangaCacheObject)
        { App.ContentDownloadManager.Download(MangaCacheObject.MangaObject, true, MangaCacheObject.DownloadProgressReporter); }
        #endregion

        #region Delete Command
        private DelegateCommand<MangaCacheObject> deleteCommand;
        public ICommand DeleteCommand
        { get { return deleteCommand ?? (deleteCommand = new DelegateCommand<MangaCacheObject>(DeleteAsync, CanDeleteAsync)); } }

        private Boolean CanDeleteAsync(MangaCacheObject MangaCacheObject)
        {
            if (Equals(MangaCacheObject, null)) return false;
            return true;
        }

        private void DeleteAsync(MangaCacheObject MangaCacheObject)
        {
            String SavePath = Path.Combine(App.CORE.MANGA_ARCHIVE_DIRECTORY, MangaCacheObject.MangaObject.MangaArchiveName(App.CORE.MANGA_ARCHIVE_EXTENSION));
            MessageBoxResult msgboxResult = MessageBox.Show(String.Format("Are you sure you wish to delete \"{0}\"?", MangaCacheObject.MangaObject.Name), "Delete Manga?", MessageBoxButton.YesNo);
            if (Equals(msgboxResult, MessageBoxResult.Yes))
                File.Delete(SavePath);
        }
        #endregion

        #region Edit Command
        private DelegateCommand<MangaCacheObject> editCommand;
        public ICommand EditCommand
        { get { return editCommand ?? (editCommand = new DelegateCommand<MangaCacheObject>(EditAsync, CanEditAsync)); } }

        private Boolean CanEditAsync(MangaCacheObject MangaCacheObject)
        {
            if (Equals(MangaCacheObject, null)) return false;
            return false;
            //return true;
        }

        private void EditAsync(MangaCacheObject MangaCacheObject)
        {
        }
        #endregion
    }
}
