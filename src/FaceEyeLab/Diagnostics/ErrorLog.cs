using Emgu.CV.Util;

namespace FaceEyeLab.Diagnostics;

internal static class ErrorLog
{
    public static string Record(Exception exception, string operation)
    {
        string details = exception.ToString();
        var cv = FindOpenCvError(exception);
        if (cv is not null)
            details += $"\nOpenCV: {cv.FunctionName}; {cv.FileName}:{cv.Line}; {cv.ErrorMessage}";
        try
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FaceEyeLab");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "error.log");
            File.AppendAllText(path, $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {operation}\n{details}\n");
            return path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "журнал записать не удалось";
        }
    }

    public static string Describe(Exception exception)
    {
        var cv = FindOpenCvError(exception);
        return cv is null ? exception.Message : $"{exception.Message} [OpenCV: {cv.FunctionName}]";
    }

    private static CvException? FindOpenCvError(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is CvException cv) return cv;
        return null;
    }
}
