import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { provideRouter } from '@angular/router';
import { JournalEntry } from '../../../domain/models/journal-entry';
import { Mood } from '../../../domain/models/mood';
import { EntryCardComponent } from './entry-card.component';
import { MoodPickerComponent } from './mood-picker.component';

@Component({
  imports: [ReactiveFormsModule, MoodPickerComponent],
  template: `<app-mood-picker [formControl]="control" />`,
})
class MoodPickerHostComponent {
  readonly control = new FormControl<Mood | null>('Good');
}

describe('MoodPickerComponent', () => {
  it('reflects the form value and updates it on click', async () => {
    const fixture = TestBed.createComponent(MoodPickerHostComponent);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(element.querySelectorAll<HTMLButtonElement>('button'));

    expect(buttons).toHaveLength(5);
    expect(element.querySelector('[aria-checked="true"]')?.textContent).toContain('Good');

    buttons.find((b) => b.textContent?.includes('Awful'))!.click();
    await fixture.whenStable();

    expect(fixture.componentInstance.control.value).toBe('Awful');
    expect(fixture.componentInstance.control.touched).toBe(true);
  });

  it('clears the mood when the selected option is clicked again', async () => {
    const fixture = TestBed.createComponent(MoodPickerHostComponent);
    await fixture.whenStable();
    const selected = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      '[aria-checked="true"]',
    )!;

    selected.click();
    await fixture.whenStable();

    expect(fixture.componentInstance.control.value).toBeNull();
  });
});

@Component({
  imports: [EntryCardComponent],
  template: `<app-entry-card [entry]="entry()" />`,
})
class EntryCardHostComponent {
  readonly entry = signal<JournalEntry>({
    id: 'abc',
    title: 'A walk in the park',
    content: 'A '.repeat(200),
    mood: 'Great',
    createdAt: new Date('2026-01-15T10:30:00Z'),
    updatedAt: null,
  });
}

describe('EntryCardComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  it('renders title, mood, excerpt and a link to the detail page', async () => {
    const fixture = TestBed.createComponent(EntryCardHostComponent);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('h2')?.textContent).toContain('A walk in the park');
    expect(element.textContent).toContain('Great');
    expect(element.querySelector('a')?.getAttribute('href')).toBe('/entries/abc');
    expect(element.querySelector('p')!.textContent!.trim().endsWith('…')).toBe(true);
  });
});
