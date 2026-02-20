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
        UpdateColorPreview();
    }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        // Ignore initialization
        if (!IsLoaded) return; 

        Models.SettingsManager.Current.ShowCrosshair = CrosshairCheck.IsChecked == true;
        Models.SettingsManager.Current.PlaySnapSound = SoundCheck.IsChecked == true;
        
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