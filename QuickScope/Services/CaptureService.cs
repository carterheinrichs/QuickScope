using System.Windows;
using System.Windows.Media.Imaging;

namespace QuickScope.Services;

public class CaptureService
{
    private static System.Drawing.Bitmap? _cachedBitmap;
    private static System.Drawing.Graphics? _cachedGraphics;
    private static int _virtualLeft;
    private static int _virtualTop;
    private static int _width;
    private static int _height;
    
    // better?
    public static BitmapSource CaptureInstant()
    {
        // get current dimensions of the entire multi-monitor setup if so
        int currentLeft = (int)SystemParameters.VirtualScreenLeft;
        int currentTop = (int)SystemParameters.VirtualScreenTop;
        int currentWidth = (int)SystemParameters.VirtualScreenWidth;
        int currentHeight = (int)SystemParameters.VirtualScreenHeight;
        
        // reallocate cache onlt if screen configs changes or on first run
        if (_cachedBitmap == null || _width != currentWidth || _height != currentHeight || _virtualLeft != currentLeft || _virtualTop != currentTop)
        {
            _virtualLeft = currentLeft;
            _virtualTop = currentTop;
            _width = currentWidth;
            _height = currentHeight;
            
            _cachedBitmap?.Dispose();
            _cachedGraphics?.Dispose();
            
            _cachedBitmap = new System.Drawing.Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            _cachedGraphics = System.Drawing.Graphics.FromImage(_cachedBitmap);
        }

        // pass the abs left and top BitBlt from screen space coords
        _cachedGraphics!.CopyFromScreen(
            _virtualLeft,
            _virtualTop,
            0,
            0,
            _cachedBitmap.Size,
            System.Drawing.CopyPixelOperation.SourceCopy
            );

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