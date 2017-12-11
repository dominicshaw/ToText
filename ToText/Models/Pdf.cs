using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using org.apache.pdfbox.pdmodel;
using org.apache.pdfbox.util;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace ToText.Models
{
    public class Pdf : IDisposable
    {
        private readonly FileInfo _location;
        private MemoryStream _stream;

        private bool _loaded;

        public Pdf(string location)
        {
            _location = new FileInfo(location);

            if (!_location.Exists)
                throw new FileNotFoundException("Could not find PDF in that location.", _location.FullName);
        }

        private async Task Load()
        {
            _stream?.Dispose();
            _stream = new MemoryStream();

            using (var file = new FileStream(_location.FullName, FileMode.Open, FileAccess.Read))
            {
                var bytes = new byte[file.Length];

                await file.ReadAsync(bytes, 0, (int)file.Length);
                await _stream.WriteAsync(bytes, 0, (int)file.Length);
            }

            _loaded = true;
        }

        private async Task<byte[]> GetImage(ImageFormat format, bool readAsOneImage = true)
        {
            if (!_loaded)
                await Load();

            return await Task.Run(() =>
            {
                _stream.Position = 0;

                if (readAsOneImage)
                {
                    var settings = new MagickReadSettings { Density = new Density(300) };

                    using (var images = new MagickImageCollection())
                    {
                        images.Read(_stream, settings);

                        using (var vertical = images.AppendVertically())
                        {
                            var path = $"pdf.{GetFilenameExtension(format)}";

                            vertical.Write(path); // todo handle with stream instead of file

                            return File.ReadAllBytes(path);
                        }
                    }
                }
                else
                {
                    var settings = new MagickReadSettings { Density = new Density(300, 300) };

                    var pages = new List<byte[]>();

                    using (var images = new MagickImageCollection())
                    {
                        images.Read(_stream, settings);

                        var page = 1;
                        foreach (var image in images.OfType<MagickImage>())
                        {
                            var path = $"pdf.{page}.{GetFilenameExtension(format)}";

                            image.Write(path);
                            pages.Add(File.ReadAllBytes(path));

                            page++;
                        }
                    }

                    return ImageManipulation.CombineImages(pages, format);
                }
            });
        }

        private static string GetFilenameExtension(ImageFormat format)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.FormatID == format.Guid)?.FilenameExtension.Split(';')[0].Replace("*.", "").ToLower() ?? format.ToString();
        }
        
        public async Task<string> GetText(string tessdataLocation = null)
        {
            if (!_loaded)
                await Load();

            var attempt = GetTextFromSimplePdf();

            if (!string.IsNullOrEmpty(attempt) && !string.IsNullOrEmpty(attempt.Trim()))
                return attempt;

            var tiff = await GetImage(ImageFormat.Tiff);

            try
            {
                return await Tesseract.GetTextFromTiff(tiff, tessdataLocation); // ocr tiff
            }
            catch (IOException)
            {
                tiff = ImageManipulation.ChangeFormat(tiff, ImageFormat.Tiff); // re-saves the image
                return await Tesseract.GetTextFromTiff(tiff, tessdataLocation); // ocr tiff
            }
        }

        private string GetTextFromSimplePdf()
        {
            PDDocument doc = null;
            try
            {
                doc = PDDocument.load(_location.FullName);
                var stripper = new PDFTextStripper();
                return stripper.getText(doc);
            }
            finally
            {
                doc?.close();
            }
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}