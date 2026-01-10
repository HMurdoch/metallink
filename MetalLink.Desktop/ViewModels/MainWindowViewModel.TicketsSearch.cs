using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Shared.Tickets;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // --- Ticket search backing fields ---

    private string _searchTicketCustomerIdText = string.Empty;
    private string _searchTicketIdNumberText = string.Empty;
    private string _searchTicketFirstNameText = string.Empty;
    private string _searchTicketLastNameText = string.Empty;
    private string _searchTicketAccountNumberText = string.Empty;
    private string _searchTicketNumberText = string.Empty;
    private string _searchTicketTypeKey = string.Empty; // reuse TicketTypeOptions keys
    private string _searchTicketCreatedFromText = string.Empty;
    private string _searchTicketCreatedToText = string.Empty;

    public string SearchTicketCustomerIdText
    {
        get => _searchTicketCustomerIdText;
        set { _searchTicketCustomerIdText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketIdNumberText
    {
        get => _searchTicketIdNumberText;
        set { _searchTicketIdNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketFirstNameText
    {
        get => _searchTicketFirstNameText;
        set { _searchTicketFirstNameText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketLastNameText
    {
        get => _searchTicketLastNameText;
        set { _searchTicketLastNameText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketAccountNumberText
    {
        get => _searchTicketAccountNumberText;
        set { _searchTicketAccountNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketNumberText
    {
        get => _searchTicketNumberText;
        set { _searchTicketNumberText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketTypeKey
    {
        get => _searchTicketTypeKey;
        set { _searchTicketTypeKey = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketCreatedFromText
    {
        get => _searchTicketCreatedFromText;
        set { _searchTicketCreatedFromText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string SearchTicketCreatedToText
    {
        get => _searchTicketCreatedToText;
        set { _searchTicketCreatedToText = value ?? string.Empty; OnPropertyChanged(); }
    }

    // --- Search results ---

    public ObservableCollection<TicketSearchResultDto> TicketSearchResults { get; } = new();

    private TicketSearchResultDto? _selectedTicket;
    public TicketSearchResultDto? SelectedTicket
    {
        get => _selectedTicket;
        set
        {
            _selectedTicket = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedTicketSummary));
        }
    }

    public string SelectedTicketSummary
    {
        get
        {
            if (SelectedTicket is null)
                return "No ticket selected.";

            return $"Ticket {SelectedTicket.TicketNumber} ({SelectedTicket.TicketType}) - " +
                   $"Customer {SelectedTicket.CustomerId}, Net {SelectedTicket.NetWeightKg:N2} kg, " +
                   $"Total {SelectedTicket.TotalInclVat:N2}";
        }
    }

    private long? ParseLongOrNull(string text)
    {
        var t = (text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(t)) return null;
        if (t.All(c => c == '0')) return null;
        return long.TryParse(t, out var v) ? v : null;
    }

    private DateTimeOffset? ParseDateOrNull(string text)
    {
        var t = (text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(t)) return null;
        if (DateTimeOffset.TryParse(t, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var dt))
            return dt;
        if (DateTimeOffset.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dt))
            return dt;
        return null;
    }

    private TicketTypeOption? _selectedSearchTicketTypeOption;

    public TicketTypeOption? SelectedSearchTicketTypeOption
    {
        get => _selectedSearchTicketTypeOption;
        set
        {
            _selectedSearchTicketTypeOption = value;
            OnPropertyChanged();

            SearchTicketTypeKey = value?.Key ?? string.Empty;
        }
    }

    private async Task SearchTicketsAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Searching tickets...";

        try
        {
            var request = new TicketSearchRequestDto
            {
                CustomerId = ParseLongOrNull(SearchTicketCustomerIdText),
                IdNumber = string.IsNullOrWhiteSpace(SearchTicketIdNumberText) ? null : SearchTicketIdNumberText.Trim(),
                FirstName = string.IsNullOrWhiteSpace(SearchTicketFirstNameText) ? null : SearchTicketFirstNameText.Trim(),
                LastName = string.IsNullOrWhiteSpace(SearchTicketLastNameText) ? null : SearchTicketLastNameText.Trim(),
                AccountNumber = ParseLongOrNull(SearchTicketAccountNumberText),
                TicketNumber = string.IsNullOrWhiteSpace(SearchTicketNumberText) ? null : SearchTicketNumberText.Trim(),
                TicketType = string.IsNullOrWhiteSpace(SearchTicketTypeKey) ? null : SearchTicketTypeKey.Trim(),
                CreatedFrom = ParseDateOrNull(SearchTicketCreatedFromText),
                CreatedTo = ParseDateOrNull(SearchTicketCreatedToText)
            };

            var results = await _ticketService.SearchTicketsAsync(request);

            TicketSearchResults.Clear();
            foreach (var t in results)
            {
                TicketSearchResults.Add(t);
            }

            SelectedTicket = TicketSearchResults.FirstOrDefault();

            StatusMessage = $"Loaded {TicketSearchResults.Count} ticket(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ticket search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearTicketSearch()
    {
        SearchTicketCustomerIdText = string.Empty;
        SearchTicketIdNumberText = string.Empty;
        SearchTicketFirstNameText = string.Empty;
        SearchTicketLastNameText = string.Empty;
        SearchTicketAccountNumberText = string.Empty;
        SearchTicketNumberText = string.Empty;
        SearchTicketTypeKey = string.Empty;
        SearchTicketCreatedFromText = string.Empty;
        SearchTicketCreatedToText = string.Empty;
        SelectedSearchTicketTypeOption = null;

        TicketSearchResults.Clear();
        SelectedTicket = null;
    }

    private async Task DeleteTicketAsync(TicketSearchResultDto? ticket)
    {
        var target = ticket ?? SelectedTicket;
        if (target is null)
            return;

        if (IsBusy)
            return;

        var ok = await ConfirmAsync($"Are you sure you want to delete ticket {target.TicketNumber}?");
        if (!ok)
            return;

        IsBusy = true;
        try
        {
            await _ticketService.DeleteTicketAsync(target.TicketId);

            TicketSearchResults.Remove(target);
            if (ReferenceEquals(SelectedTicket, target))
            {
                SelectedTicket = TicketSearchResults.FirstOrDefault();
            }

            StatusMessage = $"Ticket {target.TicketNumber} deleted (soft).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete ticket failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
