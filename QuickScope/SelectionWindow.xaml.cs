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
        FrozenScreenImage.Source = freezeFrame;
        
        
        this.Show();
        this.Activate(); // come up to the front of the class
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

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            var freezeFrame = (BitmapSource)FrozenScreenImage.Source;
            SaveImage(freezeFrame);
            CleanupAndClose();
        }
    }

    private void CropAndSave()
    {
        try
        {
            double x = Canvas.GetLeft(SelectionBox);
            double y = Canvas.GetTop(SelectionBox);

            var originalImage = (BitmapSource)FrozenScreenImage.Source;

            // Calculate DPI scaling ratio (WPF layout sizes vs Actual Image Pixels)
            double scaleX = originalImage.PixelWidth / this.ActualWidth;
            double scaleY = originalImage.PixelHeight / this.ActualHeight;

            Int32Rect cropRect = new Int32Rect(
                (int)(x * scaleX),
                (int)(y * scaleY),
                (int)(SelectionBox.Width * scaleX),
                (int)(SelectionBox.Height * scaleY));

            var croppedBitmap = new CroppedBitmap(originalImage, cropRect);
            
            SaveImage(croppedBitmap);
        }
        finally
        {
            CleanupAndClose();
        }
    }

    // im only keeping you because you were cool
    // I knew you would be useful...
    private void CleanupAndClose()
    {
        //reset the box
        SelectionBox.Visibility = Visibility.Collapsed;
        SelectionBox.Width = 0;
        SelectionBox.Height = 0;
        
        //Close();
        this.Hide();
    }

    private void SaveImage(BitmapSource freezeFrame)
    {
        // save to clipboard
        Clipboard.SetImage(freezeFrame);
        
        // save to disk
        string screenshotsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
        Directory.CreateDirectory(screenshotsFolder);
        string filePath = Path.Combine(screenshotsFolder, $"QuickScope_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(freezeFrame));
            encoder.Save(fileStream);
        }

        Console.WriteLine($"Saved capture to: {filePath}");
        
        
    }
}