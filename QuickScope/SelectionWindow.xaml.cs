using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickScope;

public partial class SelectionWindow : Window
{
    private Point _startPoint;
    private bool _isSelecting = false;

    public SelectionWindow()
    {
        InitializeComponent();
    }

    public void ShowFreeze(BitmapSource freezeFrame)
    {
        // Reset selection
        SelectionBox.Visibility = Visibility.Collapsed;
        SelectionBox.Width = 0;
        SelectionBox.Height = 0;

        // Set the frozen image
        FrozenScreenImage.Source = freezeFrame;

        // Full-screen dim from the start (no selection = everything dimmed)
        // Use oversized values; canvas clips them
        double safeW = SystemParameters.VirtualScreenWidth * 2;
        double safeH = SystemParameters.VirtualScreenHeight * 2;

        Canvas.SetLeft(DimTop, 0);
        Canvas.SetTop(DimTop, 0);
        DimTop.Width = safeW;
        DimTop.Height = safeH;

        DimLeft.Width = DimLeft.Height = 0;
        DimRight.Width = DimRight.Height = 0;
        DimBottom.Width = DimBottom.Height = 0;

        // Just show — no opacity tricks, no render hacks
        this.Opacity = 1;
        this.Show();
        this.Activate();

        // Recalculate overlay once layout completes
        OverlayCanvas.SizeChanged -= OnOverlayCanvasSizeChangedOnce;
        OverlayCanvas.SizeChanged += OnOverlayCanvasSizeChangedOnce;
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(this);

            SelectionBox.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionBox, _startPoint.X);
            Canvas.SetTop(SelectionBox, _startPoint.Y);
            SelectionBox.Width = 0;
            SelectionBox.Height = 0;
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isSelecting)
        {
            Point currentPoint = e.GetPosition(this);

            double x = Math.Min(currentPoint.X, _startPoint.X);
            double y = Math.Min(currentPoint.Y, _startPoint.Y);
            double width = Math.Max(currentPoint.X, _startPoint.X) - x;
            double height = Math.Max(currentPoint.Y, _startPoint.Y) - y;

            Canvas.SetLeft(SelectionBox, x);
            Canvas.SetTop(SelectionBox, y);
            SelectionBox.Width = width;
            SelectionBox.Height = height;

            UpdateOverlay(x, y, width, height);
        }
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isSelecting)
        {
            _isSelecting = false;

            if (SelectionBox.Width > 5 && SelectionBox.Height > 5)
            {
                CropAndSave();
            }
            else
            {
                CleanupAndClose();
            }
        }
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            var freezeFrame = (BitmapSource)FrozenScreenImage.Source;
            await SaveImageAsync(freezeFrame);
            CleanupAndClose();
        }
    }

    private async void CropAndSave()
    {
        try
        {
            double x = Canvas.GetLeft(SelectionBox);
            double y = Canvas.GetTop(SelectionBox);

            var originalImage = (BitmapSource)FrozenScreenImage.Source;

            double scaleX = originalImage.PixelWidth / this.ActualWidth;
            double scaleY = originalImage.PixelHeight / this.ActualHeight;

            Int32Rect cropRect = new Int32Rect(
                (int)(x * scaleX),
                (int)(y * scaleY),
                (int)(SelectionBox.Width * scaleX),
                (int)(SelectionBox.Height * scaleY));

            var croppedBitmap = new CroppedBitmap(originalImage, cropRect);

            await SaveImageAsync(croppedBitmap);
            CleanupAndClose();
        }
        catch (Exception ex)
        {
            CleanupAndClose();
        }
    }

    private void CleanupAndClose()
    {
        FrozenScreenImage.Source = null;
        this.Close();
    }

    private async System.Threading.Tasks.Task SaveImageAsync(BitmapSource freezeFrame)
    {
        var freezeFrameToSave = BitmapFrame.Create(freezeFrame);
        freezeFrameToSave.Freeze();

        try
        {
            Clipboard.SetImage(freezeFrameToSave);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Clipboard failed: {ex.Message}");
        }

        _ = System.Threading.Tasks.Task.Run(() =>
        {
            string screenshotsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
            Directory.CreateDirectory(screenshotsFolder);
            string filePath = Path.Combine(screenshotsFolder,
                $"QuickScope_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(freezeFrameToSave);
                encoder.Save(fileStream);
            }

            Console.WriteLine($"Saved capture to: {filePath}");
        });
    }

    private void UpdateOverlay(double x, double y, double w, double h)
    {
        double canvasWidth = OverlayCanvas.ActualWidth;
        double canvasHeight = OverlayCanvas.ActualHeight;

        Canvas.SetLeft(DimTop, 0);
        Canvas.SetTop(DimTop, 0);
        DimTop.Width = canvasWidth;
        DimTop.Height = Math.Max(0, y);

        Canvas.SetLeft(DimBottom, 0);
        Canvas.SetTop(DimBottom, y + h);
        DimBottom.Width = canvasWidth;
        DimBottom.Height = Math.Max(0, canvasHeight - y - h);

        Canvas.SetLeft(DimLeft, 0);
        Canvas.SetTop(DimLeft, y);
        DimLeft.Width = Math.Max(0, x);
        DimLeft.Height = Math.Max(0, h);

        Canvas.SetLeft(DimRight, x + w);
        Canvas.SetTop(DimRight, y);
        DimRight.Width = Math.Max(0, canvasWidth - x - w);
        DimRight.Height = Math.Max(0, h);
    }

    private void OnOverlayCanvasSizeChangedOnce(object sender, SizeChangedEventArgs e)
    {
        OverlayCanvas.SizeChanged -= OnOverlayCanvasSizeChangedOnce;
        UpdateOverlay(0, 0, 0, 0);
    }
}