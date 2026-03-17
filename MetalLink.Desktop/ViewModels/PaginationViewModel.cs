using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;

namespace MetalLink.Desktop.ViewModels;

public partial class PaginationViewModel : ObservableObject
{
    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private int pageSize = 20;

    [ObservableProperty]
    private int totalRecords = 0;

    [ObservableProperty]
    private ObservableCollection<int> pageSizeOptions = new ObservableCollection<int> { 10, 15, 20, 40, 60, 80, 100 };

    public int TotalPages => TotalRecords > 0 ? (TotalRecords + PageSize - 1) / PageSize : 1;

    public bool CanGoPrevious => CurrentPage > 1;

    public bool CanGoNext => CurrentPage < TotalPages;

    [RelayCommand]
    private void PreviousPage()
    {
        if (CanGoPrevious)
        {
            CurrentPage--;
            OnPageChanged();
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CanGoNext)
        {
            CurrentPage++;
            OnPageChanged();
        }
    }

    partial void OnPageSizeChanged(int value)
    {
        // Reset to first page when page size changes
        CurrentPage = 1;
        OnPageChanged();
    }

    public event EventHandler<EventArgs>? PageChanged;

    private void OnPageChanged()
    {
        PageChanged?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
    }

    public void Reset()
    {
        CurrentPage = 1;
        TotalRecords = 0;
        // Keep current PageSize
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
    }

    public void SetTotalRecords(int total)
    {
        TotalRecords = total;
        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
        }
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
    }

    public int GetSkip()
    {
        return (CurrentPage - 1) * PageSize;
    }

    public int GetTake()
    {
        return PageSize;
    }
}
