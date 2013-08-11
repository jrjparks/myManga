using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakaBox.Controls
{
    public class StreamImage : Image
    {
        public static DependencyProperty SourceStringProperty = DependencyProperty.Register("SourceString", typeof(String), typeof(StreamImage));
        public static DependencyProperty SourceStreamProperty = DependencyProperty.Register("SourceStream", typeof(Stream), typeof(StreamImage));
        public static DependencyProperty AutoUpdateProperty = DependencyProperty.Register("AutoUpdate", typeof(Boolean), typeof(StreamImage));

        [Description("Set the path for a local image."), Category("Common")]
        public String SourceString
        {
            get { return GetValue(SourceStringProperty) as String; }
            set
            {
                SetValue(SourceStringProperty, value);

                imageBuffer.Image = value.ImageSourceFromPath();
                imageBuffer.Size = new Size(imageBuffer.Image.Width, imageBuffer.Image.Height);

                if (AutoUpdate)
                    UpdateImage();
            }
        }
        [Description("Set image stream."), Category("Common"), DefaultValue(true)]
        public Stream SourceStream
        {
            get { return GetValue(SourceStreamProperty) as System.IO.Stream; }
            set
            {
                SetValue(SourceStreamProperty, value);
                imageBuffer = value.StreamToBitmapImage();                

                if (AutoUpdate)
                    UpdateImage();
            }
        }
        [Description("Set whether to auto-update the image. Turn of if you use image transitioning"), Category("Common")]
        public Boolean AutoUpdate
        {
            get { return (Boolean)GetValue(AutoUpdateProperty); }
            set { SetValue(AutoUpdateProperty, value); }
        }

        private ImageData imageBuffer { get; set; }
        private new Stretch Stretch { get; set; }

        public StreamImage()
            : base()
        {
            AutoUpdate = false;
            Stretch = Stretch.None;
        }

        public void UpdateImage()
        {
            try
            {
                Width = imageBuffer.Size.Width;
                Height = imageBuffer.Size.Height;
                Source = imageBuffer.Image;
            }
            catch { }
        }
    }

    internal static class ImageSourceExtensions
    {
        public static ImageSource ImageSourceFromPath(this string path)
        {
            if (!File.Exists(path)) return null;

            //create new stream and create bitmap frame
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read);
            //load the image now so we can immediately dispose of the stream
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            //clean up the stream to avoid file access exceptions when attempting to delete images
            bitmapImage.StreamSource.Dispose();

            return bitmapImage;
        }
    }

    public class ImageData
    {
        public ImageSource Image { get; set; }
        public Size Size { get; set; }

        public ImageData() { }
        public ImageData(ImageSource ImageSource, Size Size)
        {
            this.Image = ImageSource;
            this.Size = Size;
        }
    }
    public static class ImageTools
    {
        public static ImageData StreamToBitmapImage(this Stream Stream)
        {
            Stream.Seek(0, SeekOrigin.Begin);
            if (Stream != null && Stream.Length > 0 && Stream.Position < Stream.Length)
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = Stream;

                //load the image now so we can immediately dispose of the stream
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                //clean up the stream to avoid file access exceptions when attempting to delete images
                bitmapImage.StreamSource.Dispose();
                Stream.Close();

                return new ImageData(bitmapImage, new Size(bitmapImage.PixelWidth, bitmapImage.PixelHeight));
            }
            return null;
        }
    }
}
