using myManga_App.Objects.MVVM;
using System;
using System.Communication;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace myManga_App.ViewModels
{
    public abstract class BaseViewModel : DependencyObject, IDisposable, INotifyPropertyChanging, INotifyPropertyChanged
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

        #region View Management

        #region ViewType
        private static readonly DependencyProperty ViewTypeProperty = DependencyProperty.RegisterAttached(
            "ViewType",
            typeof(ViewModelViewType),
            typeof(BaseViewModel),
            new PropertyMetadata(ViewModelViewType.Normal, OnViewTypeChenged));

        public ViewModelViewType ViewType
        {
            get { return (ViewModelViewType)GetValue(ViewTypeProperty); }
            protected set { SetValue(ViewTypeProperty, value); }
        }

        private static void OnViewTypeChenged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        { (d as BaseViewModel).SaveViewType(); }
        #endregion

        #region SupportsViewTypeChange
        private static readonly DependencyProperty SupportsViewTypeChangeProperty = DependencyProperty.RegisterAttached(
            "SupportsViewTypeChange",
            typeof(Boolean),
            typeof(BaseViewModel),
            new PropertyMetadata(false));

        public Boolean SupportsViewTypeChange
        {
            get { return (Boolean)GetValue(SupportsViewTypeChangeProperty); }
            private set { SetValue(SupportsViewTypeChangeProperty, value); }
        }
        #endregion

        private void SaveViewType()
        {
            if (!IsInDesignMode && SupportsViewTypeChange)
            {
                SerializableViewModelViewType CurrentSerializableViewModelViewType = App.UserConfiguration.ViewTypes.FirstOrDefault(vt => vt.ViewModelName.Equals(GetType().Name));
                if (CurrentSerializableViewModelViewType != null) CurrentSerializableViewModelViewType.ViewType = ViewType;
                App.SaveUserConfiguration();
            }
        }

        #endregion

        #region Focus Management

        #region Pull
        public void PullFocus() { Messenger.Instance.Send(this, "FocusRequest"); SubPullFocus(); }
        protected virtual void SubPullFocus() { }
        #endregion

        #region Lost
        public void LostFocus() { Messenger.Instance.Send(this, "LostFocusRequest"); SubLostFocus(); }
        protected virtual void SubLostFocus() { }
        #endregion

        #region Return
        public void ReturnFocus() { Messenger.Instance.Send(true, "PreviousFocusRequest"); SubReturnFocus(); }
        protected virtual void SubReturnFocus() { }
        #endregion

        #endregion

        private Boolean? _IsInDesignMode = null;
        public Boolean IsInDesignMode { get { return (Boolean)(_IsInDesignMode ?? (_IsInDesignMode = DesignerProperties.GetIsInDesignMode(this))); } }

        protected BaseViewModel(Boolean SupportsViewTypeChange = false)
        {
            if (!IsInDesignMode)
                if (this.SupportsViewTypeChange = SupportsViewTypeChange)
                    try { ViewType = App.UserConfiguration.ViewTypes.FirstOrDefault(vt => vt.ViewModelName.Equals(GetType().Name)).ViewType; }
                    catch { App.UserConfiguration.ViewTypes.Add(new SerializableViewModelViewType() { ViewModelName = GetType().Name, ViewType = ViewType = ViewModelViewType.Normal }); }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void SubDispose() { }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SubDispose();
                    Messenger.Instance.Send(this, "Dispose");
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        { Dispose(true); }
        #endregion
    }
}
