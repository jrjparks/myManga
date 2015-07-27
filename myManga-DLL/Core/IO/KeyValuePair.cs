using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Core.IO
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class KeyValuePair<K, V> : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging([CallerMemberName] String caller = "")
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(caller));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] String caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
        #endregion

        #region Protected
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected K key;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected V val;
        #endregion

        #region Public
        [XmlAttribute]
        public K Key
        {
            get { return key; }
            set
            {
                OnPropertyChanging();
                key = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public V Value
        {
            get { return val; }
            set
            {
                OnPropertyChanging();
                val = value;
                OnPropertyChanged();
            }
        }

        public KeyValuePair() : base() { }
        public KeyValuePair(K Key = default(K), V Value = default(V))
            : base()
        {
            this.Key = Key;
            this.Value = Value;
        }
        public KeyValuePair(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion
    }
}
