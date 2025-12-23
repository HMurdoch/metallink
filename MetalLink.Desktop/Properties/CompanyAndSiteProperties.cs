using System;
using System.Collections.ObjectModel;
using MetalLink.Shared.Customers;
using MetalLink.Shared.Locations;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    private string _newCompanyCreateName = "";
    public string NewCompanyCreateName
    {
        get => _newCompanyCreateName;
        set
        {
            if (_newCompanyCreateName == value) return;
            _newCompanyCreateName = value ?? "";
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateCompany));
            (CreateCompanyCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private string _newCompanyCreateSiteName = "";
    public string NewCompanyCreateSiteName
    {
        get => _newCompanyCreateSiteName;
        set
        {
            if (_newCompanyCreateSiteName == value) return;
            _newCompanyCreateSiteName = value ?? "";
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateCompany));
            (CreateCompanyCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    public bool CanCreateCompany =>
        !string.IsNullOrWhiteSpace(NewCompanyCreateName) &&
        !string.IsNullOrWhiteSpace(NewCompanyCreateSiteName);

}
