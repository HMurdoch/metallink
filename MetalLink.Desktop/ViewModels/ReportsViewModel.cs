using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MetalLink.Desktop.Services;
using MetalLink.Desktop.ViewModels;
using ReactiveUI;

namespace MetalLink.Desktop.ViewModels;

public class ReportsViewModel : ReactiveObject
{
    private bool _isCriteriaExpanded = true;
    public bool IsCriteriaExpanded 
    { 
        get => _isCriteriaExpanded; 
        set => this.RaiseAndSetIfChanged(ref _isCriteriaExpanded, value); 
    }

    private bool _isResultsExpanded = true;
    public bool IsResultsExpanded 
    { 
        get => _isResultsExpanded; 
        set => this.RaiseAndSetIfChanged(ref _isResultsExpanded, value); 
    }

    private readonly ApiClient _apiClient;

    public ReportsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }
}
