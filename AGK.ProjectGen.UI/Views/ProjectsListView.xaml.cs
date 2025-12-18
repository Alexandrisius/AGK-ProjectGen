using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AGK.ProjectGen.UI.ViewModels;

namespace AGK.ProjectGen.UI.Views;

public partial class ProjectsListView : UserControl
{
    private ProjectsListViewModel? ViewModel => DataContext as ProjectsListViewModel;
    
    public ProjectsListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Подписываемся на изменения ColumnDefinitions
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            RebuildColumns();
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ProjectsListViewModel oldVm)
        {
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;
        }
        
        if (e.NewValue is ProjectsListViewModel newVm)
        {
            newVm.PropertyChanged += ViewModel_PropertyChanged;
            RebuildColumns();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectsListViewModel.ColumnDefinitions))
        {
            RebuildColumns();
        }
    }

    private void RebuildColumns()
    {
        if (ViewModel == null) return;
        if (ViewModel.ColumnDefinitions == null || !ViewModel.ColumnDefinitions.Any()) return;
        
        ProjectsDataGrid.Columns.Clear();
        
        foreach (var colDef in ViewModel.ColumnDefinitions.OrderBy(c => c.DisplayIndex))
        {
            DataGridColumn column;
            
            if (colDef.Key == "Name")
            {
                // Специальный шаблон для названия проекта с иконкой
                column = CreateNameColumn(colDef);
            }
            else if (colDef.Key == "LastGenerated")
            {
                // Форматирование даты
                column = new DataGridTextColumn
                {
                    Header = colDef.Header,
                    Binding = new Binding(colDef.BindingPath) { StringFormat = "dd.MM.yyyy HH:mm" },
                    Width = new DataGridLength(colDef.Width)
                };
            }
            else if (colDef.IsSystemColumn)
            {
                // Системные столбцы
                column = new DataGridTextColumn
                {
                    Header = colDef.Header,
                    Binding = new Binding(colDef.BindingPath),
                    Width = new DataGridLength(colDef.Width)
                };
            }
            else
            {
                // Динамические атрибуты - просто текстовый заголовок
                column = new DataGridTextColumn
                {
                    Header = colDef.Header,
                    Binding = new Binding($"AttributeValues[{colDef.Key}]") { TargetNullValue = "—" },
                    Width = new DataGridLength(colDef.Width)
                };
            }
            
            column.CanUserSort = true;
            column.CanUserReorder = true;
            column.CanUserResize = true;
            
            // Сохраняем ключ столбца для идентификации
            column.SetValue(TagProperty, colDef.Key);
            
            if (!colDef.IsVisible)
            {
                column.Visibility = Visibility.Collapsed;
            }
            
            ProjectsDataGrid.Columns.Add(column);
        }
    }

    private DataGridTemplateColumn CreateNameColumn(ProjectColumnDefinition colDef)
    {
        var column = new DataGridTemplateColumn
        {
            Header = colDef.Header,
            Width = new DataGridLength(colDef.Width)
        };
        
        // Создаём шаблон для ячейки
        var template = new DataTemplate();
        
        var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
        stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        stackPanelFactory.SetValue(StackPanel.MarginProperty, new Thickness(4, 0, 0, 0));
        
        var iconFactory = new FrameworkElementFactory(typeof(TextBlock));
        iconFactory.SetValue(TextBlock.TextProperty, "📁");
        iconFactory.SetValue(TextBlock.FontSizeProperty, 16.0);
        iconFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 10, 0));
        iconFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        
        var nameFactory = new FrameworkElementFactory(typeof(TextBlock));
        nameFactory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        nameFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        nameFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Medium);
        nameFactory.SetValue(TextBlock.FontSizeProperty, 14.0);
        
        stackPanelFactory.AppendChild(iconFactory);
        stackPanelFactory.AppendChild(nameFactory);
        
        template.VisualTree = stackPanelFactory;
        column.CellTemplate = template;
        
        return column;
    }

    private void ProjectsDataGrid_ColumnReordered(object? sender, EventArgs e)
    {
        SaveColumnSettings();
    }

    private async void SaveColumnSettings()
    {
        if (ViewModel == null) return;
        
        // Обновляем DisplayIndex и Width из DataGrid
        for (int i = 0; i < ProjectsDataGrid.Columns.Count; i++)
        {
            var column = ProjectsDataGrid.Columns[i];
            var key = column.GetValue(TagProperty) as string;
            
            if (!string.IsNullOrEmpty(key))
            {
                ViewModel.UpdateColumnOrder(key, column.DisplayIndex);
                
                // Получаем ширину столбца
                double width = 100;
                if (column.Width.IsAbsolute)
                {
                    width = column.Width.Value;
                }
                else if (column.ActualWidth > 0)
                {
                    width = column.ActualWidth;
                }
                else if (column.Width.DisplayValue > 0)
                {
                    width = column.Width.DisplayValue;
                }
                
                ViewModel.UpdateColumnWidth(key, width);
            }
        }
        
        await ViewModel.SaveColumnSettingsAsync();
    }

    // Обработчик для сохранения при изменении ширины столбца
    protected override void OnPreviewMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        
        // Небольшая задержка для того чтобы DataGrid обновил значения
        Dispatcher.BeginInvoke(new Action(() =>
        {
            SaveColumnSettings();
        }), System.Windows.Threading.DispatcherPriority.Background);
    }
}
