using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace ArxLang.Packages;

// ============================================================
// PackageManager — "arx install", аналог npm install, але без реєстру:
// пакет — це будь-який публічний GitHub-репозиторій з main.arx у корені.
//
// Маніфест проєкту — arx.json поруч із головним файлом:
//   { "dependencies": { "somepkg": "owner/repo" } }
//
// Пакети лягають у arx_modules/<name>/ (той самий вигляд файлів, що в
// репозиторії пакета). import "somepkg" (без .arx) резолвиться в
// arx_modules/somepkg/main.arx — див. ModuleResolver.
// ============================================================
public static class PackageManager
{
    private const string ManifestName = "arx.json";
    private const string ModulesDir = "arx_modules";

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient();
        // GitHub API вимагає User-Agent для будь-яких запитів, інакше 403.
        c.DefaultRequestHeaders.UserAgent.ParseAdd("ArxLang-PackageManager");
        return c;
    }

    public sealed class Manifest
    {
        public string Name { get; set; } = "";
        public Dictionary<string, string> Dependencies { get; set; } = new();
    }

    private static string ManifestPath(string projectDir) => Path.Combine(projectDir, ManifestName);

    private static Manifest LoadManifest(string projectDir)
    {
        var path = ManifestPath(projectDir);
        if (!File.Exists(path)) return new Manifest();

        var json = File.ReadAllText(path);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<Manifest>(json, opts) ?? new Manifest();
    }

    private static void SaveManifest(string projectDir, Manifest manifest)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ManifestPath(projectDir), JsonSerializer.Serialize(manifest, opts));
    }

    // "arx install" без аргументів: ставить усе з arx.json.
    public static void InstallAll(string projectDir)
    {
        var manifest = LoadManifest(projectDir);
        if (manifest.Dependencies.Count == 0)
        {
            Console.WriteLine($"{ManifestName} не знайдено або в ньому немає залежностей — нічого встановлювати.");
            return;
        }

        foreach (var (name, source) in manifest.Dependencies)
            InstallOne(name, source, projectDir).GetAwaiter().GetResult();

        Console.WriteLine($"Готово: {manifest.Dependencies.Count} пакет(ів).");
    }

    // "arx install owner/repo" — тягне конкретний пакет і дописує його в
    // arx.json, щоб наступний "arx install" без аргументів підхопив його теж.
    public static void InstallSingle(string source, string projectDir)
    {
        var name = PackageNameFrom(source);
        InstallOne(name, source, projectDir).GetAwaiter().GetResult();

        var manifest = LoadManifest(projectDir);
        manifest.Dependencies[name] = source;
        SaveManifest(projectDir, manifest);

        Console.WriteLine($"Встановлено '{name}' ({source}), додано в {ManifestName}.");
    }

    private static string PackageNameFrom(string source)
    {
        // "owner/repo" -> "repo"; "owner/repo@branch" -> "repo"
        var withoutRef = source.Split('@')[0];
        var slash = withoutRef.LastIndexOf('/');
        return slash >= 0 ? withoutRef[(slash + 1)..] : withoutRef;
    }

    private static async Task InstallOne(string name, string source, string projectDir)
    {
        var (ownerRepo, explicitRef) = SplitRef(source);
        var parts = ownerRepo.Split('/');
        if (parts.Length != 2)
            throw new Exception($"Неправильний формат залежності '{source}' — очікується owner/repo або owner/repo@ref");

        var owner = parts[0];
        var repo = parts[1];

        Console.WriteLine($"Завантаження {owner}/{repo}...");
        var ghRef = explicitRef ?? await GetDefaultBranch(owner, repo);

        var zipBytes = await Http.GetByteArrayAsync(
            $"https://github.com/{owner}/{repo}/archive/refs/heads/{ghRef}.zip");

        var targetDir = Path.Combine(projectDir, ModulesDir, name);
        if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
        Directory.CreateDirectory(targetDir);

        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        // Архів GitHub завжди має один кореневий каталог виду "repo-branch/" —
        // прибираємо його, щоб файли пакета лягли прямо в arx_modules/<name>/.
        string? rootPrefix = null;
        foreach (var entry in archive.Entries)
        {
            if (rootPrefix == null)
            {
                var firstSlash = entry.FullName.IndexOf('/');
                if (firstSlash > 0) rootPrefix = entry.FullName[..(firstSlash + 1)];
            }

            if (string.IsNullOrEmpty(entry.Name)) continue; // це запис-папка, не файл

            var relative = rootPrefix != null && entry.FullName.StartsWith(rootPrefix)
                ? entry.FullName[rootPrefix.Length..]
                : entry.FullName;

            var destPath = Path.Combine(targetDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            entry.ExtractToFile(destPath, overwrite: true);
        }

        var entryFile = Path.Combine(targetDir, "main.arx");
        if (!File.Exists(entryFile))
            Console.WriteLine($"  Увага: у '{name}' немає main.arx у корені — import \"{name}\" не спрацює, доки він не з'явиться.");

        Console.WriteLine($"  {name} -> {targetDir}");
    }

    private static (string OwnerRepo, string? Ref) SplitRef(string source)
    {
        var at = source.IndexOf('@');
        return at < 0 ? (source, null) : (source[..at], source[(at + 1)..]);
    }

    private static async Task<string> GetDefaultBranch(string owner, string repo)
    {
        var json = await Http.GetStringAsync($"https://api.github.com/repos/{owner}/{repo}");
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("default_branch").GetString() ?? "main";
    }

    // Використовується ModuleResolver'ом для пошуку arx_modules/<name>/main.arx,
    // піднімаючись від файлу, що імпортує, до кореня диска — так само, як
    // Node.js шукає node_modules.
    public static string? FindPackageEntry(string startDir, string packageName)
    {
        var dir = startDir;
        while (true)
        {
            var candidate = Path.Combine(dir, ModulesDir, packageName, "main.arx");
            if (File.Exists(candidate)) return candidate;

            var parent = Directory.GetParent(dir);
            if (parent == null) return null;
            dir = parent.FullName;
        }
    }
}
