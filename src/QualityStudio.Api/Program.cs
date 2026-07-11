using System.Diagnostics;
using AgentOrchestrator.CodeQuality;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using QualityStudio.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
builder.Services.Configure<RepositoryOptions>(builder.Configuration.GetSection(RepositoryOptions.SectionName));
builder.Services.AddSingleton<RepositoryAccess>();
builder.Services.AddSingleton<StalenessEvaluator>();
var corsOptions = builder.Configuration.GetSection(RepositoryOptions.SectionName).Get<RepositoryOptions>()
    ?? new RepositoryOptions();
builder.Services.AddCors(options => options.AddPolicy("dev-frontend", policy =>
    policy.WithOrigins(corsOptions.AllowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = exception switch
    {
        ArgumentException => (StatusCodes.Status400BadRequest, "Invalid repository path"),
        FileNotFoundException => (StatusCodes.Status404NotFound, "File not found"),
        DirectoryNotFoundException => (StatusCodes.Status503ServiceUnavailable, "Repository unavailable"),
        StalenessScanException => (StatusCodes.Status422UnprocessableEntity, "Repository scan failed"),
        _ => (StatusCodes.Status500InternalServerError, "Unexpected API error"),
    };
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("QualityStudio.Api.Errors");
    logger.LogError(new EventId(1000, "ApiRequestFailed"), exception, "API request failed with status {StatusCode}", status);
    await Results.Problem(statusCode: status, title: title, detail: exception?.Message).ExecuteAsync(context);
}));
app.UseStatusCodePages();
app.UseCors("dev-frontend");

app.MapGet("/api/tree", (string? path, RepositoryAccess repository, ILogger<Program> logger) =>
{
    var stopwatch = Stopwatch.StartNew();
    var requested = repository.NormalizeRelativePath(path);
    var projects = RepositoryHierarchyBuilder.BuildDotNet(repository.Root);
    ReviewMetaDiscovery.AttachDiscovered(repository.Root, projects);
    IReadOnlyList<HierarchyNode> selected = requested == "."
        ? projects
        : Flatten(projects).Where(node => string.Equals(node.Path, requested, StringComparison.Ordinal)).ToArray();
    if (selected.Count == 0)
    {
        return Results.NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Tree path not found",
            Detail = $"No hierarchy node exists at '{requested}'.",
        });
    }

    logger.LogInformation(new EventId(1100, "TreeLoaded"),
        "Loaded {NodeCount} tree roots for {RepositoryPath} in {ElapsedMilliseconds} ms",
        selected.Count, requested, stopwatch.ElapsedMilliseconds);
    return Results.Ok(new TreeResponse(requested, selected.Select(TreeNodeResponse.From).ToArray()));
});

app.MapGet("/api/file", async (string? path, RepositoryAccess repository, CancellationToken cancellationToken) =>
{
    var relative = repository.NormalizeRelativePath(path);
    var absolute = repository.ResolveFile(relative);
    var content = await File.ReadAllTextAsync(absolute, cancellationToken);
    return Results.Ok(new FileResponse(relative, content, repository.ReadMetaDocuments(relative)));
});

app.MapGet("/api/scan", async (RepositoryAccess repository, StalenessEvaluator evaluator,
    ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    var stopwatch = Stopwatch.StartNew();
    var report = await evaluator.ScanAsync(repository.Root, cancellationToken: cancellationToken);
    logger.LogInformation(new EventId(1200, "ScanCompleted"),
        "Scanned repository with {FileCount} files in {ElapsedMilliseconds} ms",
        report.Files.Count, stopwatch.ElapsedMilliseconds);
    return Results.Ok(report);
});

app.MapPost("/api/review", () => Results.Problem(
    statusCode: StatusCodes.Status501NotImplemented,
    title: "Review runner unavailable",
    detail: "Review triggering requires the optional QS-6 review runner, which is not available in this build."));

app.Run();

static IEnumerable<HierarchyNode> Flatten(IEnumerable<HierarchyNode> roots)
{
    foreach (var root in roots)
    {
        yield return root;
        foreach (var descendant in Flatten(root.Children))
        {
            yield return descendant;
        }
    }
}

public partial class Program;
