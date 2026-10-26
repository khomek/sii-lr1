namespace FaceEyeLab.Models;

/// <summary>Фиксированные параметры поиска. В интерфейсе они не изменяются.</summary>
public static class DetectionSettings
{
    public const double ScaleFactor = 1.1;
    public const int MinNeighbors = 5;
    public const int MinFaceSize = 40;
    public const int MaxDetectionWidth = 800;
}
