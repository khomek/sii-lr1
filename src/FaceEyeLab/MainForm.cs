using System.Diagnostics;
using System.Drawing.Imaging;
using Emgu.CV;
using FaceEyeLab.Models;
using FaceEyeLab.Services;

namespace FaceEyeLab;

public partial class MainForm : Form
{
    private readonly System.Windows.Forms.Timer frameTimer;
    private readonly Stopwatch fpsWatch = new();
    private FaceEyeDetector? detector;
    private FrameSource? source;
    private Task<DisplayFrame?>? pendingFrame;
    private bool commandBusy;
    private bool stopping;
    private bool closing;
    private bool mayClose;
    private int frameCount;
    private double fps;
    private int demoIndex;
    internal int LastFaceCount { get; private set; }

    public MainForm()
    {
        InitializeComponent();
        frameTimer = new System.Windows.Forms.Timer(components!) { Interval = 33 };
        frameTimer.Tick += FrameTimer_Tick;
        startButton.Click += async (_, _) => await RunCommandAsync(StartCameraAsync);
        stopButton.Click += async (_, _) => await RunCommandAsync(StopStreamAsync);
        photoButton.Click += async (_, _) => await RunCommandAsync(OpenPhotoAsync);
        demoButton.Click += async (_, _) => await RunCommandAsync(async () =>
        {
            string name = demoIndex++ % 2 == 0 ? "portrait.jpg" : "two-faces.jpg";
            await LoadPhotoAsync(Path.Combine(AppContext.BaseDirectory, "Samples", name));
        });
        saveButton.Click += (_, _) => SaveResult();
        FormClosing += MainForm_FormClosing;
        UpdateControls();
    }

    private FaceEyeDetector Detector => detector ??= new FaceEyeDetector(FaceEyeDetector.DefaultCascadeDirectory);
    private async Task RunCommandAsync(Func<Task> command)
    {
        if (commandBusy || closing) return;
        commandBusy = true;
        UpdateControls();
        try { await command(); }
        catch (Exception ex)
        {
            await StopStreamAsync();
            statusLabel.Text = "Ошибка: " + ex.Message;
            MessageBox.Show(this, ex.Message, "Не удалось выполнить действие", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { commandBusy = false; UpdateControls(); }
    }

    private async Task StartCameraAsync()
    {
        await StopStreamAsync();
        statusLabel.Text = "Открытие камеры…";
        source = await Task.Run(() => { _ = Detector; return FrameSource.OpenCamera(); });
        StartStream("Камера компьютера: обработка в реальном времени");
    }

    private async Task OpenPhotoAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите изображение",
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|Все файлы|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) await LoadPhotoAsync(dialog.FileName);
    }

    internal async Task LoadPhotoAsync(string path)
    {
        await StopStreamAsync();
        statusLabel.Text = "Обработка изображения…";
        using DisplayFrame result = await Task.Run(() =>
        {
            using Mat frame = ImageFiles.Load(path);
            return ProcessFrame(frame);
        });
        fps = 0;
        ShowFrame(result);
        statusLabel.Text = "Фото: " + Path.GetFileName(path);
        UpdateControls();
    }

    private void StartStream(string description)
    {
        frameCount = 0;
        fps = 0;
        fpsWatch.Restart();
        statusLabel.Text = description;
        frameTimer.Start();
    }

    private async void FrameTimer_Tick(object? sender, EventArgs e)
    {
        if (source is null || commandBusy || stopping || closing || pendingFrame is not null) return;
        FrameSource current = source;
        pendingFrame = Task.Run(() =>
        {
            using var frame = new Mat();
            if (!current.Read(frame)) return null;
            return ProcessFrame(frame);
        });
        try
        {
            using DisplayFrame? result = await pendingFrame;
            if (result is null)
            {
                await StopStreamAsync();
                statusLabel.Text = "Поток камеры прерван. Проверьте подключение.";
                return;
            }
            frameCount++;
            if (fpsWatch.ElapsedMilliseconds >= 1000)
            {
                fps = frameCount / fpsWatch.Elapsed.TotalSeconds;
                frameCount = 0;
                fpsWatch.Restart();
            }
            ShowFrame(result);
        }
        catch (Exception ex)
        {
            await StopStreamAsync();
            statusLabel.Text = "Ошибка видеопотока: " + ex.Message;
        }
        finally { pendingFrame = null; UpdateControls(); }
    }

    private DisplayFrame ProcessFrame(Mat frame)
    {
        using DetectionResult result = Detector.Process(frame);
        Bitmap original = frame.ToBitmap();
        try { return new DisplayFrame(original, result.Annotated.ToBitmap(), result.Faces.Count,
            result.EyeCount, result.Milliseconds); }
        catch { original.Dispose(); throw; }
    }

    private void ShowFrame(DisplayFrame frame)
    {
        LastFaceCount = frame.Faces;
        // PictureBox получает независимую копию: рабочие кадры можно сразу освободить.
        ReplaceImage(originalBox, new Bitmap(frame.Original));
        ReplaceImage(resultBox, new Bitmap(frame.Annotated));
        metricsLabel.Text = $"Лица: {frame.Faces}   |   Глаза: {frame.Eyes}   |   Обработка: {frame.Milliseconds:F1} мс   |   FPS: {(source is null ? "—" : fps.ToString("F1"))}";
    }

    private static void ReplaceImage(PictureBox box, Bitmap? next)
    {
        var previous = box.Image;
        box.Image = next;
        previous?.Dispose();
    }

    private async Task StopStreamAsync()
    {
        frameTimer.Stop();
        stopping = true;
        if (pendingFrame is not null)
        {
            try { await pendingFrame; }
            catch { /* Ошибка будет показана обработчиком кадра. */ }
        }
        source?.Dispose();
        source = null;
        stopping = false;
        statusLabel.Text = "Захват остановлен. Последний кадр доступен для сохранения.";
        UpdateControls();
    }

    private void SaveResult()
    {
        if (resultBox.Image is null) return;
        using var dialog = new SaveFileDialog { Filter = "PNG|*.png", FileName = $"result-{DateTime.Now:yyyyMMdd-HHmmss}.png" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { resultBox.Image.Save(dialog.FileName, ImageFormat.Png); statusLabel.Text = "Сохранено: " + dialog.FileName; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ошибка сохранения"); }
    }

    private void UpdateControls()
    {
        bool available = !commandBusy && !closing && !stopping;
        startButton.Enabled = available && source is null;
        stopButton.Enabled = available && source is not null;
        photoButton.Enabled = available;
        demoButton.Enabled = available;
        saveButton.Enabled = available && resultBox.Image is not null;
    }

    private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (mayClose) return;
        e.Cancel = true;
        if (closing) return;
        if (commandBusy) { statusLabel.Text = "Дождитесь завершения текущей операции и закройте окно повторно."; return; }
        closing = true;
        UpdateControls();
        await StopStreamAsync();
        detector?.Dispose();
        ReplaceImage(originalBox, null);
        ReplaceImage(resultBox, null);
        mayClose = true;
        Close();
    }

    private sealed record DisplayFrame(Bitmap Original, Bitmap Annotated, int Faces, int Eyes, double Milliseconds) : IDisposable
    {
        public void Dispose() { Original.Dispose(); Annotated.Dispose(); }
    }
}
