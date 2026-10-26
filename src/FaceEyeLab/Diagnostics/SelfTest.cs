using System.Drawing.Imaging;
using System.Text.Json;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using FaceEyeLab.Services;

namespace FaceEyeLab.Diagnostics;

/// <summary>Интеграционные проверки настоящего OpenCV без подключения веб-камеры.</summary>
internal static class SelfTest
{
    private sealed record Check(string Name, bool Passed, string Details);

    public static int Run(string outputDirectory)
    {
        string output = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(output);
        var checks = new List<Check>();
        void Check(string name, bool passed, string details) => checks.Add(new(name, passed, details));
        try
        {
            using var detector = new FaceEyeDetector(FaceEyeDetector.DefaultCascadeDirectory);
            string samples = Path.Combine(AppContext.BaseDirectory, "Samples");
            using Mat portrait = ImageFiles.Load(Path.Combine(samples, "portrait.jpg"));
            using (var result = detector.Process(portrait))
            {
                result.Annotated.Save(Path.Combine(output, "portrait-result.png"));
                Check("portrait", result.Faces.Count == 1 && result.EyeCount == 2,
                    $"faces={result.Faces.Count}; eyes={result.EyeCount}; ms={result.Milliseconds:F2}");
            }

            // Путь проекта на другом компьютере может содержать кириллицу.
            string unicodeCascades = Path.Combine(output, "каскады Хаара");
            Directory.CreateDirectory(unicodeCascades);
            foreach (string path in Directory.GetFiles(FaceEyeDetector.DefaultCascadeDirectory, "*.xml"))
                File.Copy(path, Path.Combine(unicodeCascades, Path.GetFileName(path)), true);
            using (var unicodeDetector = new FaceEyeDetector(unicodeCascades))
            using (var result = unicodeDetector.Process(portrait))
                Check("unicode-cascades", result.Faces.Count == 1 && result.EyeCount == 2,
                    "Cascade models load and detect correctly from a Cyrillic directory.");

            // Второй пример — явно обозначенный коллаж двух копий тестового портрета.
            using var pair = new Mat(new Size(1024, 512), DepthType.Cv8U, 3);
            using (var left = new Mat(pair, new Rectangle(0, 0, 512, 512))) portrait.CopyTo(left);
            using (var right = new Mat(pair, new Rectangle(512, 0, 512, 512))) portrait.CopyTo(right);
            pair.Save(Path.Combine(output, "two-faces.jpg"));
            using (var result = detector.Process(pair))
            {
                result.Annotated.Save(Path.Combine(output, "two-faces-result.png"));
                Check("two-faces", result.Faces.Count == 2 && result.EyeCount == 4,
                    $"faces={result.Faces.Count}; eyes={result.EyeCount}; ms={result.Milliseconds:F2}");
                bool bounded = result.Faces.All(f => new Rectangle(Point.Empty, pair.Size).Contains(f.Bounds)
                    && f.Eyes.All(e => f.Bounds.Contains(e)));
                Check("coordinates", bounded, "All face and eye rectangles fit inside the original image and face ROI.");
            }

            using (var blank = new Mat(new Size(640, 480), DepthType.Cv8U, 3))
            {
                blank.SetTo(new MCvScalar(127, 127, 127));
                using var result = detector.Process(blank);
                Check("blank", result.Faces.Count == 0 && result.EyeCount == 0, "Uniform frame has no detections.");
            }
            using (var gray = new Mat())
            {
                CvInvoke.CvtColor(portrait, gray, ColorConversion.Bgr2Gray);
                using var result = detector.Process(gray);
                Check("gray-frame", result.Faces.Count == 1 && result.EyeCount == 2,
                    "Single-channel input is accepted with the same fixed face and eye detection.");
            }
            bool emptyRejected = false;
            try { using var empty = new Mat(); using var ignored = detector.Process(empty); }
            catch (ArgumentException) { emptyRejected = true; }
            Check("empty-frame", emptyRejected, "Empty input is rejected.");

            bool missingRejected = false;
            try { using var ignored = new FaceEyeDetector(Path.Combine(output, "missing-cascades")); }
            catch (FileNotFoundException) { missingRejected = true; }
            Check("missing-cascade", missingRejected, "Missing model gives an explicit error.");

            // XML может существовать, но не содержать загружаемый классификатор.
            foreach (string invalidName in new[] { "haarcascade_frontalface_default.xml", "haarcascade_eye_tree_eyeglasses.xml" })
            {
                string invalidDirectory = Path.Combine(output, "invalid-" + Path.GetFileNameWithoutExtension(invalidName));
                Directory.CreateDirectory(invalidDirectory);
                foreach (string path in Directory.GetFiles(FaceEyeDetector.DefaultCascadeDirectory, "*.xml"))
                    File.Copy(path, Path.Combine(invalidDirectory, Path.GetFileName(path)), true);
                File.WriteAllText(Path.Combine(invalidDirectory, invalidName),
                    "<?xml version=\"1.0\"?><opencv_storage><placeholder>1</placeholder></opencv_storage>");
                bool rejected = false;
                try { using var ignored = new FaceEyeDetector(invalidDirectory); }
                catch (InvalidDataException ex) { rejected = ex.Message.Contains(invalidName); }
                Check("invalid-" + Path.GetFileNameWithoutExtension(invalidName), rejected,
                    "Invalid classifier is rejected during loading with its filename, before opening the camera.");
            }

            string unicodePath = Path.Combine(output, "тест изображения.jpg");
            File.Copy(Path.Combine(samples, "portrait.jpg"), unicodePath, true);
            using (Mat decoded = ImageFiles.Load(unicodePath))
                Check("unicode-path", decoded.Size == portrait.Size, "Cyrillic image paths work.");
            string badPath = Path.Combine(output, "invalid.jpg");
            File.WriteAllText(badPath, "Not an image");
            bool badRejected = false;
            try { using var ignored = ImageFiles.Load(badPath); }
            catch (InvalidDataException) { badRejected = true; }
            Check("invalid-image", badRejected, "Corrupted image is rejected.");

            // Рендерится реальная форма с тем же методом загрузки, который использует кнопка.
            using var form = new MainForm();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-30000, -30000);
            form.ShowInTaskbar = false;
            form.Shown += async (_, _) =>
            {
                try
                {
                    await form.LoadPhotoAsync(Path.Combine(samples, "portrait.jpg"));
                    RenderForm(form, Path.Combine(output, "interface-portrait.png"));
                    await form.LoadPhotoAsync(Path.Combine(output, "two-faces.jpg"));
                    RenderForm(form, Path.Combine(output, "interface-two-faces.png"));
                    Check("windows-form", form.LastFaceCount == 2, "Both examples processed through MainForm.LoadPhotoAsync and rendered.");
                }
                catch (Exception ex) { Check("ui-error", false, ex.ToString()); }
                finally { form.Close(); }
            };
            Application.Run(form);
        }
        catch (Exception ex) { Check("unhandled", false, ex.ToString()); }

        bool success = checks.Count > 0 && checks.All(c => c.Passed);
        File.WriteAllText(Path.Combine(output, "verification.json"), JsonSerializer.Serialize(
            new { Success = success, CameraTested = false, Checks = checks }, new JsonSerializerOptions { WriteIndented = true }));
        return success ? 0 : 1;
    }

    private static void RenderForm(MainForm form, string path)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(path, ImageFormat.Png);
    }
}
