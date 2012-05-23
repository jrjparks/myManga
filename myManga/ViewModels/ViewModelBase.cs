using System;
using System.ComponentModel;
using myManga.UI;
using System.Diagnostics;

namespace myManga.ViewModels
{
    [DebuggerStepThrough]
    public abstract class ViewModelBase : BakaBox.MVVM.ViewModelBase
    {
        public delegate void ViewModel_ToastNotification(Object _Sender, String _Message, TimeSpan _Time);
        public event ViewModel_ToastNotification ViewModelToastNotification;
        protected void SendViewModelToastNotification(Object _Sender, String _Message)
        {
            SendViewModelToastNotification(_Sender, _Message, TimeSpan.FromMilliseconds(500));
        }
        protected void SendViewModelToastNotification(Object _Sender, String _Message, ToastNotification.DisplayLength DisplayLength)
        {
            SendViewModelToastNotification(_Sender, _Message, TimeSpan.FromMilliseconds((Double)DisplayLength));
        }
        protected void SendViewModelToastNotification(Object _Sender, String _Message, TimeSpan _Time)
        {
            if (ViewModelToastNotification != null)
                ViewModelToastNotification(_Sender, _Message, _Time);
        }
    }
}
