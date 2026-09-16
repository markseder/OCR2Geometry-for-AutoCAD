using System;
using System.IO;
using System.Threading.Tasks;
using RapidOCRLib;

namespace OCR2Geometry.OCR
{
    public sealed class RapidOcrService
    {
        private OcrLite _engine;
        private bool _initialized;

        public async Task<string> RecognizeTableTextAsync(string imagePath, Action<string> statusCallback = null)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image file was not found.", imagePath);
            }

            await EnsureInitializedAsync(statusCallback);

            statusCallback?.Invoke("Recognizing image...");
            var result = await _engine.DetectAsync(
                imagePath,
                padding: 20,
                maxSideLen: 2048,
                boxScoreThresh: 0.45f,
                boxThresh: 0.25f,
                unClipRatio: 1.6f,
                doAngle: true,
                mostAngle: false);

            if (result == null || result.TextBlocks == null || result.TextBlocks.Count == 0)
            {
                return string.Empty;
            }

            return OcrLayoutTextBuilder.BuildRows(result);
        }

        private async Task EnsureInitializedAsync(Action<string> statusCallback)
        {
            if (_initialized && _engine != null)
            {
                return;
            }

            var models = await OcrModelManager.EnsureModelsAsync(statusCallback);

            statusCallback?.Invoke("Initializing RapidOCR...");
            _engine = new OcrLite
            {
                DetPath = models.Detection,
                ClsPath = models.Classification,
                RecPath = models.Recognition,
                KeyDicPath = models.Dictionary
            };

            await _engine.InitModels();
            _initialized = true;
        }
    }
}
