using System.Text.Json;
using Systekna.Application.Domain.Entities;

namespace Systekna.Application.Services;

/// <summary>
/// Interface para persistência de projetos de prototipagem
/// </summary>
public interface IProjectPersistenceService
{
    Task<List<ProjectSummary>> GetUserProjectsAsync(int userId);
    Task<PrototypeProject?> LoadProjectAsync(int userId, string projectId);
    Task<string> SaveProjectAsync(int userId, string? projectId, PrototypeProject project, string? name = null);
    Task<bool> DeleteProjectAsync(int userId, string projectId);
    Task<ProjectSummary?> GetProjectInfoAsync(int userId, string projectId);
    Task<bool> RenameProjectAsync(int userId, string projectId, string newName);
    Task<string?> DuplicateProjectAsync(int userId, string projectId, string? newName = null);
}

/// <summary>
/// Resumo de um projeto salvo
/// </summary>
public class ProjectSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ScreenCount { get; set; }
    public int ElementCount { get; set; }
    public int AssetCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// Serviço de persistência de projetos em arquivos JSON
/// Cada usuário tem uma pasta própria para seus projetos
/// </summary>
public class FileProjectPersistenceService : IProjectPersistenceService
{
    private readonly string _basePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _lockObj = new();

    public FileProjectPersistenceService(string? basePath = null)
    {
        _basePath = basePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Systekna",
            "PixelDisplay240",
            "Projects");
        
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
    }

    public Task<List<ProjectSummary>> GetUserProjectsAsync(int userId)
    {
        var userPath = GetUserPath(userId);
        var projects = new List<ProjectSummary>();

        if (!Directory.Exists(userPath))
        {
            return Task.FromResult(projects);
        }

        foreach (var file in Directory.GetFiles(userPath, "*.json"))
        {
            try
            {
                var metaFile = Path.ChangeExtension(file, ".meta.json");
                if (File.Exists(metaFile))
                {
                    var metaJson = File.ReadAllText(metaFile);
                    var summary = JsonSerializer.Deserialize<ProjectSummary>(metaJson, _jsonOptions);
                    if (summary != null)
                    {
                        projects.Add(summary);
                    }
                }
                else
                {
                    // Cria metadata do arquivo existente
                    var projectJson = File.ReadAllText(file);
                    var project = JsonSerializer.Deserialize<PrototypeProject>(projectJson, _jsonOptions);
                    if (project != null)
                    {
                        var summary = CreateSummary(Path.GetFileNameWithoutExtension(file), project, file);
                        SaveMetadata(metaFile, summary);
                        projects.Add(summary);
                    }
                }
            }
            catch
            {
                // Ignora arquivos corrompidos
            }
        }

        return Task.FromResult(projects.OrderByDescending(p => p.UpdatedAt).ToList());
    }

    public Task<PrototypeProject?> LoadProjectAsync(int userId, string projectId)
    {
        var filePath = GetProjectPath(userId, projectId);
        
        if (!File.Exists(filePath))
        {
            return Task.FromResult<PrototypeProject?>(null);
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var project = JsonSerializer.Deserialize<PrototypeProject>(json, _jsonOptions);
            return Task.FromResult(project);
        }
        catch
        {
            return Task.FromResult<PrototypeProject?>(null);
        }
    }

    public Task<string> SaveProjectAsync(int userId, string? projectId, PrototypeProject project, string? name = null)
    {
        var userPath = GetUserPath(userId);
        EnsureDirectoryExists(userPath);

        // Gera novo ID se não fornecido
        var id = projectId ?? $"proj_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}".Substring(0, 32);
        var filePath = GetProjectPath(userId, id);
        var metaPath = Path.ChangeExtension(filePath, ".meta.json");

        lock (_lockObj)
        {
            // Salva o projeto
            var json = JsonSerializer.Serialize(project, _jsonOptions);
            File.WriteAllText(filePath, json);

            // Atualiza metadata
            var summary = CreateSummary(id, project, filePath);
            if (!string.IsNullOrEmpty(name))
            {
                summary.Name = name;
            }
            
            // Se já existe metadata, mantém a data de criação
            if (File.Exists(metaPath))
            {
                try
                {
                    var existingMeta = JsonSerializer.Deserialize<ProjectSummary>(File.ReadAllText(metaPath), _jsonOptions);
                    if (existingMeta != null)
                    {
                        summary.CreatedAt = existingMeta.CreatedAt;
                    }
                }
                catch { }
            }

            SaveMetadata(metaPath, summary);
        }

        return Task.FromResult(id);
    }

    public Task<bool> DeleteProjectAsync(int userId, string projectId)
    {
        var filePath = GetProjectPath(userId, projectId);
        var metaPath = Path.ChangeExtension(filePath, ".meta.json");

        if (!File.Exists(filePath))
        {
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(filePath);
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<ProjectSummary?> GetProjectInfoAsync(int userId, string projectId)
    {
        var metaPath = Path.ChangeExtension(GetProjectPath(userId, projectId), ".meta.json");
        
        if (!File.Exists(metaPath))
        {
            return Task.FromResult<ProjectSummary?>(null);
        }

        try
        {
            var json = File.ReadAllText(metaPath);
            var summary = JsonSerializer.Deserialize<ProjectSummary>(json, _jsonOptions);
            return Task.FromResult(summary);
        }
        catch
        {
            return Task.FromResult<ProjectSummary?>(null);
        }
    }

    public async Task<bool> RenameProjectAsync(int userId, string projectId, string newName)
    {
        var info = await GetProjectInfoAsync(userId, projectId);
        if (info == null) return false;

        info.Name = newName;
        info.UpdatedAt = DateTime.UtcNow;
        
        var metaPath = Path.ChangeExtension(GetProjectPath(userId, projectId), ".meta.json");
        SaveMetadata(metaPath, info);
        
        return true;
    }

    public async Task<string?> DuplicateProjectAsync(int userId, string projectId, string? newName = null)
    {
        var project = await LoadProjectAsync(userId, projectId);
        if (project == null) return null;

        var originalInfo = await GetProjectInfoAsync(userId, projectId);
        var name = newName ?? $"{originalInfo?.Name ?? "Projeto"} (Cópia)";

        return await SaveProjectAsync(userId, null, project, name);
    }

    private string GetUserPath(int userId)
    {
        return Path.Combine(_basePath, $"user_{userId}");
    }

    private string GetProjectPath(int userId, string projectId)
    {
        return Path.Combine(GetUserPath(userId), $"{projectId}.json");
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private ProjectSummary CreateSummary(string id, PrototypeProject project, string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var elementCount = project.Screens.Sum(s => s.Elements.Count);

        return new ProjectSummary
        {
            Id = id,
            Name = project.Screens.FirstOrDefault()?.Name ?? "Novo Projeto",
            ScreenCount = project.Screens.Count,
            ElementCount = elementCount,
            AssetCount = project.Assets.Count,
            CreatedAt = fileInfo.Exists ? fileInfo.CreationTimeUtc : DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private void SaveMetadata(string metaPath, ProjectSummary summary)
    {
        var json = JsonSerializer.Serialize(summary, _jsonOptions);
        File.WriteAllText(metaPath, json);
    }
}
