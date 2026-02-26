using System;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using QuickScope.Models;

namespace QuickScope.Services;

public class AudioService
{
    private static byte[]? _shutterBytes;

    // Direct native call to the core Windows multimedia API
    [DllImport("winmm.dll", SetLastError = true)]
    private static extern bool PlaySound(byte[]? ptrToSound, IntPtr hmod, uint fdwSound);
    
    private const uint SND_ASYNC = 0x0001;    // Play asynchronously without blocking the UI
    private const uint SND_MEMORY = 0x0004;   // pszSound points to a memory file


    public static void Initialize(string soundName)
    {
        _shutterBytes = null;

        if (string.IsNullOrEmpty(soundName)) return;

        try
        {
            string manifestPath = $"QuickScope.Resources.{soundName}";
            using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestPath);

            if (stream != null)
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                _shutterBytes = ms.ToArray();
                
                // Pre-warm the P/Invoke boundary! 
                // Calling this with null forces the .NET JIT compiler to resolve the memory address
                // for winmm.dll immediately, rather than waiting for your first fast-click.
                PlaySound(null, IntPtr.Zero, 0); 
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load sound: {ex.Message}");
        }
    }

    public static void PlayShutter()
    {
        if (SettingsManager.Current.PlaySnapSound && _shutterBytes != null)
        {
            // Fires instantly in the native unmanaged layer - zero ThreadPool delay!
            PlaySound(_shutterBytes, IntPtr.Zero, SND_ASYNC | SND_MEMORY);
        }
    }
}