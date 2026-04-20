using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MetalLink.Desktop.Views;

public partial class ProductImageWindow : Window
{
    public ProductImageWindow()
    {
        InitializeComponent();
    }

    public async Task LoadImageAsync(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                var bitmap = new Bitmap(stream);
                var imageControl = this.FindControl<Image>("MainImage");
                if (imageControl != null)
                {
                    imageControl.Source = bitmap;
                }
            }
        }
        catch { }
    }

    private void CloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
