using AgentOrchestrator.CodeQuality;

namespace AgentOrchestrator.CodeQuality.Tests;

public sealed class RepositoryHierarchyBuilderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"quality-studio-{Guid.NewGuid():N}");

    [Fact]
    public void DerivesAllFiveLevelsFromDotNetSources()
    {
        Directory.CreateDirectory(Path.Combine(root, "src", "Demo"));
        File.WriteAllText(Path.Combine(root, "Demo.slnx"),
            "<Solution><Project Path=\"src/Demo/Demo.csproj\" /></Solution>");
        File.WriteAllText(Path.Combine(root, "src", "Demo", "Demo.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(Path.Combine(root, "src", "Demo", "Greeter.cs"),
            "namespace Demo.Greetings; public sealed class Greeter\n{\n    public string SayHello() => \"hello\";\n}");

        var project = Assert.Single(RepositoryHierarchyBuilder.BuildDotNet(root));
        var module = Assert.Single(project.Children);
        var ns = Assert.Single(module.Children);
        var file = Assert.Single(ns.Children);
        var function = Assert.Single(file.Children);

        Assert.Equal(ReviewLevel.Project, project.Level);
        Assert.Equal("Demo.Greetings", ns.Name);
        Assert.Equal("Greeter.cs", file.Name);
        Assert.Equal("SayHello", function.Name);
        Assert.StartsWith("qs-v1/dotnet/function/", function.Id, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
