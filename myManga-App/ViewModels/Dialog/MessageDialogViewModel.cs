using System;
using System.Windows;

namespace myManga_App.ViewModels.Dialog
{
    public class MessageDialogViewModel : DialogViewModel
    {
        public MessageDialogViewModel()
            : base()
        {

        }

        #region Message Properties
        private static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached(
            "Title",
            typeof(String),
            typeof(AuthenticationDialogViewModel),
            new PropertyMetadata(String.Empty));

        public String Title
        {
            get { return (String)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        private static readonly DependencyProperty MessageProperty = DependencyProperty.RegisterAttached(
            "Message",
            typeof(String),
            typeof(AuthenticationDialogViewModel),
            null);

        public String Message
        {
            get { return (String)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }
        #endregion
    }
}
