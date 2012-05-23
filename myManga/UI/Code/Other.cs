using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Data;
using System.IO;

namespace myManga
{
    public class ListItem
    {
        public Object Display { get; set; }
        public Object Value { get; set; }
    }

    public class ListQueueItem : Manga.NotifyPropChangeBase
    {
        private Guid _ID { get; set; }
        private String _Text { get; set; }
        private String _Queue { get; set; }
        private Int32 _Progress { get; set; }

        public Guid ID
        {
            get { return _ID; }
            set { _ID = value; OnPropertyChanged("ID"); }
        }
        public String Text
        {
            get { return _Text; }
            set { _Text = value; OnPropertyChanged("Text"); }
        }
        public String Queue
        {
            get { return _Queue; }
            set { _Queue = value; OnPropertyChanged("Queue"); }
        }
        public Int32 Progress
        {
            get { return _Progress; }
            set { _Progress = value; OnPropertyChanged("Progress"); }
        }

        public String ID_Text { get { return ID.ToString(); } }
    }
}
