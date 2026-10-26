#nullable enable
namespace FaceEyeLab;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private TableLayoutPanel layout = null!;
    private FlowLayoutPanel actions = null!;
    private TableLayoutPanel images = null!;
    private PictureBox originalBox = null!;
    private PictureBox resultBox = null!;
    private Label titleLabel = null!;
    private Label statusLabel = null!;
    private Label metricsLabel = null!;
    private Label legendLabel = null!;
    private Button startButton = null!;
    private Button stopButton = null!;
    private Button photoButton = null!;
    private Button demoButton = null!;
    private Button saveButton = null!;
    private GroupBox sourceGroup = null!;
    private GroupBox resultGroup = null!;
    private TableLayoutPanel footer = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        layout = new TableLayoutPanel();
        actions = new FlowLayoutPanel();
        images = new TableLayoutPanel();
        originalBox = new PictureBox();
        resultBox = new PictureBox();
        titleLabel = new Label();
        statusLabel = new Label();
        metricsLabel = new Label();
        legendLabel = new Label();
        startButton = new Button();
        stopButton = new Button();
        photoButton = new Button();
        demoButton = new Button();
        saveButton = new Button();
        sourceGroup = new GroupBox();
        resultGroup = new GroupBox();
        footer = new TableLayoutPanel();
        SuspendLayout();
        layout.Dock = DockStyle.Fill;
        layout.Padding = new Padding(16);
        layout.ColumnCount = 1;
        layout.RowCount = 5;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        titleLabel.Text = "Поиск лиц и глаз";
        titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        titleLabel.Dock = DockStyle.Fill;
        actions.Dock = DockStyle.Fill;
        actions.WrapContents = false;
        actions.AutoScroll = true;
        startButton.Text = "Запустить камеру";
        startButton.Size = new Size(150, 36);
        stopButton.Text = "Остановить";
        stopButton.Size = new Size(115, 36);
        photoButton.Text = "Открыть фото";
        photoButton.Size = new Size(130, 36);
        demoButton.Text = "Пример";
        demoButton.Size = new Size(100, 36);
        saveButton.Text = "Сохранить PNG";
        saveButton.Size = new Size(140, 36);
        actions.Controls.AddRange(new Control[] { startButton, stopButton, photoButton, demoButton, saveButton });
        images.Dock = DockStyle.Fill;
        images.ColumnCount = 2;
        images.RowCount = 1;
        images.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        images.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        images.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        sourceGroup.Text = "Исходное изображение";
        sourceGroup.Dock = DockStyle.Fill;
        sourceGroup.Padding = new Padding(10);
        resultGroup.Text = "Результат обработки";
        resultGroup.Dock = DockStyle.Fill;
        resultGroup.Padding = new Padding(10);
        originalBox.Dock = DockStyle.Fill;
        originalBox.BackColor = Color.FromArgb(29, 36, 47);
        originalBox.SizeMode = PictureBoxSizeMode.Zoom;
        resultBox.Dock = DockStyle.Fill;
        resultBox.BackColor = Color.FromArgb(29, 36, 47);
        resultBox.SizeMode = PictureBoxSizeMode.Zoom;
        sourceGroup.Controls.Add(originalBox);
        resultGroup.Controls.Add(resultBox);
        images.Controls.Add(sourceGroup, 0, 0);
        images.Controls.Add(resultGroup, 1, 0);
        legendLabel.Text = "Зелёная рамка — лицо   •   Голубая рамка — глаз";
        legendLabel.Dock = DockStyle.Fill;
        legendLabel.TextAlign = ContentAlignment.MiddleLeft;
        legendLabel.ForeColor = Color.FromArgb(80, 91, 107);
        footer.Dock = DockStyle.Fill;
        footer.RowCount = 2;
        footer.ColumnCount = 1;
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        statusLabel.Text = "Запустите камеру компьютера или откройте фото.";
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.AutoEllipsis = true;
        metricsLabel.Text = "Лица: —   |   Глаза: —   |   Обработка: —   |   FPS: —";
        metricsLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        metricsLabel.Dock = DockStyle.Fill;
        footer.Controls.Add(statusLabel, 0, 0);
        footer.Controls.Add(metricsLabel, 0, 1);
        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(actions, 0, 1);
        layout.Controls.Add(images, 0, 2);
        layout.Controls.Add(legendLabel, 0, 3);
        layout.Controls.Add(footer, 0, 4);
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1000, 680);
        MinimumSize = new Size(860, 560);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(245, 247, 250);
        Text = "Лабораторная работа 1 — Поиск лиц и глаз";
        StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(layout);
        ResumeLayout(false);
    }
}
