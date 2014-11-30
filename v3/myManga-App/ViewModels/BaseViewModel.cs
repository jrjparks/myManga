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
        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging([CallerMemberName] String caller = "")
        { if (PropertyChanging != null)PropertyChanging(this, new PropertyChangingEventArgs(caller)); }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] String caller = "")
        { if (PropertyChanged != null)PropertyChanged(this, new PropertyChangedEventArgs(caller)); }
        #endregion

        public virtual void Dispose() { }
    }
}
