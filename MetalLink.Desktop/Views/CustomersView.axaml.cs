using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MetalLink.Desktop.Views;

public partial class CustomersView : UserControl
{
    private int _previousResultCount = 0;
    private Button? _searchButton;

    public CustomersView()
    {
        InitializeComponent();
        // NOTE: Selection is already handled via DataGrid SelectedItem binding.
        // Do NOT execute commands from PropertyChanged here; it causes recursion.
        DataContextChanged += OnDataContextChanged;
        Loaded += (s, e) => OnLoaded();
        
        // Hook up button click debugging
        Loaded += (s, e) => SetupButtonDebug();
    }
    
    private void SetupButtonDebug()
    {
        var clearBtn = this.FindControl<Button>("ClearCustomerButton");
        var createBtn = this.FindControl<Button>("CreateCustomerButton");
        var updateBtn = this.FindControl<Button>("UpdateCustomerButton");
        
        if (clearBtn != null)
        {
            clearBtn.Click += (s, e) => 
            {
                System.Console.WriteLine("[CustomersView] Clear button clicked");
                e.Handled = false; // Allow command to execute
            };
        }
        
        if (createBtn != null)
        {
            createBtn.Click += (s, e) => 
            {
                System.Console.WriteLine("[CustomersView] Create button clicked");
                e.Handled = false; // Allow command to execute
            };
        }
        
        if (updateBtn != null)
        {
            updateBtn.Click += (s, e) => 
            {
                System.Console.WriteLine("[CustomersView] Update button clicked");
                e.Handled = false; // Allow command to execute
            };
        }
    }

    private void OnLoaded()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            HookupSearchResultsMonitoring(vm);
        }
        
        // Expand Create/Edit panel on load
        var createEditArrow = this.FindControl<TextBlock>("CreateEditArrow");
        var createEditContent = this.FindControl<Control>("CreateEditContent");
        if (createEditArrow != null && createEditContent != null)
        {
            createEditArrow.Text = "▼";
            createEditContent.IsVisible = true;
        }
        
        // Set focus to Search button so user can press Enter to search
        _searchButton = this.FindControl<Button>("SearchCustomersButton");
        _searchButton?.Focus();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            HookupSearchResultsMonitoring(vm);
        }
    }

    private void HookupSearchResultsMonitoring(MainWindowViewModel vm)
    {
        // Listen to PropertyChanged for the ObservableCollection reference itself
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.PagedCustomerSearchResults))
            {
                // Collection reference changed, hook up to CollectionChanged
                HookupCollectionMonitoring(vm);
            }
        };
        
        // Also hook up immediately in case the collection already exists
        HookupCollectionMonitoring(vm);
    }

    private void HookupCollectionMonitoring(MainWindowViewModel vm)
    {
        var collection = vm.PagedCustomerSearchResults;
        if (collection == null) return;
        
        // Listen to collection changes (items added/removed)
        collection.CollectionChanged += async (s, e) =>
        {
            System.Console.WriteLine($"[CustomersView] PagedCustomerSearchResults collection changed: Count={collection.Count}, PreviousCount={_previousResultCount}");
            
            if (collection.Count > 0)
            {
                if (collection.Count != _previousResultCount)
                {
                    System.Console.WriteLine($"[CustomersView] Results count changed, expanding panels...");
                    // Small delay to ensure controls are rendered before expanding
                    await System.Threading.Tasks.Task.Delay(100);
                    
                    // Results changed, expand all three panels
                    ExpandSearchResultsAndDetails();
                    ExpandCreateEdit();
                    _previousResultCount = collection.Count;
                }
            }
            else if (collection.Count == 0)
            {
                System.Console.WriteLine($"[CustomersView] No results found");
                _previousResultCount = 0;
            }
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ToggleSearchCriteria(object? sender, PointerPressedEventArgs e)
    {
        var arrow = this.FindControl<TextBlock>("SearchCriteriaArrow");
        var content = this.FindControl<Grid>("SearchCriteriaContent");
        TogglePanel(arrow, content);
    }

    private void ToggleSearchResults(object? sender, PointerPressedEventArgs e)
    {
        var arrow = this.FindControl<TextBlock>("SearchResultsArrow");
        var content = this.FindControl<Grid>("SearchResultsContent");
        TogglePanel(arrow, content);
    }

    private void ToggleCustomerDetails(object? sender, PointerPressedEventArgs e)
    {
        var arrow = this.FindControl<TextBlock>("CustomerDetailsArrow");
        var content = this.FindControl<StackPanel>("CustomerDetailsContent");
        TogglePanel(arrow, content);
    }

    private void ToggleCreateEdit(object? sender, PointerPressedEventArgs e)
    {
        var arrow = this.FindControl<TextBlock>("CreateEditArrow");
        var content = this.FindControl<Grid>("CreateEditContent");
        TogglePanel(arrow, content);
    }

    private void TogglePanel(TextBlock? arrow, Control? content)
    {
        if (arrow == null || content == null) return;
        
        bool isCollapsed = arrow.Text == "▶";
        arrow.Text = isCollapsed ? "▼" : "▶";
        content.IsVisible = isCollapsed;
    }

    public void ExpandSearchResultsAndDetails()
    {
        ExpandPanel("SearchResultsArrow", "SearchResultsContent");
        ExpandPanel("CustomerDetailsArrow", "CustomerDetailsContent");
    }

    public void ExpandCreateEdit()
    {
        ExpandPanel("CreateEditArrow", "CreateEditContent");
    }

    private void ExpandPanel(string arrowName, string contentName)
    {
        var arrow = this.FindControl<TextBlock>(arrowName);
        var content = this.FindControl<Control>(contentName);
        
        System.Console.WriteLine($"[CustomersView] ExpandPanel: {arrowName} - arrow={arrow != null}, content={content != null}");
        
        if (arrow == null || content == null)
        {
            System.Console.WriteLine($"[CustomersView] ERROR: Could not find {arrowName} or {contentName}");
            return;
        }
        
        // Only expand if currently collapsed
        if (arrow.Text == "▶")
        {
            System.Console.WriteLine($"[CustomersView] Expanding {arrowName}");
            arrow.Text = "▼";
            content.IsVisible = true;
        }
        else
        {
            System.Console.WriteLine($"[CustomersView] {arrowName} already expanded");
        }
    }
}
