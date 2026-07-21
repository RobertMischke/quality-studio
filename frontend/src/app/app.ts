import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { Editor } from './editor/editor';
import { Explorer } from './explorer/explorer';
import { QualityApi, ReviewFinding, ReviewKind } from './quality-api';
import { ReviewPanel } from './review-panel/review-panel';
import { flattenTree } from './tree-utils';

@Component({
  selector: 'app-root',
  imports: [Explorer, Editor, ReviewPanel],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(window:resize)': 'onResize()' },
})
export class App {
  readonly api = inject(QualityApi);
  readonly embedded = signal(this.detectEmbedded());
  readonly theme = signal<'dark' | 'light'>((new URLSearchParams(location.search).get('theme') as 'dark' | 'light') || (localStorage.getItem('qs-theme') as 'dark' | 'light') || 'dark');
  readonly selected = signal(new URLSearchParams(location.search).get('path') || 'src/QualityStudio.Api/Program.cs');
  readonly activeKind = signal<ReviewKind>((new URLSearchParams(location.search).get('kind') as ReviewKind) || 'code');
  readonly selectedFinding = signal<ReviewFinding | null>(null);
  readonly viewportHeight = signal(typeof window === 'undefined' ? 1000 : window.innerHeight);
  readonly selectedNode = computed(() => flattenTree(this.api.tree(), new Set(), true).find(n => n.path === this.selected()));

  constructor() {
    effect(() => document.documentElement.dataset['theme'] = this.theme());
    // Deep-linkable position: mirror the selected path and review kind into the
    // URL, and report every navigation to an embedding Studio preview so its
    // address bar stays current (url-preview-embed contract).
    effect(() => {
      const params = new URLSearchParams(location.search);
      params.set('path', this.selected());
      params.set('kind', this.activeKind());
      history.replaceState(null, '', `?${params}`);
      if (this.embedded()) {
        window.parent.postMessage({ source: 'url-preview-embed', type: 'navigation', url: location.href }, '*');
      }
    });
    this.open(this.selected(), false);
  }

  open(path: string, track = true): void {
    const start = performance.now();
    this.selected.set(path);
    this.api.loadFile(path).then(() => {
      const kinds = this.api.file()?.metaDocuments.map(meta => meta.kind) ?? [];
      if (!kinds.includes(this.activeKind())) this.activeKind.set(kinds[0] ?? 'code');
      this.selectedFinding.set(null);
      if (track) requestAnimationFrame(() => this.measure('qs.file.first-content', start, 150));
    });
  }

  selectKind(kind: ReviewKind): void {
    const start = performance.now();
    this.activeKind.set(kind);
    this.selectedFinding.set(null);
    requestAnimationFrame(() => this.measure('qs.review.aspect-switch', start, 50));
  }

  selectFinding(finding: ReviewFinding): void { this.selectedFinding.set(finding); }

  onResize(): void { this.viewportHeight.set(window.innerHeight); }

  setTheme(): void {
    const next = this.theme() === 'dark' ? 'light' : 'dark';
    this.theme.set(next);
    localStorage.setItem('qs-theme', next);
  }

  private measure(name: string, start: number, budget: number): void {
    const duration = performance.now() - start;
    performance.measure(name, { start, end: performance.now(), detail: { budget, path: this.selected() } });
    console.info(JSON.stringify({ event: name, durationMs: +duration.toFixed(2), budgetMs: budget, withinBudget: duration < budget }));
  }

  private detectEmbedded(): boolean {
    if (typeof window === 'undefined' || typeof document === 'undefined') return false;
    try {
      return window.self !== window.top;
    } catch {
      return true;
    }
  }
}
