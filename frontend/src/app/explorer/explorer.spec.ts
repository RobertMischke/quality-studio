import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Explorer } from './explorer';

describe('Explorer container activation', () => {
  let fixture: ComponentFixture<Explorer>;
  let component: Explorer;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Explorer],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(Explorer);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('selectedPath', 'src/QualityStudio.Api/Program.cs');
    fixture.componentRef.setInput('activeKind', 'code');
    fixture.componentRef.setInput('viewportHeight', 600);
    fixture.detectChanges();
  });

  it('selects and toggles a container when its row is clicked', () => {
    const opened: string[] = [];
    component.nodeOpen.subscribe(path => opened.push(path));
    const row = fixture.nativeElement.querySelector('[data-node-id="quality-studio"]') as HTMLButtonElement;

    expect(component.expanded().has('quality-studio')).toBeTrue();
    row.click();

    expect(component.expanded().has('quality-studio')).toBeFalse();
    expect(opened).toEqual(['.']);
  });

  it('only toggles a container when its chevron is clicked', () => {
    const opened: string[] = [];
    component.nodeOpen.subscribe(path => opened.push(path));
    const chevron = fixture.nativeElement.querySelector('[data-node-id="quality-studio"] .chevron') as HTMLElement;

    expect(component.expanded().has('quality-studio')).toBeTrue();
    chevron.click();

    expect(component.expanded().has('quality-studio')).toBeFalse();
    expect(opened).toEqual([]);
  });
});
