using System;
using System.IO;
using System.Media;
using QuickScope.Models;

namespace QuickScope.Services;

public class AudioService
{
    private static SoundPlayer? _player;
    private static Stream? _audioStream;

    public static void Initialize(string soundName)
    {
        _player?.Stop();
        _player?.Dispose();
        _audioStream?.Dispose();

        if (string.IsNullOrEmpty(soundName)) return;

        try
        {
            string manifestPath = $"QuickScope.Resources.{soundName}";
            _audioStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestPath);

            if (_audioStream != null)
            {
                _player = new SoundPlayer(_audioStream);
                _player.Load(); // Decodes header into memory instantly
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load sound: {ex.Message}");
        }
    }

    public static void PlayShutter()
    {
        if (SettingsManager.Current.PlaySnapSound && _player != null)
        {
            _player.Play();
        }
    }
}