using AGK.ProjectGen.Application.Interfaces;
using AGK.ProjectGen.Domain.Entities;
using AGK.ProjectGen.Domain.Schema;
using AGK.ProjectGen.Domain.Enums;
using AGK.ProjectGen.Domain.AccessControl;

namespace AGK.ProjectGen.Application.Services;

public class ProjectUpdater : IProjectUpdater
{
    private readonly IFileSystemService _fileSystem;
    private readonly IAclService _aclService;
    private readonly IAclFormulaEngine _aclFormulaEngine;

    public ProjectUpdater(IFileSystemService fileSystem, IAclService aclService, IAclFormulaEngine aclFormulaEngine)
    {
        _fileSystem = fileSystem;
        _aclService = aclService;
        _aclFormulaEngine = aclFormulaEngine;
    }

    public List<GeneratedNode> CreateDiffPlan(GeneratedNode newStructure, string rootPath, ProfileSchema profile)
    {
        var plan = new List<GeneratedNode>();
        
        // Traverse the new structure and check against filesystem
        // Now respects IsIncluded property
        CheckNodeRecursive(newStructure, plan);
        
        return plan;
    }

    private void CheckNodeRecursive(GeneratedNode node, List<GeneratedNode> plan)
    {
        bool exists = _fileSystem.DirectoryExists(node.FullPath);
        node.Exists = exists;

        // Если узел уже помечен как Delete (из RefreshNodeStatus), не меняем операцию
        if (node.Operation == NodeOperation.Delete)
        {
            plan.Add(node);
            foreach (var child in node.Children)
            {
                CheckNodeRecursive(child, plan);
            }
            return;
        }

        // Проверяем IsIncluded для определения операции
        if (!node.IsIncluded)
        {
            // Если папка исключена и существует — помечаем на удаление
            if (exists)
            {
                node.Operation = NodeOperation.Delete;
            }
            else
            {
                // Папка исключена и не существует — игнорируем (ничего не делаем)
                node.Operation = NodeOperation.None;
            }
        }
        else
        {
            // Папка включена
            if (!exists)
            {
                node.Operation = NodeOperation.Create;
            }
            else
            {
                node.Operation = NodeOperation.None;
            }
        }

        plan.Add(node);

        foreach (var child in node.Children)
        {
            CheckNodeRecursive(child, plan);
        }
    }
    
    /// <summary>
    /// Собирает информацию о папках, помеченных на удаление, которые содержат файлы.
    /// </summary>
    public List<DeleteFolderInfo> GetFoldersWithFilesToDelete(List<GeneratedNode> diffPlan)
    {
        var result = new List<DeleteFolderInfo>();
        
        var nodesToDelete = diffPlan
            .Where(n => n.Operation == NodeOperation.Delete && n.Exists)
            .ToList();
        
        // Находим корневые удаляемые узлы (те, чьи родители не удаляются)
        var deletePaths = nodesToDelete.Select(n => n.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        foreach (var node in nodesToDelete)
        {
            // Проверяем, не является ли родитель также удаляемым
            var parentPath = Path.GetDirectoryName(node.FullPath);
            if (parentPath != null && deletePaths.Contains(parentPath))
            {
                // Пропускаем дочерние элементы — файлы будут показаны у родителя
                continue;
            }
            
            if (_fileSystem.DirectoryContainsFiles(node.FullPath))
            {
                var files = _fileSystem.GetAllFilesInDirectory(node.FullPath);
                result.Add(new DeleteFolderInfo
                {
                    FolderPath = node.FullPath,
                    FolderName = node.Name,
                    Files = files
                });
            }
        }
        
        return result;
    }

    public async Task ExecutePlanAsync(List<GeneratedNode> diffPlan, Project project, ProfileSchema profile)
    {
        // 1. Сначала выполняем удаления (от детей к родителям - по убыванию длины пути)
        var deleteNodes = diffPlan
            .Where(n => n.Operation == NodeOperation.Delete && n.Exists)
            .OrderByDescending(n => n.FullPath.Length)
            .ToList();
        
        foreach (var node in deleteNodes)
        {
            if (_fileSystem.DirectoryExists(node.FullPath))
            {
                _fileSystem.DeleteDirectory(node.FullPath);
            }
        }
        
        // 2. Затем создаём новые папки (от родителей к детям - по возрастанию длины пути)
        var createNodes = diffPlan
            .Where(n => n.Operation == NodeOperation.Create && n.IsIncluded)
            .OrderBy(n => n.FullPath.Length)
            .ToList();

        foreach (var node in createNodes)
        {
            _fileSystem.CreateDirectory(node.FullPath);
            ApplyAcl(node, profile);
        }
        
        // 3. Обновляем ACL для существующих папок при необходимости
        var updateAclNodes = diffPlan
            .Where(n => n.Operation == NodeOperation.UpdateAcl)
            .ToList();
        
        foreach (var node in updateAclNodes)
        {
            ApplyAcl(node, profile);
        }

        await Task.CompletedTask;
    }

    private void ApplyAcl(GeneratedNode node, ProfileSchema profile)
    {
        // 1. Получаем правила через AclFormulaEngine (учитывает формулы, bindings и overrides)
        var rules = _aclFormulaEngine.CalculateAclRules(node, null, profile);
        
        // 2. Если есть правила, применяем их
        if (rules.Count > 0)
        {
            // По умолчанию отключаем наследование (только создатель-админ)
            var inheritanceMode = InheritanceMode.BreakClear;
            
            _aclService.SetDirectoryAcl(
                node.FullPath, 
                rules, 
                inheritanceMode, 
                AclApplyMode.Normalize
            );
        }
        else
        {
            // Если правил нет — отключаем наследование, оставляем только создателя
            _aclService.SetDirectoryAcl(
                node.FullPath,
                new List<AclRule>(),
                InheritanceMode.BreakClear,
                AclApplyMode.Normalize
            );
        }
        
        // Сохраняем рассчитанные правила для предпросмотра
        node.PlannedAcl = rules;
    }

}
