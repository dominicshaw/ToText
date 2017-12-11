using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Tesseract;

namespace ToText.Models
{
    public static class Tesseract
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Tesseract));

        public static async Task<string> GetTextFromTiff(byte[] tiff, string tessdataLocation = null)
        {
            return await Task.Run(() =>
            {
                using (var img = Pix.LoadTiffFromMemory(tiff))
                {
                    return ReadTextFromPix(img, tessdataLocation);
                }
            });
        }

        private static TesseractEngine GetTesseractEngine(string tessdataLocation = null)
        {
            if (tessdataLocation == null)
            {
                tessdataLocation = @"./tessdata";

                if (ConfigurationManager.AppSettings["TessdataLocation"] != null)
                    tessdataLocation = ConfigurationManager.AppSettings["TessdataLocation"];
            }

            _log.Info("Tessdata Location: " + tessdataLocation);

            if (tessdataLocation != @"./tessdata" && !Directory.Exists(tessdataLocation))
                _log.Error("TESSDATA Location was not found.");

            return new TesseractEngine(tessdataLocation, "eng", EngineMode.Default);
        }

        private static string ReadTextFromPix(Pix img, string tessdataLocation = null)
        {
            var sb = new StringBuilder();

            using (var engine = GetTesseractEngine(tessdataLocation))
            {
                using (var page = engine.Process(img))
                {
                    var text = page.GetText();

                    sb.AppendLine(text);
                    sb.AppendLine();

                    using (var iter = page.GetIterator())
                    {
                        iter.Begin();

                        do
                        {
                            do
                            {
                                do
                                {
                                    do
                                    {
                                        if (iter.IsAtBeginningOf(PageIteratorLevel.Block))
                                        {
                                            sb.AppendLine();
                                        }

                                        sb.Append(iter.GetText(PageIteratorLevel.Word));
                                        sb.Append(" ");

                                        if (iter.IsAtFinalOf(PageIteratorLevel.TextLine, PageIteratorLevel.Word))
                                        {
                                            sb.AppendLine();
                                        }
                                    } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));

                                    if (iter.IsAtFinalOf(PageIteratorLevel.Para, PageIteratorLevel.TextLine))
                                    {
                                    }
                                } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                            } while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                        } while (iter.Next(PageIteratorLevel.Block));
                    }

                    sb.AppendLine(string.Format("OCR Confidence: {0}", page.GetMeanConfidence()));
                }
            }

            return sb.ToString();
        }

    }
}