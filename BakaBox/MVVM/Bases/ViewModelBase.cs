using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace BakaBox.MVVM
{
    /// <summary>
    /// Base class for all ViewModel classes in the application.
    /// It provides support for property change notifications.
    /// This class is abstract.
    /// </summary>
    [DebuggerStepThrough]
    public abstract class ViewModelBase : INotifyPropertyChanged, INotifyPropertyChanging, IDisposable
    {
        #region Constructor

        protected ViewModelBase()
        { }

        #endregion // Constructor

        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(String propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                String msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                else
                    Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        #endregion // Debugging Aides

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(String Name)
        {
            this.VerifyPropertyName(Name);

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging(String Name)
        {
            this.VerifyPropertyName(Name);

            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(Name));
        }

        #endregion // INotifyPropertyChanged Members

        #region IDisposable Members

        /// <summary>
        /// Invoked when this object is being removed from the application
        /// and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        { this.OnDispose(); }

        /// <summary>
        /// Child classes can override this method to perform 
        /// clean-up logic, such as removing event handlers.
        /// </summary>
        protected virtual void OnDispose()
        { }

        #endregion // IDisposable Members

        #region ViewModelComs

        public event RelayMessage ViewModelMessage;
        protected void SendViewModelMessage(Object Message)
        { SendViewModelMessage(this, Message); }
        protected void SendViewModelMessage(Object Sender, Object Message)
        {
            if (ViewModelMessage != null)
                ViewModelMessage(Sender, Message);
        }

        #endregion

        protected Boolean IsInDesignerMode
        { get { return DesignerProperties.GetIsInDesignMode(new DependencyObject()); } }
    }
}
