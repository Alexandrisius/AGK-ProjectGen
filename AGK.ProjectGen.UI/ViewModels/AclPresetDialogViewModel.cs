using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Schema;

namespace AGK.ProjectGen.UI.ViewModels;

public partial class AclPresetDialogViewModel : ObservableObject
{
    private readonly IAclPresetRepository _presetRepository;
    private readonly ISecurityPrincipalRepository _principalRepository;
    private IEnumerable<DictionarySchema> _dictionaries = Array.Empty<DictionarySchema>();
    
    [ObservableProperty]
    private ObservableCollection<AclPreset> _presets = new();
    
    [ObservableProperty]
    private AclPreset? _selectedPreset;
    
    [ObservableProperty]
    private string _mode = string.Empty;
    
    [ObservableProperty]
    private bool _isLoadMode;
    
    [ObservableProperty]
    private bool _hasNoPresets;
    
    public bool CanLoadPreset => SelectedPreset != null && IsLoadMode;
    
    public AclPresetDialogViewModel(
        IAclPresetRepository presetRepository, 
        ISecurityPrincipalRepository principalRepository)
    {
        _presetRepository = presetRepository;
        _principalRepository = principalRepository;
    }
    
    /// <summary>
    /// Устанавливает словари профиля для использования в редакторе ACL.
    /// </summary>
    public void SetDictionaries(IEnumerable<DictionarySchema> dictionaries)
    {
        _dictionaries = dictionaries;
    }
    
    /// <summary>
    /// Устанавливает словари и атрибуты профиля для использования в редакторе ACL.
    /// </summary>
    public void SetProfileData(IEnumerable<DictionarySchema> dictionaries, IEnumerable<FieldSchema>? attributes)
    {
        _dictionaries = dictionaries;
        _attributes = attributes;
    }
    
    private IEnumerable<FieldSchema>? _attributes;
    
    /// <summary>
    /// Инициализирует диалог в режиме загрузки (для применения пресета).
    /// </summary>
    public async Task InitializeForLoad()
    {
        IsLoadMode = true;
        Mode = "Выберите пресет для загрузки в текущую папку";
        await LoadPresetsAsync();
    }
    
    /// <summary>
    /// Инициализирует диалог в режиме управления (просмотр, редактирование).
    /// </summary>
    public async Task InitializeForManage()
    {
        IsLoadMode = false;
        Mode = "Управление сохранёнными пресетами";
        await LoadPresetsAsync();
    }
    
    private async Task LoadPresetsAsync()
    {
        var presets = await _presetRepository.GetAllAsync();
        Presets = new ObservableCollection<AclPreset>(presets);
        HasNoPresets = Presets.Count == 0;
    }
    
    partial void OnSelectedPresetChanged(AclPreset? value)
    {
        OnPropertyChanged(nameof(CanLoadPreset));
    }
    
    [RelayCommand]
    private async Task DeletePreset()
    {
        if (SelectedPreset == null) return;
        
        var result = System.Windows.MessageBox.Show(
            $"Удалить пресет \"{SelectedPreset.Name}\"?",
            "Подтверждение удаления",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
            
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await _presetRepository.DeleteAsync(SelectedPreset.Id);
            Presets.Remove(SelectedPreset);
            SelectedPreset = Presets.FirstOrDefault();
            HasNoPresets = Presets.Count == 0;
        }
    }
    
    [RelayCommand]
    private async Task SavePreset()
    {
        if (SelectedPreset == null) return;
        
        if (string.IsNullOrWhiteSpace(SelectedPreset.Name))
        {
            System.Windows.MessageBox.Show(
                "Введите название пресета",
                "Ошибка",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }
        
        await _presetRepository.SaveAsync(SelectedPreset);
        
        System.Windows.MessageBox.Show(
            "Пресет сохранён",
            "Успешно",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }
    
    [RelayCommand]
    private void EditPresetRules()
    {
        if (SelectedPreset == null) return;
        
        var viewModel = new AclRuleEditorViewModel(_principalRepository);
        viewModel.LoadRules(SelectedPreset.Rules, $"Пресет: {SelectedPreset.Name}");
        viewModel.LoadAttributes(_dictionaries, _attributes);
        
        var dialog = new Views.AclRuleEditorDialog
        {
            DataContext = viewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };
        
        if (dialog.ShowDialog() == true && dialog.DialogResultOk)
        {
            // Применяем изменения - переносим из копии в оригинальную коллекцию
            viewModel.ApplyChanges();
            OnPropertyChanged(nameof(SelectedPreset));
        }
    }
    
    /// <summary>
    /// Получить выбранный пресет для загрузки.
    /// </summary>
    public AclPreset? GetSelectedPreset() => SelectedPreset;
}

