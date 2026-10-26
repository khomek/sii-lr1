using Emgu.CV;
using System.Diagnostics;
using Emgu.CV.Util;

namespace FaceEyeLab.Services;

/// <summary>VideoCapture — современное имя класса Capture из старой версии методички.</summary>
public sealed class FrameSource : IDisposable
{
    private readonly VideoCapture capture;
    private Mat? firstFrame;
    public string BackendName { get; }

    private FrameSource(VideoCapture capture, Mat firstFrame, string backendName)
    {
        this.capture = capture;
        this.firstFrame = firstFrame;
        BackendName = backendName;
    }

    public static FrameSource OpenCamera()
    {
        const int cameraIndex = 0;
        var failures = new List<string>();
        var errors = new List<Exception>();
        foreach (var backend in new[] { VideoCapture.API.DShow, VideoCapture.API.Msmf })
        {
            VideoCapture? candidate = null;
            Mat? frame = null;
            try
            {
                candidate = new VideoCapture(cameraIndex, backend);
                if (!candidate.IsOpened) throw new InvalidOperationException("Устройство не открылось.");
                // Драйвер выбирает поддерживаемый формат без повторного переключения FPS/разрешения.
                frame = new Mat();
                if (!ReadReadyFrame(candidate, frame, TimeSpan.FromSeconds(3)))
                    throw new InvalidOperationException("Устройство открылось, но не передает изображение.");
                var source = new FrameSource(candidate, frame, backend.ToString());
                candidate = null;
                frame = null;
                return source;
            }
            catch (Exception ex) when (ex is CvException or InvalidOperationException)
            {
                failures.Add($"{backend}: {ex.Message}");
                errors.Add(ex);
            }
            finally { frame?.Dispose(); candidate?.Dispose(); }
        }
        throw new InvalidOperationException(
            "Не удалось получить изображение с камеры компьютера. Проверьте подключение, " +
            "доступ Windows и другие приложения. " + string.Join(" ", failures), new AggregateException(errors));
    }

    // Переданный Mat принадлежит вызывающему коду и переиспользуется либо освобождается им.
    public bool Read(Mat frame)
    {
        if (firstFrame is not null)
        {
            firstFrame.CopyTo(frame);
            firstFrame.Dispose();
            firstFrame = null;
            return true;
        }
        // Кратковременный пустой кадр не должен немедленно выключать камеру.
        return ReadReadyFrame(capture, frame, TimeSpan.FromSeconds(2));
    }

    private static bool ReadReadyFrame(VideoCapture capture, Mat frame, TimeSpan timeout)
    {
        var watch = Stopwatch.StartNew();
        CvException? lastError = null;
        do
        {
            try
            {
                if (capture.Read(frame) && !frame.IsEmpty && frame.Width > 0 && frame.Height > 0)
                    return true;
            }
            catch (CvException ex) when (ex.ErrorMessage.Contains("empty", StringComparison.OrdinalIgnoreCase))
            {
                // Некоторые драйверы временно возвращают пустой буфер при запуске.
                lastError = ex;
            }
            Thread.Sleep(50);
        } while (watch.Elapsed < timeout);
        if (lastError is not null)
            throw new InvalidOperationException("Камера продолжает возвращать пустой буфер.", lastError);
        return false;
    }

    public void Dispose() { firstFrame?.Dispose(); capture.Dispose(); }
}
