using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace QuickScope.Models;

public class AppSettings
{
    // Default Settings (MINE)
    public string BorderColorHex { get; set; } = "#FFFFFF";
    public bool ShowCrosshair { get; set; } = true;
    public bool PlaySnapSound { get; set; } = true;

    [System.Text.Json.Serialization.JsonIgnore]
    public SolidColorBrush BorderBrush
    {
        get
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(BorderColorHex);
                return new SolidColorBrush(color);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse BorderColorHex: {ex.Message}");
                return Brushes.White;
            }
        }
    }
}

public static class SettingsManager
{
    private static readonly string SettingsFile = Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
        "QuickScope", "settings.json");
    
    public static AppSettings Current { get; private set; } = new AppSettings();

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
            string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

}