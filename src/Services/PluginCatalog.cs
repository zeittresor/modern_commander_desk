using System.Text.Json;

namespace ModernCommanderDesk.Services;

public sealed class PluginManifest
{
    public string Id { get; set; } = "unknown";
    public string Name { get; set; } = "Unknown plugin";
    public string Version { get; set; } = "0.0";
    public string Type { get; set; } = "helper";
    public string Entry { get; set; } = "";
    public string Description { get; set; } = "";
}

public static class PluginCatalog
{
    public static IReadOnlyList<PluginManifest> LoadManifests()
    {
        AppPaths.EnsureApplicationFolders();
        var manifests = new List<PluginManifest>();
        foreach (var file in Directory.GetFiles(AppPaths.PluginsDirectory, "plugin.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(file);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (manifest is not null)
                {
                    manifests.Add(manifest);
                }
            }
            catch
            {
                // Ignore broken plugin manifests in this early architecture build.
            }
        }

        return manifests.OrderBy(x => x.Name).ToList();
    }

    public static string BuildPluginReport()
    {
        var manifests = LoadManifests();
        if (manifests.Count == 0)
        {
            return "No plugin manifests found. Put plugins into the plugins/ folder. Each plugin folder may contain a plugin.json manifest.";
        }

        return string.Join(Environment.NewLine + Environment.NewLine, manifests.Select(m =>
            $"{m.Name} ({m.Id})\nVersion: {m.Version}\nType: {m.Type}\nEntry: {m.Entry}\n{m.Description}"));
    }
}
