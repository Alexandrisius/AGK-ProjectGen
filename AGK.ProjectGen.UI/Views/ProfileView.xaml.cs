using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace AGK.ProjectGen.UI.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();
    }

    private void OnStructureNodeSelected(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is AGK.ProjectGen.UI.ViewModels.ProfileViewModel vm && e.NewValue is AGK.ProjectGen.Domain.Schema.StructureNodeDefinition node)
        {
            vm.SelectedStructureNode = node;
            UpdateSelectedDictionaryItems(vm, node);
        }
        else if (DataContext is AGK.ProjectGen.UI.ViewModels.ProfileViewModel vm2)
        {
            vm2.SelectedStructureNode = null;
            vm2.SelectedDictionaryItems.Clear();
        }
    }

    /// <summary>
    /// Обновляет список элементов словаря на основе SourceKey выбранного узла.
    /// Сохраняет текущее значение SelectedItemCode чтобы избежать сброса при очистке списка.
    /// </summary>
    private void UpdateSelectedDictionaryItems(AGK.ProjectGen.UI.ViewModels.ProfileViewModel vm, AGK.ProjectGen.Domain.Schema.StructureNodeDefinition node)
    {
        // Сохраняем текущее значение до очистки
        var savedCode = node.SelectedItemCode;
        
        vm.SelectedDictionaryItems.Clear();
        
        if (!string.IsNullOrEmpty(node.SourceKey) && vm.SelectedProfile != null)
        {
            var dict = vm.SelectedProfile.Dictionaries.FirstOrDefault(d => d.Key == node.SourceKey);
            if (dict != null)
            {
                foreach (var item in dict.Items)
                {
                    vm.SelectedDictionaryItems.Add(item);
                }
            }
        }
        
        // Восстанавливаем значение после заполнения списка
        // Используем Dispatcher для гарантии, что биндинг успеет отработать
        if (!string.IsNullOrEmpty(savedCode))
        {
            Dispatcher.BeginInvoke(() => { node.SelectedItemCode = savedCode; }, 
                System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
    
    /// <summary>
    /// Обработчик клика на узел структуры.
    /// Решает проблему когда ProjectRoot — единственный узел без дочерних
    /// и TreeView не регистрирует SelectedItemChanged.
    /// </summary>
    private void OnStructureNodeClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement fe &&
            fe.DataContext is AGK.ProjectGen.Domain.Schema.StructureNodeDefinition node &&
            DataContext is AGK.ProjectGen.UI.ViewModels.ProfileViewModel vm)
        {
            vm.SelectedStructureNode = node;
        }
    }
    
    /// <summary>
    /// Обработчик изменения выбора словаря (SourceKey).
    /// Обновляет список элементов для Single-узлов.
    /// </summary>
    private void OnSourceKeyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AGK.ProjectGen.UI.ViewModels.ProfileViewModel vm && 
            vm.SelectedStructureNode != null)
        {
            UpdateSelectedDictionaryItems(vm, vm.SelectedStructureNode);
        }
    }
}
