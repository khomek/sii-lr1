using FaceEyeLab.Diagnostics;

namespace FaceEyeLab;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length > 0 && args[0] == "--self-test")
            return SelfTest.Run(args.Length > 1 ? args[1] : "TestResults");
        if (args.Length > 0 && args[0] == "--camera-test")
            return CameraDiagnostic.Run(args.Length > 1 ? args[1] : "TestResults");

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => MessageBox.Show(e.Exception.Message,
            "Ошибка приложения", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Application.Run(new MainForm());
        return 0;
    }
}
