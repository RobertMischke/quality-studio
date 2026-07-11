using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace QualityStudio.Api.Tests;

public sealed class ApiSmokeTests : IAsyncLifetime
{
    private readonly string repositoryRoot = Path.Combine(Path.GetTempPath(), "quality-studio-api-tests", Guid.NewGuid().ToString("N"));
    private TestApplication? application;

    [Fact]
    public async Task Tree_returns_derived_hierarchy_and_kind_states()
    {
        using var client = application!.CreateClient();
        using var response = await client.GetAsync("/api/tree?path=", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var project = Assert.Single(json.RootElement.GetProperty("nodes").EnumerateArray());
        Assert.Equal("project", project.GetProperty("level").GetString());
        Assert.True(project.GetProperty("kinds").TryGetProperty("code", out var code));
        Assert.Equal("missing", code.GetProperty("overall").GetString());
        var module = Assert.Single(project.GetProperty("children").EnumerateArray());
        Assert.Equal("Sample", module.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Scan_returns_staleness_report()
    {
        using var client = application!.CreateClient();
        using var response = await client.GetAsync("/api/scan", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(1, json.GetProperty("missingCount").GetInt32());
        var file = Assert.Single(json.GetProperty("files").EnumerateArray());
        Assert.Equal("Sample.cs", file.GetProperty("relativePath").GetString());
        Assert.Equal("missing", file.GetProperty("state").GetString());
    }

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(repositoryRoot);
        await File.WriteAllTextAsync(Path.Combine(repositoryRoot, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        await File.WriteAllTextAsync(Path.Combine(repositoryRoot, "Sample.cs"), "namespace Sample; public static class Greeter { public static string Hello() => \"hello\"; }");
        await RunGitAsync("init", "--quiet");
        application = new TestApplication(repositoryRoot);
    }

    public async ValueTask DisposeAsync()
    {
        if (application is not null)
        {
            await application.DisposeAsync();
        }

        try
        {
            Directory.Delete(repositoryRoot, true);
        }
        catch (IOException)
        {
        }
    }

    private async Task RunGitAsync(params string[] arguments)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo("git")
            {
                WorkingDirectory = repositoryRoot,
                UseShellExecute = false,
            },
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        await process.WaitForExitAsync();
        Assert.Equal(0, process.ExitCode);
    }

    private sealed class TestApplication(string root) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["QualityStudio:RepositoryRoot"] = root }));
        }
    }
}
