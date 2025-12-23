using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.AccessControl;
using AGK.ProjectGen.Domain.Enums;

namespace AGK.ProjectGen.UI.ViewModels;

public partial class SecurityPrincipalsViewModel : ObservableObject
{
    private readonly ISecurityPrincipalRepository _repository;
    
    [ObservableProperty]
    private ObservableCollection<SecurityPrincipal> _groups = new();
    
    [ObservableProperty]
    private ObservableCollection<SecurityPrincipal> _users = new();
    
    [ObservableProperty]
    private SecurityPrincipal? _selectedGroup;
    
    [ObservableProperty]
    private SecurityPrincipal? _selectedUser;
    
    [ObservableProperty]
    private string _domainStatus = "Проверка подключения...";
    
    [ObservableProperty]
    private string _statusMessage = "";
    
    [ObservableProperty]
    private bool _isAdAvailable;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string _userSearchQuery = "";
    
    [ObservableProperty]
    private ObservableCollection<SecurityPrincipal> _searchResults = new();
    
    [ObservableProperty]
    private bool _isSearching;
    
    [ObservableProperty]
    private SecurityPrincipal? _selectedSearchResult;
    
    private string _configPath = "";
    
    public SecurityPrincipalsViewModel(ISecurityPrincipalRepository repository)
    {
        _repository = repository;
    }
    
    public async Task InitializeAsync()
    {
        IsLoading = true;
        
        try
        {
            // Загружаем текущую конфигурацию
            var config = await _repository.LoadAsync();
            Groups = new ObservableCollection<SecurityPrincipal>(config.Groups);
            Users = new ObservableCollection<SecurityPrincipal>(config.Users);
            
            // Проверяем доступность AD
            var (isAvailable, domainName) = await _repository.CheckActiveDirectoryAsync();
            IsAdAvailable = isAvailable;
            
            if (isAvailable)
            {
                DomainStatus = $"✅ Подключено к домену: {domainName}";
                StatusMessage = "Вы можете импортировать группы из Active Directory";
            }
            else
            {
                DomainStatus = "⚠️ Active Directory недоступен";
                StatusMessage = "Используйте ручной ввод или импорт локальных групп";
            }
            
            // Определяем путь к конфигу
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = System.IO.Path.Combine(appDir, "Config", "SecurityPrincipals.json");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task ImportFromAd()
    {
        IsLoading = true;
        StatusMessage = "Импорт групп из Active Directory...";
        
        try
        {
            var adGroups = await _repository.ImportFromActiveDirectoryAsync();
            
            if (adGroups.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Не удалось получить группы из Active Directory.\n\n" +
                    "Возможные причины:\n" +
                    "• Компьютер не в домене\n" +
                    "• Нет прав на чтение AD\n" +
                    "• Сетевые проблемы",
                    "Импорт не удался",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }
            
            var addedCount = await _repository.MergePrincipalsAsync(adGroups);
            
            // Перезагружаем данные
            var config = await _repository.LoadAsync();
            Groups = new ObservableCollection<SecurityPrincipal>(config.Groups);
            Users = new ObservableCollection<SecurityPrincipal>(config.Users);
            
            StatusMessage = $"Импортировано новых групп: {addedCount} из {adGroups.Count}";
            
            System.Windows.MessageBox.Show(
                $"Импорт завершён!\n\n" +
                $"Найдено групп в AD: {adGroups.Count}\n" +
                $"Добавлено новых: {addedCount}\n" +
                $"(дубликаты пропущены)",
                "Импорт из AD",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Ошибка импорта: {ex.Message}",
                "Ошибка",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task ImportFromLocal()
    {
        IsLoading = true;
        StatusMessage = "Импорт локальных групп и пользователей...";
        
        try
        {
            var localPrincipals = await _repository.ImportFromLocalMachineAsync();
            
            if (localPrincipals.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Не удалось получить локальные группы.",
                    "Импорт не удался",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }
            
            var addedCount = await _repository.MergePrincipalsAsync(localPrincipals);
            
            // Перезагружаем данные
            var config = await _repository.LoadAsync();
            Groups = new ObservableCollection<SecurityPrincipal>(config.Groups);
            Users = new ObservableCollection<SecurityPrincipal>(config.Users);
            
            StatusMessage = $"Импортировано новых: {addedCount} из {localPrincipals.Count}";
            
            System.Windows.MessageBox.Show(
                $"Импорт завершён!\n\n" +
                $"Найдено на локальной машине: {localPrincipals.Count}\n" +
                $"Добавлено новых: {addedCount}",
                "Импорт локальных",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Ошибка импорта: {ex.Message}",
                "Ошибка",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task AddManual()
    {
        var dialog = new Views.AddPrincipalDialog();
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true)
        {
            var newPrincipal = dialog.GetPrincipal();
            if (newPrincipal != null)
            {
                var added = await _repository.MergePrincipalsAsync(new List<SecurityPrincipal> { newPrincipal });
                
                if (added > 0)
                {
                    if (newPrincipal.Type == SecurityPrincipalType.Group)
                        Groups.Add(newPrincipal);
                    else
                        Users.Add(newPrincipal);
                    
                    StatusMessage = $"Добавлено: {newPrincipal.FullName}";
                }
                else
                {
                    StatusMessage = $"'{newPrincipal.FullName}' уже существует";
                }
            }
        }
    }
    
    [RelayCommand]
    private async Task RemoveGroup()
    {
        if (SelectedGroup == null) return;
        
        var result = System.Windows.MessageBox.Show(
            $"Удалить группу '{SelectedGroup.FullName}'?",
            "Подтверждение",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
            
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            var config = await _repository.LoadAsync();
            config.Groups.RemoveAll(g => g.FullName == SelectedGroup.FullName);
            await _repository.SaveAsync(config);
            
            Groups.Remove(SelectedGroup);
            StatusMessage = "Группа удалена";
        }
    }
    
    [RelayCommand]
    private async Task RemoveUser()
    {
        if (SelectedUser == null) return;
        
        var result = System.Windows.MessageBox.Show(
            $"Удалить пользователя '{SelectedUser.FullName}'?",
            "Подтверждение",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
            
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            var config = await _repository.LoadAsync();
            config.Users.RemoveAll(u => u.FullName == SelectedUser.FullName);
            await _repository.SaveAsync(config);
            
            Users.Remove(SelectedUser);
            StatusMessage = "Пользователь удалён";
        }
    }
    
    [RelayCommand]
    private void OpenConfigFile()
    {
        try
        {
            if (System.IO.File.Exists(_configPath))
            {
                Process.Start(new ProcessStartInfo(_configPath) { UseShellExecute = true });
            }
            else
            {
                System.Windows.MessageBox.Show(
                    $"Файл конфигурации не найден:\n{_configPath}",
                    "Файл не найден",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Не удалось открыть файл: {ex.Message}",
                "Ошибка",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
    
    [RelayCommand]
    private async Task SearchUsers()
    {
        if (string.IsNullOrWhiteSpace(UserSearchQuery) || UserSearchQuery.Length < 2)
        {
            StatusMessage = "Введите минимум 2 символа для поиска";
            return;
        }
        
        IsSearching = true;
        StatusMessage = $"Поиск \"{UserSearchQuery}\"...";
        SearchResults.Clear();
        
        try
        {
            var results = await _repository.SearchUsersInAdAsync(UserSearchQuery);
            
            if (results.Count == 0)
            {
                StatusMessage = $"По запросу \"{UserSearchQuery}\" ничего не найдено";
            }
            else
            {
                SearchResults = new ObservableCollection<SecurityPrincipal>(results);
                StatusMessage = $"Найдено: {results.Count} пользователей. Выберите и нажмите \"+ Добавить\"";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка поиска: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }
    
    [RelayCommand]
    private async Task AddFromSearch()
    {
        if (SelectedSearchResult == null)
        {
            StatusMessage = "Выберите пользователя из результатов поиска";
            return;
        }
        
        var added = await _repository.MergePrincipalsAsync(new List<SecurityPrincipal> { SelectedSearchResult });
        
        if (added > 0)
        {
            Users.Add(SelectedSearchResult);
            StatusMessage = $"Добавлен: {SelectedSearchResult.FullName}";
            
            // Убираем из результатов поиска
            SearchResults.Remove(SelectedSearchResult);
            SelectedSearchResult = null;
        }
        else
        {
            StatusMessage = $"'{SelectedSearchResult.FullName}' уже есть в списке";
        }
    }
    
    [RelayCommand]
    private async Task AddAllFromSearch()
    {
        if (SearchResults.Count == 0)
        {
            StatusMessage = "Нет результатов для добавления";
            return;
        }
        
        var toAdd = SearchResults.ToList();
        var added = await _repository.MergePrincipalsAsync(toAdd);
        
        // Перезагружаем данные
        var config = await _repository.LoadAsync();
        Users = new ObservableCollection<SecurityPrincipal>(config.Users);
        
        SearchResults.Clear();
        StatusMessage = $"Добавлено: {added} пользователей";
    }
}
