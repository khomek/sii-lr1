using Emgu.CV;
using Emgu.CV.CvEnum;

namespace FaceEyeLab.Services;

/// <summary>VideoCapture — современное имя класса Capture из старой версии методички.</summary>
public sealed class FrameSource : IDisposable
{
    private readonly VideoCapture capture;
    private FrameSource(VideoCapture capture)
    {
        this.capture = capture;
        if (!capture.IsOpened)
        {
            capture.Dispose();
            throw new InvalidOperationException(
                "Камера компьютера недоступна. Проверьте подключение, разрешение доступа Windows и другие приложения.");
        }
    }

    public static FrameSource OpenCamera()
    {
        const int cameraIndex = 0;
        var capture = new VideoCapture(cameraIndex, VideoCapture.API.DShow);
        if (!capture.IsOpened)
        {
            capture.Dispose();
            capture = new VideoCapture(cameraIndex, VideoCapture.API.Msmf);
        }
        var source = new FrameSource(capture);
        capture.Set(CapProp.FrameWidth, 640);
        capture.Set(CapProp.FrameHeight, 480);
        capture.Set(CapProp.Fps, 30);
        return source;
    }

    // Переданный Mat принадлежит вызывающему коду и переиспользуется либо освобождается им.
    public bool Read(Mat frame) => capture.Read(frame) && !frame.IsEmpty;
    public void Dispose() => capture.Dispose();
}
