using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Core.IO;
using myMangaSiteExtension.Enums;

namespace myMangaSiteExtension.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class SearchRequestObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
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
        protected String url;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String referer;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String request_content;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected SearchMethod method;
        #endregion

        #region Public
        [XmlAttribute]
        public String Url
        {
            get { return url; }
            set
            {
                OnPropertyChanging();
                url = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public String Referer
        {
            get { return referer; }
            set
            {
                OnPropertyChanging();
                referer = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public String RequestContent
        {
            get { return request_content; }
            set
            {
                OnPropertyChanging();
                request_content = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public SearchMethod Method
        {
            get { return method; }
            set
            {
                OnPropertyChanging();
                method = value;
                OnPropertyChanged();
            }
        }

        public SearchRequestObject() : base() { }
        public SearchRequestObject(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion
    }
}
