import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { formatBytes, formatDateTime } from '../format';
import { languageForPath } from '../language';
import { FindingSeverity, QualityApi, ReviewFinding, ReviewKind } from '../quality-api';
import { FlatNode } from '../tree-utils';

const LINE_ENDING_LABELS: Record<string, string> = { lf: 'LF', crlf: 'CRLF', mixed: 'Mixed' };
const ENCODING_LABELS: Record<string, string> = { 'utf-8': 'UTF-8', 'utf-8-bom': 'UTF-8 BOM', other: 'Unknown encoding' };

@Component({
  selector: 'qs-editor',
  templateUrl: './editor.html',
  styleUrl: './editor.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Editor {
  readonly api = inject(QualityApi);
  readonly selectedPath = input.required<string>();
  readonly activeKind = input.required<ReviewKind>();
  readonly selectedNode = input<FlatNode | undefined>();
  readonly viewportHeight = input.required<number>();
  readonly kindSelect = output<ReviewKind>();
  readonly findingSelect = output<ReviewFinding>();

  readonly lineHeight = 22;
  readonly codeScrollTop = signal(0);
  readonly codeLines = computed(() => this.api.file()?.content.split(/\r?\n/) ?? []);
  readonly activeMeta = computed(() => this.api.file()?.metaDocuments.find(meta => meta.kind === this.activeKind()) ?? null);
  readonly availableMeta = computed(() => this.api.file()?.metaDocuments ?? []);
  readonly activeState = computed(() => this.selectedNode()?.kinds[this.activeKind()]?.direct ?? 'missing');
  readonly findingsByLine = computed(() => {
    const map = new Map<number, ReviewFinding[]>();
    const path = this.api.file()?.path;
    for (const finding of this.activeMeta()?.findings ?? []) for (const location of finding.locations) {
      if (location.path !== path || !location.range) continue;
      for (let line = location.range.start.line; line <= location.range.end.line; line++) map.set(line, [...(map.get(line) ?? []), finding]);
    }
    return map;
  });
  readonly visibleLines = computed(() => {
    const start = Math.max(0, Math.floor(this.codeScrollTop() / this.lineHeight) - 10);
    const count = Math.ceil(this.viewportHeight() / this.lineHeight) + 25;
    const markers = this.findingsByLine();
    return this.codeLines().slice(start, start + count).map((text, i) => ({ text, number: start + i + 1, top: (start + i) * this.lineHeight, findings: markers.get(start + i + 1) ?? [] }));
  });
  readonly topVisibleLine = computed(() => Math.floor(this.codeScrollTop() / this.lineHeight) + 1);
  readonly pathParts = computed(() => {
    const path = this.api.file()?.path ?? '';
    const slash = path.lastIndexOf('/');
    return slash === -1 ? { directory: '', name: path } : { directory: path.slice(0, slash + 1), name: path.slice(slash + 1) };
  });
  readonly language = computed(() => languageForPath(this.api.file()?.path));
  readonly fileSizeLabel = computed(() => formatBytes(this.api.file()?.sizeBytes ?? 0));
  readonly lineEndingLabel = computed(() => LINE_ENDING_LABELS[this.api.file()?.lineEnding ?? 'lf']);
  readonly encodingLabel = computed(() => ENCODING_LABELS[this.api.file()?.encoding ?? 'utf-8']);

  constructor() {
    effect(() => { this.selectedPath(); this.codeScrollTop.set(0); });
  }

  selectKind(kind: ReviewKind): void { this.kindSelect.emit(kind); }

  findingTitle(findings: ReviewFinding[]): string { return findings.map(finding => `${finding.severity.toUpperCase()}: ${finding.title}`).join('\n'); }

  severity(findings: ReviewFinding[]): FindingSeverity { return findings[0]?.severity ?? 'info'; }

  reviewed(value: string): string { return formatDateTime(value); }
}
