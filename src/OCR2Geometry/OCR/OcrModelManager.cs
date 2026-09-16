using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace OCR2Geometry.OCR
{
    public sealed class OcrModelPaths
    {
        public string Detection { get; set; }
        public string Classification { get; set; }
        public string Recognition { get; set; }
        public string Dictionary { get; set; }
    }

    public static class OcrModelManager
    {
        private const string BaseUrl = "https://raw.githubusercontent.com/RapidAI/RapidOCRCSharp/main/RapidOCRConsole/models/";

        private static readonly Dictionary<string, string> ModelFiles = new Dictionary<string, string>
        {
            { "ch_PP-OCRv5_mobile_det.onnx", BaseUrl + "ch_PP-OCRv5_mobile_det.onnx" },
            { "ch_ppocr_mobile_v2.0_cls_infer.onnx", BaseUrl + "ch_ppocr_mobile_v2.0_cls_infer.onnx" },
            { "ch_PP-OCRv5_rec_mobile_infer.onnx", BaseUrl + "ch_PP-OCRv5_rec_mobile_infer.onnx" },
            { "ppocrv5_dict.txt", BaseUrl + "ppocrv5_dict.txt" }
        };

        public static string ModelsDirectory
        {
            get
            {
                var assemblyDirectory = Path.GetDirectoryName(typeof(OcrModelManager).Assembly.Location);
                return Path.Combine(assemblyDirectory ?? AppDomain.CurrentDomain.BaseDirectory, "models");
            }
        }

        public static bool AreModelsAvailable()
        {
            foreach (var fileName in ModelFiles.Keys)
            {
                var path = Path.Combine(ModelsDirectory, fileName);
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static async Task<OcrModelPaths> EnsureModelsAsync(Action<string> statusCallback = null)
        {
            Directory.CreateDirectory(ModelsDirectory);
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using (var client = new WebClient())
            {
                foreach (var pair in ModelFiles)
                {
                    var target = Path.Combine(ModelsDirectory, pair.Key);
                    if (File.Exists(target) && new FileInfo(target).Length > 0)
                    {
                        continue;
                    }

                    statusCallback?.Invoke("Downloading OCR model: " + pair.Key);
                    var temporary = target + ".download";

                    try
                    {
                        if (File.Exists(temporary))
                        {
                            File.Delete(temporary);
                        }

                        await client.DownloadFileTaskAsync(new Uri(pair.Value), temporary);

                        if (!File.Exists(temporary) || new FileInfo(temporary).Length == 0)
                        {
                            throw new InvalidOperationException("Downloaded OCR model is empty: " + pair.Key);
                        }

                        if (File.Exists(target))
                        {
                            File.Delete(target);
                        }

                        File.Move(temporary, target);
                    }
                    catch
                    {
                        if (File.Exists(temporary))
                        {
                            File.Delete(temporary);
                        }

                        throw;
                    }
                }
            }

            return GetPaths();
        }

        public static OcrModelPaths GetPaths()
        {
            return new OcrModelPaths
            {
                Detection = Path.Combine(ModelsDirectory, "ch_PP-OCRv5_mobile_det.onnx"),
                Classification = Path.Combine(ModelsDirectory, "ch_ppocr_mobile_v2.0_cls_infer.onnx"),
                Recognition = Path.Combine(ModelsDirectory, "ch_PP-OCRv5_rec_mobile_infer.onnx"),
                Dictionary = Path.Combine(ModelsDirectory, "ppocrv5_dict.txt")
            };
        }
    }
}
