using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BakaBox.Controls;

namespace PluginTester
{
    /// <summary>
    /// Interaction logic for PageView.xaml
    /// </summary>
    public partial class PageView : Window
    {
        public PageView()
        { InitializeComponent(); }

        public void SetImage(String ImageSource)
        { pageImage.SourceString = ImageSource; }
    }
}
