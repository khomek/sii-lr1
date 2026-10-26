using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using FaceEyeLab.Models;

namespace FaceEyeLab.Services;

/// <summary>Один экземпляр используется последовательно одним рабочим потоком.</summary>
public sealed class FaceEyeDetector : IDisposable
{
    private readonly CascadeClassifier faceCascade;
    private readonly CascadeClassifier eyeCascade;

    public FaceEyeDetector(string cascadeDirectory)
    {
        string facePath = Path.Combine(cascadeDirectory, "haarcascade_frontalface_default.xml");
        string eyePath = Path.Combine(cascadeDirectory, "haarcascade_eye_tree_eyeglasses.xml");
        foreach (string path in new[] { facePath, eyePath })
            if (!File.Exists(path))
                throw new FileNotFoundException("Не найден XML-каскад. Пересоберите проект.", path);
        faceCascade = new CascadeClassifier(facePath);
        try { eyeCascade = new CascadeClassifier(eyePath); }
        catch { faceCascade.Dispose(); throw; }
    }

    public static string DefaultCascadeDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "Cascades");

    public DetectionResult Process(Mat frame)
    {
        if (frame.IsEmpty) throw new ArgumentException("Получен пустой кадр.", nameof(frame));
        var watch = Stopwatch.StartNew();
        using var reduced = new Mat();
        double scale = Math.Min(1.0, (double)DetectionSettings.MaxDetectionWidth / frame.Width);
        CvInvoke.Resize(frame, reduced, new Size(
            Math.Max(1, (int)Math.Round(frame.Width * scale)),
            Math.Max(1, (int)Math.Round(frame.Height * scale))), 0, 0, Inter.Area);
        using var gray = new Mat();
        if (reduced.NumberOfChannels == 1) reduced.CopyTo(gray);
        else CvInvoke.CvtColor(reduced, gray,
            reduced.NumberOfChannels == 4 ? ColorConversion.Bgra2Gray : ColorConversion.Bgr2Gray);
        CvInvoke.EqualizeHist(gray, gray);

        Rectangle[] faces = faceCascade.DetectMultiScale(gray, DetectionSettings.ScaleFactor,
            DetectionSettings.MinNeighbors, new Size(DetectionSettings.MinFaceSize, DetectionSettings.MinFaceSize));
        var found = new List<DetectedFace>();
        // Координаты переводятся обратно в исходное разрешение кадра.
        double sx = (double)frame.Width / reduced.Width;
        double sy = (double)frame.Height / reduced.Height;
        Rectangle ToOriginal(Rectangle r) => Rectangle.Intersect(new Rectangle(Point.Empty, frame.Size),
            Rectangle.FromLTRB((int)Math.Round(r.Left * sx), (int)Math.Round(r.Top * sy),
                (int)Math.Round(r.Right * sx), (int)Math.Round(r.Bottom * sy)));

        foreach (Rectangle face in faces.OrderBy(f => f.X))
        {
            var eyes = new List<Rectangle>();
            // Ищем глаза только в верхней части лица, исключая рот и фон.
            var upperFace = new Rectangle(face.X, face.Y, face.Width, (int)(face.Height * 0.65));
            using var roi = new Mat(gray, upperFace);
            int minEye = Math.Max(8, face.Width / 10);
            var candidates = eyeCascade.DetectMultiScale(roi, DetectionSettings.ScaleFactor, DetectionSettings.MinNeighbors,
                new Size(minEye, minEye), new Size(face.Width / 2, face.Height / 2));
            // Не более одного результата в каждой половине лица.
            var selected = candidates
                .Where(e => e.Y + e.Height / 2.0 > face.Height * 0.12)
                .GroupBy(e => e.X + e.Width / 2.0 < face.Width / 2.0)
                .Select(g => g.OrderByDescending(e => e.Width * e.Height).First());
            foreach (var eye in selected)
                eyes.Add(ToOriginal(new Rectangle(upperFace.X + eye.X, upperFace.Y + eye.Y,
                    eye.Width, eye.Height)));
            found.Add(new DetectedFace(ToOriginal(face), eyes));
        }

        Mat annotated = new();
        try
        {
            if (frame.NumberOfChannels == 1) CvInvoke.CvtColor(frame, annotated, ColorConversion.Gray2Bgr);
            else if (frame.NumberOfChannels == 4) CvInvoke.CvtColor(frame, annotated, ColorConversion.Bgra2Bgr);
            else frame.CopyTo(annotated);
            int thickness = Math.Max(2, frame.Width / 400);
            int number = 0;
            foreach (DetectedFace face in found)
            {
                CvInvoke.Rectangle(annotated, face.Bounds, new MCvScalar(90, 210, 30), thickness);
                CvInvoke.PutText(annotated, $"Face {++number}",
                    new Point(face.Bounds.X, Math.Max(18, face.Bounds.Y - 8)),
                    FontFace.HersheySimplex, 0.6, new MCvScalar(90, 210, 30), thickness);
                foreach (Rectangle eye in face.Eyes)
                    CvInvoke.Rectangle(annotated, eye, new MCvScalar(255, 160, 40), thickness);
            }
            return new DetectionResult(annotated, found, watch.Elapsed.TotalMilliseconds);
        }
        catch { annotated.Dispose(); throw; }
    }

    public void Dispose()
    {
        eyeCascade.Dispose();
        faceCascade.Dispose();
    }
}
