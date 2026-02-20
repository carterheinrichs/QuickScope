using System.Windows;
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
        //reset the box
        SelectionBox.Visibility = Visibility.Collapsed;
        SelectionBox.Width = 0;
        SelectionBox.Height = 0;

        // cover the window
        double safeW = SystemParameters.PrimaryScreenWidth * 2;
        double safeH = SystemParameters.PrimaryScreenHeight * 2;

        Canvas.SetLeft(DimTop, 0);
        Canvas.SetTop(DimTop, 0);
        DimTop.Width = safeW;
        DimTop.Height = safeH;

        DimLeft.Width = DimLeft.Height = 0;
        DimRight.Width = DimRight.Height = 0;
        DimBottom.Width = DimBottom.Height = 0;

        FrozenScreenImage.Source = freezeFrame;
        
        this.Opacity = 0;

        
        this.Show();
        this.Activate(); // come up to the front of the class

        void OnRendering(object? s, EventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            
            this.Dispatcher.Invoke(() =>
            {
                this.Opacity = 1;
            }, System.Windows.Threading.DispatcherPriority.Input);
        }
        
        CompositionTarget.Rendering += OnRendering;

        
        // Once layout is done, recalculate overlay properly using actual dimensions
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
            
            UpdateOverlay(Canvas.GetLeft(SelectionBox), Canvas.GetTop(SelectionBox), SelectionBox.Width, SelectionBox.Height);
        }
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isSelecting)
        {
            _isSelecting = false;

            // Ensure it was a drag, not just an accidental click
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

            // hide selection box
            SelectionBox.Visibility = Visibility.Collapsed;
            DimTop.Width = DimTop.Height = 0;
            DimLeft.Width = DimLeft.Height = 0;
            DimRight.Width = DimRight.Height = 0;
            DimBottom.Width = DimBottom.Height = 0;
            
            // Calculate DPI scaling ratio (WPF layout sizes vs Actual Image Pixels)
            double scaleX = originalImage.PixelWidth / this.ActualWidth;
            double scaleY = originalImage.PixelHeight / this.ActualHeight;

            Int32Rect cropRect = new Int32Rect(
                (int)(x * scaleX),
                (int)(y * scaleY),
                (int)(SelectionBox.Width * scaleX),
                (int)(SelectionBox.Height * scaleY));

            var croppedBitmap = new CroppedBitmap(originalImage, cropRect);
            
            
            //await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);
            
            await SaveImageAsync(croppedBitmap);
            CleanupAndClose();
        }
        catch (Exception ex)
        {
            CleanupAndClose();
        }
    }

    // im only keeping you because you were cool
    // I knew you would be useful...
    private void CleanupAndClose()
    {
        // clear window
        this.Opacity = 0;
        
       // take a reset man
       SelectionBox.Visibility = Visibility.Collapsed;
       SelectionBox.Width = 0;
       SelectionBox.Height = 0;
       
       // dim not tim
       DimTop.Width = DimTop.Height = 0;
       DimLeft.Width = DimLeft.Height = 0;
       DimRight.Width = DimRight.Height = 0;
       DimBottom.Width = DimBottom.Height = 0;
       
       // prevent ghosting pleasesese
       FrozenScreenImage.Source = null;
       
       //Close();
       this.Hide();
       
       // force wpf to update
       this.UpdateLayout();
       
       // prevent 
       GC.Collect();
    }
    
    private async System.Threading.Tasks.Task SaveImageAsync(BitmapSource freezeFrame)
    {
        // The UI is already gone, so this heavy lifting won't freeze your screen anymore
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
    
        // Save to disk in the background
        _ = System.Threading.Tasks.Task.Run(() => 
        {
            string screenshotsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
            Directory.CreateDirectory(screenshotsFolder);
            string filePath = Path.Combine(screenshotsFolder, $"QuickScope_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(freezeFrameToSave);
                encoder.Save(fileStream);
            }
            Console.WriteLine($"Saved capture to: {filePath}");
        });
    }
    
    private void SaveImage(BitmapSource freezeFrame)
    {
        var freezeFrameToSave = BitmapFrame.Create(freezeFrame);
        freezeFrameToSave.Freeze();
        
        
        CleanupAndClose();

        try
        {
            // save to clipboard
            Clipboard.SetImage(freezeFrame);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Clipboard failed: {ex.Message}");
        }
        
        System.Threading.Tasks.Task.Run(() => 
        {
            string screenshotsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
            Directory.CreateDirectory(screenshotsFolder);
            string filePath = Path.Combine(screenshotsFolder, $"QuickScope_{DateTime.Now:yyyyMMdd_HHmmss}.png");

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
        
        // top strip
        Canvas.SetLeft(DimTop, 0);
        Canvas.SetTop(DimTop, 0);
        DimTop.Width = canvasWidth;
        DimTop.Height = Math.Max(0, y);
        
        // bottom 
        Canvas.SetLeft(DimBottom, 0);
        Canvas.SetTop(DimBottom, y + h);
        DimBottom.Width = canvasWidth;
        DimBottom.Height = Math.Max(0, canvasHeight - y - h);
        
        // left
        Canvas.SetLeft(DimLeft, 0);
        Canvas.SetTop(DimLeft, y);
        DimLeft.Width = Math.Max(0, x);
        DimLeft.Height = Math.Max(0, h);
        
        // right
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