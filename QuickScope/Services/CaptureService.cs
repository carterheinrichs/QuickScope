using System.Windows;
using System.Windows.Media.Imaging;

namespace QuickScope.Services;

public class CaptureService
{
    private static System.Drawing.Bitmap? _cachedBitmap;
    private static System.Drawing.Graphics? _cachedGraphics;
    private static int _width;
    private static int _height;
    
    // better?
    public static BitmapSource CaptureInstant()
    {
        if (_cachedBitmap == null)
        {
            _width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            _height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            
            _cachedBitmap = new System.Drawing.Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            _cachedGraphics = System.Drawing.Graphics.FromImage(_cachedBitmap);
        }

        // The Native GDI+ BitBlt approach (Instantaneous)
        _cachedGraphics!.CopyFromScreen(0, 0, 0, 0, _cachedBitmap.Size, System.Drawing.CopyPixelOperation.SourceCopy);

        // Fast conversion to WPF BitmapSource avoiding unmanaged handles (HBITMAP)
        var bitmapData = _cachedBitmap.LockBits(
            new System.Drawing.Rectangle(0, 0, _width, _height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

        try
        {
            // WriteableBitmap is intensely optimized for fast bulk-pixel writes in WPF
            var writeableBitmap = new WriteableBitmap(
                _width, _height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32, null);
                
            writeableBitmap.WritePixels(
                new Int32Rect(0, 0, _width, _height),
                bitmapData.Scan0,
                bitmapData.Stride * _height,
                bitmapData.Stride);
                
            writeableBitmap.Freeze();
            return writeableBitmap;
        }
        finally
        {
            _cachedBitmap.UnlockBits(bitmapData);
        }
    }
}