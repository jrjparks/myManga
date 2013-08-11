using System;
using System.ComponentModel;
using System.Diagnostics;

namespace BakaBox.MVVM
{
    [DebuggerStepThrough]
    public abstract class ModelBase : INotifyPropertyChanged, INotifyPropertyChanging, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(String Name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }

        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging(String Name)
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(Name));
        }

        public event RelayMessage ModelMessage;
        protected void SendModelMessage(Object Message)
        { SendModelMessage(this, Message); }
        protected void SendModelMessage(Object Sender, Object Message)
        {
            if (ModelMessage != null)
                ModelMessage(Sender, Message);
        }

        #region IDisposable Members

        /// <summary>
        /// Invoked when this object is being removed from the application
        /// and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        {
            this.OnDispose();
        }

        /// <summary>
        /// Child classes can override this method to perform 
        /// clean-up logic, such as removing event handlers.
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        #endregion // IDisposable Members
    }
}
