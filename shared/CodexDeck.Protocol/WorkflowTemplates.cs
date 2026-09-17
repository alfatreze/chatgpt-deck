using System.Text.Json;

namespace CodexDeck.Protocol;

public sealed record WorkflowTemplate(string Id, string Label, string Action, bool Enabled = true);

public static class WorkflowTemplateDefaults
{
    public static readonly IReadOnlyList<WorkflowTemplate> All = new[]
    {
        new WorkflowTemplate("review", "Review", "launch_review"),
        new WorkflowTemplate("debug", "Debug", "launch_debug"),
        new WorkflowTemplate("refactor", "Refactor", "launch_refactor")
    };
}

public static class WorkflowTemplateValidator
{
    public static bool IsValid(WorkflowTemplate template) =>
        !string.IsNullOrWhiteSpace(template.Id) && !string.IsNullOrWhiteSpace(template.Label)
        && template.Label.Length <= 40 && template.Id.All(char.IsLetterOrDigit)
        && template.Action.StartsWith("launch_", StringComparison.Ordinal);
}

public sealed class WorkflowTemplateStore
{
    private sealed record Document(int SchemaVersion, List<WorkflowTemplate> Templates);

    public IReadOnlyList<WorkflowTemplate> Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return WorkflowTemplateDefaults.All;
            var document = JsonSerializer.Deserialize<Document>(File.ReadAllText(path), ProtocolJson.Options);
            return document is { SchemaVersion: 1 } && document.Templates.Count > 0 && document.Templates.All(WorkflowTemplateValidator.IsValid)
                ? document.Templates : WorkflowTemplateDefaults.All;
        }
        catch { return WorkflowTemplateDefaults.All; }
    }

    public void Save(string path, IEnumerable<WorkflowTemplate> templates)
    {
        var values = templates.ToList();
        if (values.Count == 0 || values.Any(t => !WorkflowTemplateValidator.IsValid(t))) throw new ArgumentException("Invalid template set.", nameof(templates));
        var directory = Path.GetDirectoryName(path); if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (File.Exists(path)) File.Copy(path, path + ".bak", true);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new Document(1, values), ProtocolJson.Options));
        File.Move(temporary, path, true);
    }
}
