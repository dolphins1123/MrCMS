using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using MrCMS.Entities.Documents.Media;
using MrCMS.Models;
using NHibernate;

namespace MrCMS.Services
{
    public interface IImageProcessor
    {
        List<ImageSize> GetImageSizes();
        MediaFile GetImage(string imageUrl);

        void SetFileDimensions(MediaFile mediaFile, Stream stream);
        void SaveResizedImage(MediaFile file, Size size, byte[] fileBytes, string filePath);
    }

    public class ImageProcessor : IImageProcessor
    {
        private readonly ISession _session;

        public ImageProcessor(ISession session)
        {
            _session = session;
        }

        public List<ImageSize> GetImageSizes()
        {
            throw new NotImplementedException();
        }

        public MediaFile GetImage(string imageUrl)
        {
            if (imageUrl.StartsWith("/"))
            {
                imageUrl = imageUrl.Substring(1);
            }
            if (IsResized(imageUrl))
            {
                var resizePart = GetResizePart(imageUrl);
                var lastIndexOf = imageUrl.LastIndexOf(resizePart);
                imageUrl = imageUrl.Remove(lastIndexOf - 1, resizePart.Length + 1);
            }
            var fileByLocation =
                _session.QueryOver<MediaFile>()
                        .Where(file => file.FileLocation == imageUrl)
                        .Take(1)
                        .Cacheable()
                        .SingleOrDefault();

            return fileByLocation ?? null;
        }

        private bool IsResized(string imageUrl)
        {
            var resizePart = GetResizePart(imageUrl);
            if (resizePart == null) return false;

            int val;
            return new List<char> { 'w', 'h' }.Contains(resizePart[0]) && Int32.TryParse(resizePart.Substring(1), out val);
        }

        private static string GetResizePart(string imageUrl)
        {
            if (imageUrl.LastIndexOf('_') == -1 || imageUrl.LastIndexOf('.') == -1)
                return null;

            var startIndex = imageUrl.LastIndexOf('_') + 1;
            var length = imageUrl.LastIndexOf('.') - startIndex;
            if (length < 2) return null;
            var resizePart = imageUrl.Substring(startIndex, length);
            return resizePart;
        }

        public void SetFileDimensions(MediaFile mediaFile, Stream stream)
        {
            using (var b = new Bitmap(stream))
            {
                mediaFile.Width = b.Size.Width;
                mediaFile.Height = b.Size.Height;
            }
        }

        public void SaveResizedImage(MediaFile file, Size size, byte[] fileBytes, string filePath)
        {


            using (var stream = new MemoryStream(fileBytes))
            {
                using (var b = new Bitmap(stream))
                {
                    var newSize = CalculateDimensions(b.Size, size);

                    if (newSize.Width < 1)
                        newSize.Width = 1;
                    if (newSize.Height < 1)
                        newSize.Height = 1;

                    using (var newBitMap = new Bitmap(newSize.Width, newSize.Height))
                    {
                        var g = Graphics.FromImage(newBitMap);
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawImage(b, 0, 0, newSize.Width, newSize.Height);
                        var ep = new EncoderParameters();
                        ep.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
                        ImageCodecInfo ici = GetImageCodecInfoFromExtension(file.FileExtension)
                                             ?? GetImageCodecInfoFromMimeType("image/jpeg");
                        newBitMap.Save(filePath, ici, ep);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the first ImageCodecInfo instance with the specified mime type.
        /// </summary>
        /// <param name="mimeType">Mime type</param>
        /// <returns>ImageCodecInfo</returns>
        private ImageCodecInfo GetImageCodecInfoFromMimeType(string mimeType)
        {
            var info = ImageCodecInfo.GetImageEncoders();
            return info.FirstOrDefault(ici => ici.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns the first ImageCodecInfo instance with the specified extension.
        /// </summary>
        /// <param name="fileExt">File extension</param>
        /// <returns>ImageCodecInfo</returns>
        private ImageCodecInfo GetImageCodecInfoFromExtension(string fileExt)
        {
            fileExt = fileExt.TrimStart(".".ToCharArray()).ToLower().Trim();
            switch (fileExt)
            {
                case "jpg":
                case "jpeg":
                    return GetImageCodecInfoFromMimeType("image/jpeg");
                case "png":
                    return GetImageCodecInfoFromMimeType("image/png");
                case "gif":
                    //use png codec for gif to preserve transparency
                    //return GetImageCodecInfoFromMimeType("image/gif");
                    return GetImageCodecInfoFromMimeType("image/png");
                default:
                    return GetImageCodecInfoFromMimeType("image/jpeg");
            }
        }


        public static Size CalculateDimensions(Size originalSize, Size targetSize)
        {
            // If the target image is bigger than the source
            if (!RequiresResize(originalSize, targetSize))
            {
                return originalSize;
            }

            double ratio = 0;

            // What ratio should we resize it by
            var widthRatio = originalSize.Width / (double)targetSize.Width;
            var heightRatio = originalSize.Height / (double)targetSize.Height;
            ratio = widthRatio > heightRatio
                        ? originalSize.Width / (double)targetSize.Width
                        : originalSize.Height / (double)targetSize.Height;

            var resizeWidth = Math.Floor(originalSize.Width / ratio);

            var resizeHeight = Math.Floor(originalSize.Height / ratio);

            return new Size((int)resizeWidth, (int)resizeHeight);
        }

        public static bool RequiresResize(Size originalSize, Size targetSize)
        {
            return targetSize.Width < originalSize.Width || targetSize.Height < originalSize.Height;
        }

        public static List<ImageSize> ImageSizes
        {
            get
            {
                return new List<ImageSize>
                           {
                               new ImageSize {Size = new Size(480, 640), Name = "Large - Portrait"},
                               new ImageSize {Size = new Size(640, 480), Name = "Large - Landscape"},
                               new ImageSize {Size = new Size(240, 320), Name = "Medium - Portrait"},
                               new ImageSize {Size = new Size(320, 240), Name = "Medium - Landscape"},
                               new ImageSize {Size = new Size(75, 100), Name = "Small - Portrait"},
                               new ImageSize {Size = new Size(100, 75), Name = "Small - Landscape"},
                               new ImageSize {Size = new Size(64, 64), Name = "Thumbnail"}
                           };
            }
        }
    }
}