using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ToText.Models
{
    public static class ImageManipulation
    {
        public static byte[] ChangeFormat(byte[] data, ImageFormat format)
        {
            using (var from = new MemoryStream(data))
            {
                var img = Image.FromStream(from);
                using (var to = new MemoryStream())
                {
                    img.Save(to, format);
                    return to.ToArray();
                }
            }
        }

        public static byte[] CombineImages(List<byte[]> files, ImageFormat format)
        {
            if (files.Count == 1)
                return files[0];

            var images = new List<Image>();
            var nIndex = 0;
            var height = 0;

            foreach (var file in files)
            {
                using (var ms = new MemoryStream(file))
                {
                    var img = Image.FromStream(ms);

                    images.Add(img);
                    height += img.Height;
                }
            }

            var width = images.Max(x => x.Width);

            using (var result = new MemoryStream())
            {
                using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb))
                {
                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.Clear(SystemColors.AppWorkspace);

                        foreach (var image in images)
                        {
                            if (nIndex == 0)
                            {
                                g.DrawImage(image, new Point(0, 0));
                                nIndex++;
                                height = image.Height;
                            }
                            else
                            {
                                g.DrawImage(image, new Point(0, height));
                                height += image.Height;
                            }

                            image.Dispose();
                        }
                    }

                    bitmap.Save(result, format);
                }

                return result.ToArray();
            }
        }

    }
}