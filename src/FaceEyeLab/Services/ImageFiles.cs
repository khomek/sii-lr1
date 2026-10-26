using Emgu.CV;
using Emgu.CV.CvEnum;

namespace FaceEyeLab.Services;

public static class ImageFiles
{
    public static Mat Load(string path)
    {
        // Imdecode с байтами поддерживает кириллицу в путях независимо от сборки OpenCV.
        byte[] bytes = File.ReadAllBytes(path);
        var frame = new Mat();
        try
        {
            CvInvoke.Imdecode(bytes, ImreadModes.ColorBgr, frame);
            if (frame.IsEmpty) throw new InvalidDataException("Файл не содержит поддерживаемого изображения.");
            return frame;
        }
        catch { frame.Dispose(); throw; }
    }
}
