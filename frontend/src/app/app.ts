import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { QualityApi, ReviewState, TreeNode } from './quality-api';

type FlatNode = TreeNode & { depth: number; state: ReviewState };

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  readonly api = inject(QualityApi);
  readonly theme = signal<'dark' | 'light'>((new URLSearchParams(location.search).get('theme') as 'dark' | 'light') || (localStorage.getItem('qs-theme') as 'dark' | 'light') || 'dark');
  readonly expanded = signal(new Set<string>(['quality-studio', 'src', 'api']));
  readonly selected = signal('src/QualityStudio.Api/Program.cs');
  readonly query = signal('');
  readonly scrollTop = signal(0);
  readonly codeScrollTop = signal(0);
  readonly lineHeight = 22;
  readonly treeRows = computed(() => this.flatten(this.api.tree()));
  readonly filteredRows = computed(() => {
    const q = this.query().trim().toLowerCase();
    return q ? this.flatten(this.api.tree(), true).filter(n => n.name.toLowerCase().includes(q) || n.path.toLowerCase().includes(q)) : this.treeRows();
  });
  readonly visibleRows = computed(() => {
    const start = Math.max(0, Math.floor(this.scrollTop() / 30) - 5);
    return this.filteredRows().slice(start, start + 40).map((node, i) => ({ node, top: (start + i) * 30 }));
  });
  readonly codeLines = computed(() => this.api.file()?.content.split(/\r?\n/) ?? []);
  readonly visibleLines = computed(() => {
    const start = Math.max(0, Math.floor(this.codeScrollTop() / this.lineHeight) - 10);
    return this.codeLines().slice(start, start + 80).map((text, i) => ({ text, number: start + i + 1, top: (start + i) * this.lineHeight }));
  });
  readonly selectedNode = computed(() => this.flatten(this.api.tree(), true).find(n => n.path === this.selected()));

  constructor() {
    effect(() => document.documentElement.dataset['theme'] = this.theme());
    this.api.loadTree();
    this.open(this.selected(), false);
  }

  toggle(node: FlatNode): void {
    const start = performance.now();
    this.expanded.update(current => {
      const next = new Set(current);
      next.has(node.id) ? next.delete(node.id) : next.add(node.id);
      return next;
    });
    requestAnimationFrame(() => this.measure('qs.tree.toggle', start, 50));
  }

  open(path: string, track = true): void {
    const start = performance.now();
    this.selected.set(path);
    this.codeScrollTop.set(0);
    this.api.loadFile(path).then(() => {
      if (track) requestAnimationFrame(() => this.measure('qs.file.first-content', start, 150));
    });
  }

  setTheme(): void {
    const next = this.theme() === 'dark' ? 'light' : 'dark';
    this.theme.set(next);
    localStorage.setItem('qs-theme', next);
  }

  private flatten(nodes: TreeNode[], all = false, depth = 0): FlatNode[] {
    const result: FlatNode[] = [];
    for (const node of nodes) {
      const state = (node.kinds['code']?.overall ?? Object.values(node.kinds)[0]?.overall ?? 'missing') as ReviewState;
      result.push({ ...node, depth, state });
      if ((all || this.expanded().has(node.id)) && node.children.length) result.push(...this.flatten(node.children, all, depth + 1));
    }
    return result;
  }

  private measure(name: string, start: number, budget: number): void {
    const duration = performance.now() - start;
    performance.measure(name, { start, end: performance.now(), detail: { budget, path: this.selected() } });
    console.info(JSON.stringify({ event: name, durationMs: +duration.toFixed(2), budgetMs: budget, withinBudget: duration < budget }));
  }
}
