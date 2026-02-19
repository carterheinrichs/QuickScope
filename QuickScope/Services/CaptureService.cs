using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Microsoft.Graphics.Canvas;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Media.Imaging;
using WinRT;

namespace QuickScope.Services;

//  The COM bridge
// This allows Win32/WPF desktop app to create a capture item from a monitor handle
[ComImport]
[Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComVisible(true)]
interface IGraphicsCaptureItemInterop
{
    IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);
    IntPtr CreateForMonitor([In] IntPtr monitor, [In] ref Guid iid);
}

public class CaptureService
{
    // Get primary moniter
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    private const uint MONITOR_DEFAULTTOPRIMARY = 1;

    public static async Task<BitmapImage> CapturePrimaryScreenAsync()
    {
        // Get the primary monitor handle
        IntPtr monitorHandle = MonitorFromWindow(IntPtr.Zero, MONITOR_DEFAULTTOPRIMARY);

        // create the GraphicsCaptureItem using our COM bridge
        var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
        Guid guid = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760"); // IGraphicsCaptureItem GUID

        IntPtr itemPointer = interop.CreateForMonitor(monitorHandle, ref guid);

        // .net 10 baby
        var captureItem = GraphicsCaptureItem.FromAbi(itemPointer);

        // prevent memory leak 
        Marshal.Release(itemPointer);

        if (captureItem == null) return null;

        // Initialize the GPU Device and Frame Pool
        var canvasDevice = new CanvasDevice();
        var direct3DDevice = (IDirect3DDevice)canvasDevice;

        var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            direct3DDevice,
            Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            captureItem.Size);

        var session = framePool.CreateCaptureSession(captureItem);

        // Disable the yellow capture border (Windows 11 feature)
        if (Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent(
                "Windows.Graphics.Capture.GraphicsCaptureSession", "IsBorderRequired"))
        {
            session.IsBorderRequired = false;
        }

        //  grab one frame
        var tcs = new TaskCompletionSource<CanvasBitmap>();

        framePool.FrameArrived += (s, e) =>
        {
            using var frame = s.TryGetNextFrame();
            if (frame != null)
            {
                // read the frame from the GPU into a CanvasBitmap
                var bitmap = CanvasBitmap.CreateFromDirect3D11Surface(canvasDevice, frame.Surface);
                tcs.TrySetResult(bitmap);
            }
        };

        // start the capture await first frame
        session.StartCapture();
        var capturedBitmap = await tcs.Task;

        //  stop capture and clean up
        session.Dispose();
        framePool.Dispose();

        // save to RAM instead =PPP
        using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        await capturedBitmap.SaveAsync(stream, CanvasBitmapFileFormat.Bmp); // bmp instead of png for SPEED
       
        // READ IMMEADATLY
        stream.Seek(0);
       
        // convert to WPF BitmapImage
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = stream.AsStream();
        bitmapImage.EndInit();
        bitmapImage.Freeze(); // let me touch the hot memory

        return bitmapImage;
        

        //  save to disk
        /*
        string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string screenShotsFolder = Path.Combine(picturesFolder, "Screenshots");
        Directory.CreateDirectory(screenShotsFolder); // ensure the dir exists (should)

        string filePath = Path.Combine(screenShotsFolder, $"QuickScope_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        await capturedBitmap.SaveAsync(filePath, CanvasBitmapFileFormat.Png);

        Console.WriteLine($"Saved full-screen capture to: {filePath}");

        // For clipboard need to stream this into a format WPF understands

        // copy to Windows Clipboard
        // load the saved file into a WPF-compatible BitmapImage
        var clipboardImage = new BitmapImage();
        clipboardImage.BeginInit();
        clipboardImage.CacheOption = BitmapCacheOption.OnLoad;
        clipboardImage.UriSource = new Uri(filePath);
        clipboardImage.EndInit();

        // Freeze is required to pass the image across different threads
        clipboardImage.Freeze();

        // the clipboard can only be accessed from a Single Thread Apartment (STA) UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            Clipboard.SetImage(clipboardImage);
            Console.WriteLine("Image successfully copied to clipboard!");
        });
        */
    }
}
        