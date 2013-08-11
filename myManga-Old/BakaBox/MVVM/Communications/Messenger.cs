using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BakaBox.MVVM.Communications
{
    public class Messenger
    {
        #region Instance
        private static Messenger _Instance;
        private static Object SyncObject = new Object();
        public static Messenger Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (SyncObject)
                    {
                        if (_Instance == null)
                            _Instance = new Messenger();
                    }
                }
                return _Instance;
            }
        }
        #endregion

        private Messenger()
        { }

        public delegate void BroadcastMessageEvent(Object Sender, Object Data);
        public event BroadcastMessageEvent BroadcastMessage;
        public void SendBroadcastMessage(Object Message)
        { SendBroadcastMessage(this, Message); }
        public void SendBroadcastMessage(Object Sender, Object Message)
        {
            if (BroadcastMessage != null)
                BroadcastMessage(Sender, Message);
        }
    }
}
