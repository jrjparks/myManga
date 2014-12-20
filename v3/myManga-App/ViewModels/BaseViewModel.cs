using myManga_App.IO.ViewModel;
using myManga_App.Objects.MVVM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace myManga_App.ViewModels
{
    public abstract class BaseViewModel : DependencyObject, IDisposable, INotifyPropertyChanging, INotifyPropertyChanged
    {
        protected readonly App App = App.Current as App;

        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging([CallerMemberName] String caller = null)
        { if (PropertyChanging != null)PropertyChanging(this, new PropertyChangingEventArgs(caller)); }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] String caller = null)
        { if (PropertyChanged != null)PropertyChanged(this, new PropertyChangedEventArgs(caller)); }

        protected virtual Boolean SetProperty<T>(ref T storage, T value, [CallerMemberName] String caller = null)
        {
            if (Object.Equals(storage, value)) return false;

            this.OnPropertyChanging(caller);
            storage = value;
            this.OnPropertyChanged(caller);

            return true;
        }
        #endregion

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

        private Boolean? _IsInDesignMode = null;
        public Boolean IsInDesignMode { get { return (Boolean)(_IsInDesignMode ?? (_IsInDesignMode = DesignerProperties.GetIsInDesignMode(this))); } }

        public void PullFocus()
        { Messenger.Default.Send(this, "FocusRequest"); }

        protected BaseViewModel(Boolean SupportsViewTypeChange = false)
        {
            if (!IsInDesignMode)
                if (this.SupportsViewTypeChange = SupportsViewTypeChange)
                    try { this.ViewType = App.UserConfig.ViewTypes.FirstOrDefault(vt => vt.ViewModelName.Equals(this.GetType().Name)).ViewType; }
                    catch { App.UserConfig.ViewTypes.Add(new SerializableViewModelViewType() { ViewModelName = this.GetType().Name, ViewType = this.ViewType = ViewModelViewType.Normal }); }
        }

        private void SaveViewType()
        {
            if (!IsInDesignMode && this.SupportsViewTypeChange)
            {
                SerializableViewModelViewType CurrentSerializableViewModelViewType = App.UserConfig.ViewTypes.FirstOrDefault(vt => vt.ViewModelName.Equals(this.GetType().Name));
                if (CurrentSerializableViewModelViewType != null) CurrentSerializableViewModelViewType.ViewType = this.ViewType;
                App.SaveUserConfig();
            }
        }

        public virtual void Dispose() { Messenger.Default.Send(this, "Dispose"); }
    }
}
