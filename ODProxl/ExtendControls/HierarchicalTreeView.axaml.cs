using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System.Collections;

namespace ODProxl.ExtendControls
{
    public partial class HierarchicalTreeView : UserControl
    {
        public static readonly StyledProperty<IEnumerable> ItemsSourceProperty =
            AvaloniaProperty.Register<HierarchicalTreeView, IEnumerable>(nameof(ItemsSource));

        public IEnumerable ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly StyledProperty<IDataTemplate> NodeTemplateProperty =
            AvaloniaProperty.Register<HierarchicalTreeView, IDataTemplate>(nameof(NodeTemplate));

        public IDataTemplate NodeTemplate
        {
            get => GetValue(NodeTemplateProperty);
            set => SetValue(NodeTemplateProperty, value);
        }

        public static readonly StyledProperty<IDataTemplate> GroupHeaderTemplateProperty =
            AvaloniaProperty.Register<HierarchicalTreeView, IDataTemplate>(nameof(GroupHeaderTemplate));

        public IDataTemplate GroupHeaderTemplate
        {
            get => GetValue(GroupHeaderTemplateProperty);
            set => SetValue(GroupHeaderTemplateProperty, value);
        }

        public HierarchicalTreeView()
        {
            InitializeComponent();
        }
    }
}