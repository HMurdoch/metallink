using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views
{
    /// <summary>
    /// Maps EnumMainSection to the corresponding view.
    /// IMPORTANT:
    /// Do NOT set DataContext here, so the DataContext from MainWindow
    /// (MainWindowViewModel) flows down into the child views.
    /// </summary>
    public class SectionToViewConverter : IValueConverter
    {
        public static readonly SectionToViewConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not EnumMainSection section)
                return null;

            return section switch
            {
                EnumMainSection.Dashboard => new DashboardView(),
                EnumMainSection.Customers => new CustomersView(),
                EnumMainSection.Tickets   => new TicketsView(),
                EnumMainSection.Documents => new DocumentsView(),
                EnumMainSection.Camera    => new CameraView(),
                _                         => null
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
