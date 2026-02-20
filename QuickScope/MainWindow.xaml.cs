using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QuickScope;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        var settings = Models.SettingsManager.Current;
        CrosshairCheck.IsChecked = settings.ShowCrosshair;
        SoundCheck.IsChecked = settings.PlaySnapSound;
        ColorHexBox.Text = settings.BorderColorHex;
        
        LoadEmbeddedAudioFiles(settings.SelectedSound);
        UpdateColorPreview();
    }

    private void LoadEmbeddedAudioFiles(string savedSound)
    {
        SoundDropdown.Items.Clear();

        // Get all resource paths embedded in the executable
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        string[] resourceNames = assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
        {
            if (resourceName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                // .NET embed names usually look like: "QuickScope.Resources.gun-gunshot-01.wav"
                // Let's accurately grab the end of it so it always works.
                int lastDotIndex = resourceName.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    int secondToLastDotIndex = resourceName.LastIndexOf('.', lastDotIndex - 1);
                    if (secondToLastDotIndex >= 0)
                    {
                        string fileName = resourceName.Substring(secondToLastDotIndex + 1);
                        SoundDropdown.Items.Add(fileName);
                    }
                    else
                    {
                        SoundDropdown.Items.Add(resourceName);
                    }
                }
            }
        }

        if (SoundDropdown.Items.Contains(savedSound))
        {
            SoundDropdown.SelectedItem = savedSound;
        }
        else if (SoundDropdown.Items.Count > 0)
        {
            SoundDropdown.SelectedIndex = 0;
        }
    }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        // Ignore initialization
        if (!IsLoaded) return; 

        Models.SettingsManager.Current.ShowCrosshair = CrosshairCheck.IsChecked == true;
        Models.SettingsManager.Current.PlaySnapSound = SoundCheck.IsChecked == true;
        
        Models.SettingsManager.Save();
    }

    private void SoundDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || SoundDropdown.SelectedItem == null) return;
        
        Models.SettingsManager.Current.SelectedSound = SoundDropdown.SelectedItem.ToString()!;
        Models.SettingsManager.Save();
    }

    private void ColorHexBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;

        UpdateColorPreview();
        Models.SettingsManager.Current.BorderColorHex = ColorHexBox.Text;
        Models.SettingsManager.Save();
    }

    private void UpdateColorPreview()
    {
        try
        {
            var brush = Models.SettingsManager.Current.BorderBrush;
            ColorPreview.Fill = brush;
        }
        catch { /* Invalid hex, don't update preview */ }
    }
}