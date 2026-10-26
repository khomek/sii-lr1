using Emgu.CV;

namespace FaceEyeLab.Models;

public sealed record DetectedFace(Rectangle Bounds, IReadOnlyList<Rectangle> Eyes);

/// <summary>Владеет размеченным Mat; вызывающий код обязан вызвать Dispose.</summary>
public sealed class DetectionResult(Mat annotated, IReadOnlyList<DetectedFace> faces, double milliseconds)
    : IDisposable
{
    public Mat Annotated { get; } = annotated;
    public IReadOnlyList<DetectedFace> Faces { get; } = faces;
    public int EyeCount => Faces.Sum(face => face.Eyes.Count);
    public double Milliseconds { get; } = milliseconds;
    public void Dispose() => Annotated.Dispose();
}
