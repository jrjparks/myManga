using myManga_App.ViewModels.Objects.Cache.MangaCacheObjectViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
    }
}
