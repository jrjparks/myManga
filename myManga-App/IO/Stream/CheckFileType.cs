using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myManga_App.IO.StreamExtensions
{
    public static partial class ImageStreamExtensions
    {
        private static readonly Byte[] PNG = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly Byte[] JPG = { 0xFF, 0xD8, 0xFF };
        private static readonly Byte[] GIF = { 0x47, 0x49, 0x46, 0x38 };
        private static readonly Byte[] BMP = { 0x42, 0x4D };

        public enum ImageFormat
        {
            UNKNOWN = 0x0,
            PNG = 0x1,
            JPG = 0x2,
            GIF = 0x3,
            BMP = 0x4
        }

        public static ImageFormat CheckImageFileType(this Stream strm)
        {
            // Store the current Posision of the stream.
            Int64 sPos = strm.Position;
            strm.Seek(0, SeekOrigin.Begin);
            // Read stream header.
            Byte[] header = new Byte[10];
            strm.Read(header, 0, 10);
            // Reset the stream position.
            strm.Seek(sPos, SeekOrigin.Begin);

            if (PNG.SequenceEqual(header.Take(PNG.Length)))
                return ImageFormat.PNG;

            else if (JPG.SequenceEqual(header.Take(JPG.Length)))
                return ImageFormat.JPG;

            else if (GIF.SequenceEqual(header.Take(GIF.Length)))
                return ImageFormat.GIF;

            else if (BMP.SequenceEqual(header.Take(BMP.Length)))
                return ImageFormat.BMP;

            return ImageFormat.UNKNOWN;
        }

        public static async Task<ImageFormat> CheckImageFileTypeAsync(this Stream strm)
        {
            // Store the current Posision of the stream.
            Int64 sPos = strm.Position;
            strm.Seek(0, SeekOrigin.Begin);
            // Read stream header.
            Byte[] header = new Byte[10];
            await strm.ReadAsync(header, 0, 10);
            // Reset the stream position.
            strm.Seek(sPos, SeekOrigin.Begin);

            if (PNG.SequenceEqual(header.Take(PNG.Length)))
                return ImageFormat.PNG;

            else if (JPG.SequenceEqual(header.Take(JPG.Length)))
                return ImageFormat.JPG;

            else if (GIF.SequenceEqual(header.Take(GIF.Length)))
                return ImageFormat.GIF;

            else if (BMP.SequenceEqual(header.Take(BMP.Length)))
                return ImageFormat.BMP;

            return ImageFormat.UNKNOWN;
        }
    }
}
