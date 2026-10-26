using System.Text.Json;
using Emgu.CV;
using FaceEyeLab.Services;

namespace FaceEyeLab.Diagnostics;

/// <summary>Проверка камеры без сохранения фотографий: только параметры кадров и ошибки.</summary>
internal static class CameraDiagnostic
{
    public static int Run(string outputDirectory)
    {
        string directory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(directory);
        var frames = new List<object>();
        string stage = "load-cascades";
        string? error = null;
        bool success = false;
        try
        {
            using var detector = Task.Run(() => new FaceEyeDetector(FaceEyeDetector.DefaultCascadeDirectory))
                .GetAwaiter().GetResult();
            stage = "open-camera";
            using var source = Task.Run(FrameSource.OpenCamera).GetAwaiter().GetResult();
            for (int index = 0; index < 10; index++)
            {
                stage = $"read-frame-{index + 1}";
                bool received = Task.Run(() =>
                {
                    using var frame = new Mat();
                    if (!source.Read(frame)) return false;
                    stage = $"detect-frame-{index + 1}";
                    using var result = detector.Process(frame);
                    stage = $"convert-frame-{index + 1}";
                    using var original = frame.ToBitmap();
                    using var annotated = result.Annotated.ToBitmap();
                    frames.Add(new { Number = index + 1, frame.Width, frame.Height,
                        Backend = source.BackendName, Channels = frame.NumberOfChannels,
                        Faces = result.Faces.Count, Eyes = result.EyeCount });
                    return true;
                }).GetAwaiter().GetResult();
                if (!received) throw new InvalidOperationException("Камера вернула пустой кадр.");
            }
            success = true;
            stage = "complete";
        }
        catch (Exception ex) { error = ex.ToString(); }
        File.WriteAllText(Path.Combine(directory, "camera-diagnostic.json"), JsonSerializer.Serialize(
            new { Success = success, Stage = stage, Frames = frames, Error = error },
            new JsonSerializerOptions { WriteIndented = true }));
        return success ? 0 : 1;
    }
}
