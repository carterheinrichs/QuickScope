using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using QuickScope.Services;

namespace QuickScope;

/// <summary>
/// Interaction logic for App.xaml
/// App will be triggered by a keystroke
/// </summary>
public partial class App : Application
{
    // P/Invoke Signatures to Native Windows API
    [DllImport( "user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport( "user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
    // Windows Constants
    private const int WM_HOTKEY = 0x0312;
    
    // Modifers
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    
    // Virtual Key Code for 'S'
    private const uint VK_S = 0x53;

    // id for main capture
    private const int HOTKEY_ID_CAPTURE = 1;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Register hotkeys
        // Passing IntPtr.Zero attaches the hotkey to the current thread rather than a specific window.
        bool registered = RegisterHotKey(IntPtr.Zero, HOTKEY_ID_CAPTURE, MOD_CONTROL | MOD_SHIFT, VK_S);

        if (!registered)
        {
            MessageBox.Show("Failed to register hotkeys. Another program might be using them", "QuickScope Err");
            Current.Shutdown();
            return;
        }
        
        //  Hook into the WPF component dispatcher to listen to raw Windows messages
        ComponentDispatcher.ThreadPreprocessMessage += OnThreadPreprocessMessage;
    }

    private void OnThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        // intercept the WM_HOTKEY message
        if (msg.message == WM_HOTKEY)
        {
            int id = msg.wParam.ToInt32();

            if (id == HOTKEY_ID_CAPTURE)
            {
                // call our screenshot service
                //CaptureService.CapturePrimaryScreenAsync();
                
                // wtf is this
                _ = Task.Run(async () =>
                {
                    try
                    {
                        //await CaptureService.CapturePrimaryScreenAsync();
                        string tempPath = await CaptureService.CapturePrimaryScreenAsync();
                        
                        // dispatch ui to open the freeze screen
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var selectionWindow = new SelectionWindow(tempPath);
                            selectionWindow.ShowDialog();
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"CAPTURE FAILED: {ex.Message}");
                    }
                });
                
                handled = true;
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // jannies
        ComponentDispatcher.ThreadPreprocessMessage -= OnThreadPreprocessMessage;
        UnregisterHotKey(IntPtr.Zero, HOTKEY_ID_CAPTURE);

        base.OnExit(e);
    }
}