using System.Diagnostics;
using AgentOrchestrator.CodeQuality;

return QualityCommand.Run(args, Console.Out, Console.Error);

internal static class QualityCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h")
        {
            output.WriteLine("Usage: quality scan [path] [--by-level]");
            return 0;
        }

        if (!StringComparer.OrdinalIgnoreCase.Equals(args[0], "scan"))
        {
            error.WriteLine($"Unknown command '{args[0]}'.");
            return 2;
        }

        var byLevel = args.Contains("--by-level", StringComparer.OrdinalIgnoreCase);
        var pathArgument = args.Skip(1).FirstOrDefault(value => !value.StartsWith('-')) ?? ".";
        var root = Path.GetFullPath(pathArgument);
        if (!Directory.Exists(root))
        {
            error.WriteLine($"Repository path does not exist: {root}");
            return 2;
        }

        var timer = Stopwatch.StartNew();
        try
        {
            var projects = RepositoryHierarchyBuilder.BuildDotNet(root);
            ReviewMetaDiscovery.AttachDiscovered(root, projects);
            if (byLevel)
            {
                WriteByLevel(projects, output);
            }
            else
            {
                var documents = Flatten(projects).Sum(node => node.Documents.Count);
                output.WriteLine($"Scanned {projects.Count} project(s); found {documents} review document(s). Use --by-level for module summaries.");
            }

            timer.Stop();
            var moduleCount = projects.Sum(project => project.Children.Count);
            error.WriteLine($"event=quality.scan.completed projects={projects.Count} modules={moduleCount} elapsedMs={timer.ElapsedMilliseconds}");
            return 0;
        }
        catch (Exception exception)
        {
            timer.Stop();
            error.WriteLine($"event=quality.scan.failed elapsedMs={timer.ElapsedMilliseconds} error={exception.Message}");
            return 1;
        }
    }

    private static void WriteByLevel(IEnumerable<HierarchyNode> projects, TextWriter output)
    {
        foreach (var project in projects)
        {
            output.WriteLine($"project {project.Name}");
            foreach (var module in project.Children)
            {
                var states = HierarchyAggregation.ForAllKinds(module);
                output.WriteLine(
                    $"  module {module.Name} " +
                    string.Join(' ', Enum.GetValues<ReviewKind>().Select(kind =>
                        $"{kind.ToString().ToLowerInvariant()}={Format(states[kind].Overall)}")));
            }
        }
    }

    private static string Format(ReviewState state) => state switch
    {
        ReviewState.NotReviewed => "not-reviewed",
        ReviewState.Current => "current",
        ReviewState.Stale => "stale",
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };

    private static IEnumerable<HierarchyNode> Flatten(IEnumerable<HierarchyNode> roots)
    {
        foreach (var root in roots)
        {
            yield return root;
            foreach (var child in Flatten(root.Children))
            {
                yield return child;
            }
        }
    }
}
