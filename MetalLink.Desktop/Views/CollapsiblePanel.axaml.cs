using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace MetalLink.Desktop.Views
{
    public partial class CollapsiblePanel : UserControl
    {
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<CollapsiblePanel, string>(nameof(Title), "Panel");

        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<CollapsiblePanel, bool>(nameof(IsExpanded), true);

        public static readonly StyledProperty<object> PanelContentProperty =
            AvaloniaProperty.Register<CollapsiblePanel, object>(nameof(PanelContent));

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public object PanelContent
        {
            get => GetValue(PanelContentProperty);
            set => SetValue(PanelContentProperty, value);
        }

        private Border? _contentArea;
        private Path? _toggleArrow;
        private TextBlock? _panelTitle;
        private ContentPresenter? _contentPresenter;

        public CollapsiblePanel()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            _contentArea = this.FindControl<Border>("ContentArea");
            _toggleArrow = this.FindControl<Path>("ToggleArrow");
            _panelTitle = this.FindControl<TextBlock>("PanelTitle");
            _contentPresenter = this.FindControl<ContentPresenter>("ContentPresenter");

            // Initialize based on IsExpanded property
            UpdateExpandedState(false);
            
            // Bind properties
            if (_panelTitle != null)
            {
                this.GetObservable(TitleProperty).Subscribe(title => _panelTitle.Text = title);
            }

            if (_contentPresenter != null)
            {
                this.GetObservable(PanelContentProperty).Subscribe(content => _contentPresenter.Content = content);
            }

            this.GetObservable(IsExpandedProperty).Subscribe(isExpanded => UpdateExpandedState(true));
        }

        private void ToggleButton_Click(object? sender, RoutedEventArgs e)
        {
            IsExpanded = !IsExpanded;
        }

        private void UpdateExpandedState(bool animate)
        {
            if (_contentArea == null || _toggleArrow == null) return;

            if (IsExpanded)
            {
                // Expanded: arrow points down, content visible
                if (_toggleArrow.RenderTransform is RotateTransform rotateTransform)
                {
                    rotateTransform.Angle = 90;
                }
                _contentArea.MaxHeight = double.PositiveInfinity;
            }
            else
            {
                // Collapsed: arrow points right, content hidden
                if (_toggleArrow.RenderTransform is RotateTransform rotateTransform)
                {
                    rotateTransform.Angle = 0;
                }
                _contentArea.MaxHeight = 0;
            }
        }
    }
}
