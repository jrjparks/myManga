using System;
using System.Communication;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace myManga_App.ViewModels.Dialog
{
    public abstract class DialogViewModel : DependencyObject
    {
        protected readonly App App = App.Current as App;

        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging([CallerMemberName] String caller = null)
        { if (PropertyChanging != null) PropertyChanging(this, new PropertyChangingEventArgs(caller)); }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] String caller = null)
        { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(caller)); }

        protected virtual Boolean SetProperty<T>(ref T storage, T value, [CallerMemberName] String caller = null)
        {
            if (Object.Equals(storage, value)) return false;

            this.OnPropertyChanging(caller);
            storage = value;
            this.OnPropertyChanged(caller);

            return true;
        }
        #endregion

        private Boolean? _IsInDesignMode = null;
        public Boolean IsInDesignMode { get { return (Boolean)(_IsInDesignMode ?? (_IsInDesignMode = DesignerProperties.GetIsInDesignMode(this))); } }

        #region ShowDialog
        private static readonly DependencyProperty IsShownDialogProperty = DependencyProperty.RegisterAttached(
            "IsShown",
            typeof(Visibility),
            typeof(DialogViewModel),
            new PropertyMetadata(Visibility.Collapsed));

        public Visibility IsShown
        {
            get { return (Visibility)GetValue(IsShownDialogProperty); }
            protected set { SetValue(IsShownDialogProperty, value); }
        }
        #endregion

        #region HideMode
        private static readonly DependencyProperty HideModeProperty = DependencyProperty.RegisterAttached(
            "HideMode",
            typeof(Visibility),
            typeof(DialogViewModel),
            new PropertyMetadata(Visibility.Collapsed, OnHideModeChanged));

        public Visibility HideMode
        {
            get { return (Visibility)GetValue(HideModeProperty); }
            protected set { SetValue(HideModeProperty, value); }
        }

        private static void OnHideModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DialogViewModel control = d as DialogViewModel;
            if (!Equals(control.IsShown, Visibility.Visible))
            { control.IsShown = (Visibility)e.NewValue; }
        }
        #endregion

        #region Show Dialog Command
        protected DelegateCommand showDialogCommand;
        public ICommand ShowDialogCommand
        { get { return showDialogCommand ?? (showDialogCommand = new DelegateCommand(ShowDialog, () => !Equals(IsShown, Visibility.Visible))); } }

        public void ShowDialog()
        { IsShown = Visibility.Visible; }
        #endregion

        #region Hide Dialog Command
        protected DelegateCommand hideDialogCommand;
        public ICommand HideDialogCommand
        { get { return hideDialogCommand ?? (hideDialogCommand = new DelegateCommand(HideDialog, () => !Equals(IsShown, HideMode))); } }

        public void HideDialog()
        { IsShown = HideMode; }
        #endregion

        #region OK Authenticate
        protected DelegateCommand okCommand;
        public ICommand OKCommand
        { get { return okCommand ?? (okCommand = new DelegateCommand(OK, CanOK)); } }

        protected virtual Boolean CanOK()
        { return true; }

        protected virtual void OK()
        { HideDialog(); }
        #endregion

        #region Cancel Authenticate
        protected DelegateCommand cancelCommand;
        public ICommand CancelCommand
        { get { return cancelCommand ?? (cancelCommand = new DelegateCommand(Cancel, CanCancel)); } }

        protected virtual Boolean CanCancel()
        { return true; }

        protected virtual void Cancel()
        { HideDialog(); }
        #endregion

        public DialogViewModel()
            : base()
        {
        }
    }
}
