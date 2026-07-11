import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type ReviewState = 'fresh' | 'stale' | 'missing';
export interface KindState { direct: ReviewState; descendants: ReviewState; overall: ReviewState; score: number | null; band: string | null; metaPath: string | null; }
export interface TreeNode { id: string; name: string; level: string; path: string; kinds: Record<string, KindState>; children: TreeNode[]; }
export interface FileDocument { path: string; content: string; metaDocuments: unknown[]; }
export interface ScanReport { files: unknown[]; freshCount: number; staleCount: number; missingCount: number; }

const demoFile = `using System.Diagnostics;
using AgentOrchestrator.CodeQuality;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<RepositoryAccess>();

var app = builder.Build();
app.UseExceptionHandler();

app.MapGet("/api/tree", (RepositoryAccess repository) =>
{
    var stopwatch = Stopwatch.StartNew();
    var projects = RepositoryHierarchyBuilder.BuildDotNet(repository.Root);
    return Results.Ok(projects);
});

app.MapGet("/api/file", async (string path) =>
{
    var content = await File.ReadAllTextAsync(path);
    return Results.Ok(content);
});

app.Run();`;

const kind = (overall: ReviewState): Record<string, KindState> => ({ code: { direct: overall, descendants: overall, overall, score: overall === 'fresh' ? 91 : overall === 'stale' ? 72 : null, band: overall === 'fresh' ? 'A' : overall === 'stale' ? 'C' : null, metaPath: null } });
const demoTree: TreeNode[] = [{ id: 'quality-studio', name: 'Quality Studio', level: 'repository', path: '.', kinds: kind('stale'), children: [
  { id: 'src', name: 'src', level: 'folder', path: 'src', kinds: kind('stale'), children: [
    { id: 'api', name: 'QualityStudio.Api', level: 'project', path: 'src/QualityStudio.Api', kinds: kind('fresh'), children: [
      { id: 'program', name: 'Program.cs', level: 'file', path: 'src/QualityStudio.Api/Program.cs', kinds: kind('fresh'), children: [] },
      { id: 'contracts', name: 'ApiContracts.cs', level: 'file', path: 'src/QualityStudio.Api/ApiContracts.cs', kinds: kind('stale'), children: [] },
      { id: 'settings', name: 'appsettings.json', level: 'file', path: 'src/QualityStudio.Api/appsettings.json', kinds: kind('missing'), children: [] },
    ]},
    { id: 'core', name: 'AgentOrchestrator.CodeQuality', level: 'project', path: 'src/AgentOrchestrator.CodeQuality', kinds: kind('stale'), children: [
      { id: 'runner', name: 'ReviewRunner.cs', level: 'file', path: 'src/AgentOrchestrator.CodeQuality/ReviewRunner.cs', kinds: kind('stale'), children: [] },
      { id: 'state', name: 'ReviewState.cs', level: 'file', path: 'src/AgentOrchestrator.CodeQuality/ReviewState.cs', kinds: kind('fresh'), children: [] },
    ]},
  ]},
  { id: 'tests', name: 'tests', level: 'folder', path: 'tests', kinds: kind('missing'), children: [] },
  { id: 'docs', name: 'docs', level: 'folder', path: 'docs', kinds: kind('fresh'), children: [] },
]}];

@Injectable({ providedIn: 'root' })
export class QualityApi {
  private readonly http = inject(HttpClient);
  readonly tree = signal<TreeNode[]>(demoTree);
  readonly file = signal<FileDocument | null>(null);
  readonly scan = signal<ScanReport>({ files: [], freshCount: 8, staleCount: 4, missingCount: 3 });
  readonly connected = signal(false);
  readonly loading = signal(false);

  async loadTree(): Promise<void> {
    try {
      const [tree, scan] = await Promise.all([
        firstValueFrom(this.http.get<{ nodes: TreeNode[] }>('/api/tree?path=')),
        firstValueFrom(this.http.get<ScanReport>('/api/scan')),
      ]);
      this.tree.set(tree.nodes); this.scan.set(scan); this.connected.set(true);
      console.info(JSON.stringify({ event: 'qs.data.tree-loaded', nodeCount: tree.nodes.length, source: 'api' }));
    } catch (error) {
      console.warn(JSON.stringify({ event: 'qs.data.demo-fallback', reason: error instanceof Error ? error.message : 'API unavailable' }));
    }
  }

  async loadFile(path: string): Promise<void> {
    this.loading.set(true);
    try {
      const file = await firstValueFrom(this.http.get<FileDocument>('/api/file', { params: { path } }));
      this.file.set(file); this.connected.set(true);
    } catch {
      this.file.set({ path, content: demoFile, metaDocuments: [] });
    } finally { this.loading.set(false); }
  }
}
