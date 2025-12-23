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
    private ObservableCollection<SecurityPrincipal> _selectedGroups = new();
    
    [ObservableProperty]
    private ObservableCollection<SecurityPrincipal> _selectedUsers = new();
    
    [ObservableProperty]
    private string _domainStatus = "Проверка подключения...";
    
    [ObservableProperty]
    private string _statusMessage = "";
    
    [ObservableProperty]
    private bool _isAdAvailable;
    
    [ObservableProperty]
    private bool _isLoading;
    
    // Поиск пользователей
    [ObservableProperty]
    private string _userSearchQuery = "";
    
    [ObservableProperty]
    private ObservableCollection<SecurityPrincipal> _userSearchResults = new();
    
    [ObservableProperty]
    private bool _isSearchingUsers;
    
    [ObservableProperty]
    private SecurityPrincipal? _selectedUserSearchResult;
    
    // Поиск групп
    [ObservableProperty]
    private string _groupSearchQuery = "";
    
    [ObservableProperty]
    private ObservableCollection<SecurityPrincipal> _groupSearchResults = new();
    
    [ObservableProperty]
    private bool _isSearchingGroups;
    
    [ObservableProperty]
    private SecurityPrincipal? _selectedGroupSearchResult;
    
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
            var config = await _repository.LoadAsync();
            Groups = new ObservableCollection<SecurityPrincipal>(config.Groups);
            Users = new ObservableCollection<SecurityPrincipal>(config.Users);
            
            var (isAvailable, domainName) = await _repository.CheckActiveDirectoryAsync();
            IsAdAvailable = isAvailable;
            
            if (isAvailable)
            {
                DomainStatus = $"✅ Подключено к домену: {domainName}";
                StatusMessage = "Используйте поиск для добавления групп и пользователей из AD";
            }
            else
            {
                DomainStatus = "⚠️ Active Directory недоступен";
                StatusMessage = "Используйте ручной ввод для добавления";
            }
            
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = System.IO.Path.Combine(appDir, "Config", "SecurityPrincipals.json");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Сохраняет текущее состояние групп и пользователей в конфиг.
    /// Вызывается после редактирования описания.
    /// </summary>
    public async Task SaveChangesAsync()
    {
        try
        {
            var config = new SecurityPrincipalsConfig
            {
                Groups = Groups.ToList(),
                Users = Users.ToList()
            };
            await _repository.SaveAsync(config);
            StatusMessage = "Изменения сохранены";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task ImportAllGroups()
    {
        // Предупреждение
        var confirm = System.Windows.MessageBox.Show(
            "⚠️ ВНИМАНИЕ!\n\n" +
            "Импорт ВСЕХ групп из AD добавит много записей в список.\n" +
            "В AD может быть много системных и неиспользуемых групп.\n\n" +
            "Рекомендуется использовать ПОИСК для точечного добавления нужных групп.\n\n" +
            "Продолжить полный импорт?",
            "Подтверждение импорта",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        
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
            
            var config = await _repository.LoadAsync();
            Groups = new ObservableCollection<SecurityPrincipal>(config.Groups);
            
            StatusMessage = $"Импортировано новых групп: {addedCount} из {adGroups.Count}";
            
            System.Windows.MessageBox.Show(
                $"Импорт завершён!\n\n" +
                $"Найдено групп в AD: {adGroups.Count}\n" +
                $"Добавлено новых: {addedCount}\n" +
                $"(дубликаты пропущены)",
                "Импорт групп",
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
    private async Task ImportAllUsers()
    {
        // Предупреждение
        var confirm = System.Windows.MessageBox.Show(
            "⚠️ ВНИМАНИЕ!\n\n" +
            "Импорт ВСЕХ пользователей из AD добавит много записей в список.\n" +
            "В AD может быть много неактивных и системных учётных записей.\n\n" +
            "Рекомендуется использовать ПОИСК для точечного добавления нужных пользователей.\n\n" +
            "Продолжить полный импорт?",
            "Подтверждение импорта",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        
        IsLoading = true;
        StatusMessage = "Импорт пользователей из Active Directory...";
        
        try
        {
            var adUsers = await _repository.ImportUsersFromActiveDirectoryAsync();
            
            if (adUsers.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Не удалось получить пользователей из Active Directory.\n\n" +
                    "Возможные причины:\n" +
                    "• Компьютер не в домене\n" +
                    "• Нет прав на чтение AD\n" +
                    "• Сетевые проблемы",
                    "Импорт не удался",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }
            
            var addedCount = await _repository.MergePrincipalsAsync(adUsers);
            
            var config = await _repository.LoadAsync();
            Users = new ObservableCollection<SecurityPrincipal>(config.Users);
            
            StatusMessage = $"Импортировано новых пользователей: {addedCount} из {adUsers.Count}";
            
            System.Windows.MessageBox.Show(
                $"Импорт завершён!\n\n" +
                $"Найдено пользователей в AD: {adUsers.Count}\n" +
                $"Добавлено новых: {addedCount}\n" +
                $"(дубликаты пропущены)",
                "Импорт пользователей",
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
    private async Task RemoveSelectedGroups(System.Collections.IList? selectedItems)
    {
        if (selectedItems == null || selectedItems.Count == 0) return;
        
        var toRemove = selectedItems.Cast<SecurityPrincipal>().ToList();
        var count = toRemove.Count;
        
        var message = count == 1 
            ? $"Удалить группу '{toRemove[0].FullName}'?"
            : $"Удалить выбранные группы ({count} шт.)?";
        
        var result = System.Windows.MessageBox.Show(
            message,
            "Подтверждение удаления",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
            
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            var config = await _repository.LoadAsync();
            var namesToRemove = toRemove.Select(g => g.FullName).ToHashSet();
            config.Groups.RemoveAll(g => namesToRemove.Contains(g.FullName));
            await _repository.SaveAsync(config);
            
            foreach (var item in toRemove)
            {
                Groups.Remove(item);
            }
            
            StatusMessage = count == 1 ? "Группа удалена" : $"Удалено групп: {count}";
        }
    }
    
    [RelayCommand]
    private async Task RemoveSelectedUsers(System.Collections.IList? selectedItems)
    {
        if (selectedItems == null || selectedItems.Count == 0) return;
        
        var toRemove = selectedItems.Cast<SecurityPrincipal>().ToList();
        var count = toRemove.Count;
        
        var message = count == 1 
            ? $"Удалить пользователя '{toRemove[0].FullName}'?"
            : $"Удалить выбранных пользователей ({count} шт.)?";
        
        var result = System.Windows.MessageBox.Show(
            message,
            "Подтверждение удаления",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
            
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            var config = await _repository.LoadAsync();
            var namesToRemove = toRemove.Select(u => u.FullName).ToHashSet();
            config.Users.RemoveAll(u => namesToRemove.Contains(u.FullName));
            await _repository.SaveAsync(config);
            
            foreach (var item in toRemove)
            {
                Users.Remove(item);
            }
            
            StatusMessage = count == 1 ? "Пользователь удалён" : $"Удалено пользователей: {count}";
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
    
    // ===== Поиск пользователей =====
    
    [RelayCommand]
    private async Task SearchUsers()
    {
        if (string.IsNullOrWhiteSpace(UserSearchQuery) || UserSearchQuery.Length < 2)
        {
            StatusMessage = "Введите минимум 2 символа для поиска";
            return;
        }
        
        IsSearchingUsers = true;
        StatusMessage = $"Поиск пользователей \"{UserSearchQuery}\"...";
        UserSearchResults.Clear();
        
        try
        {
            var results = await _repository.SearchUsersInAdAsync(UserSearchQuery);
            
            if (results.Count == 0)
            {
                StatusMessage = $"По запросу \"{UserSearchQuery}\" пользователей не найдено";
            }
            else
            {
                UserSearchResults = new ObservableCollection<SecurityPrincipal>(results);
                StatusMessage = $"Найдено: {results.Count} пользователей";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка поиска: {ex.Message}";
        }
        finally
        {
            IsSearchingUsers = false;
        }
    }
    
    [RelayCommand]
    private async Task AddUserFromSearch()
    {
        if (SelectedUserSearchResult == null)
        {
            StatusMessage = "Выберите пользователя из результатов поиска";
            return;
        }
        
        var added = await _repository.MergePrincipalsAsync(new List<SecurityPrincipal> { SelectedUserSearchResult });
        
        if (added > 0)
        {
            Users.Add(SelectedUserSearchResult);
            StatusMessage = $"Добавлен: {SelectedUserSearchResult.FullName}";
            UserSearchResults.Remove(SelectedUserSearchResult);
            SelectedUserSearchResult = null;
        }
        else
        {
            StatusMessage = $"'{SelectedUserSearchResult.FullName}' уже есть в списке";
        }
    }
    
    [RelayCommand]
    private async Task AddAllUsersFromSearch()
    {
        if (UserSearchResults.Count == 0)
        {
            StatusMessage = "Нет результатов для добавления";
            return;
        }
        
        var toAdd = UserSearchResults.ToList();
        var added = await _repository.MergePrincipalsAsync(toAdd);
        
        var config = await _repository.LoadAsync();
        Users = new ObservableCollection<SecurityPrincipal>(config.Users);
        
        UserSearchResults.Clear();
        StatusMessage = $"Добавлено пользователей: {added}";
    }
    
    // ===== Поиск групп =====
    
    [RelayCommand]
    private async Task SearchGroups()
    {
        if (string.IsNullOrWhiteSpace(GroupSearchQuery) || GroupSearchQuery.Length < 2)
        {
            StatusMessage = "Введите минимум 2 символа для поиска";
            return;
        }
        
        IsSearchingGroups = true;
        StatusMessage = $"Поиск групп \"{GroupSearchQuery}\"...";
        GroupSearchResults.Clear();
        
        try
        {
            var results = await _repository.SearchGroupsInAdAsync(GroupSearchQuery);
            
            if (results.Count == 0)
            {
                StatusMessage = $"По запросу \"{GroupSearchQuery}\" групп не найдено";
            }
            else
            {
                GroupSearchResults = new ObservableCollection<SecurityPrincipal>(results);
                StatusMessage = $"Найдено: {results.Count} групп";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка поиска: {ex.Message}";
        }
        finally
        {
            IsSearchingGroups = false;
        }
    }
    
    [RelayCommand]
    private async Task AddGroupFromSearch()
    {
        if (SelectedGroupSearchResult == null)
        {
            StatusMessage = "Выберите группу из результатов поиска";
            return;
        }
        
        var added = await _repository.MergePrincipalsAsync(new List<SecurityPrincipal> { SelectedGroupSearchResult });
        
        if (added > 0)
        {
            Groups.Add(SelectedGroupSearchResult);
            StatusMessage = $"Добавлена: {SelectedGroupSearchResult.FullName}";
            GroupSearchResults.Remove(SelectedGroupSearchResult);
            SelectedGroupSearchResult = null;
        }
        else
        {
            StatusMessage = $"'{SelectedGroupSearchResult.FullName}' уже есть в списке";
        }
    }
    
    [RelayCommand]
    private async Task AddAllGroupsFromSearch()
    {
        if (GroupSearchResults.Count == 0)
        {
            StatusMessage = "Нет результатов для добавления";
            return;
        }
        
        var toAdd = GroupSearchResults.ToList();
        var added = await _repository.MergePrincipalsAsync(toAdd);
        
        var config = await _repository.LoadAsync();
        Groups = new ObservableCollection<SecurityPrincipal>(config.Groups);
        
        GroupSearchResults.Clear();
        StatusMessage = $"Добавлено групп: {added}";
    }
}
