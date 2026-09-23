import { Pipe, PipeTransform } from '@angular/core';

/** Collapses whitespace and truncates text at a word boundary, adding an ellipsis. */
@Pipe({ name: 'excerpt' })
export class ExcerptPipe implements PipeTransform {
  transform(value: string | null | undefined, maxLength = 160): string {
    const text = (value ?? '').replace(/\s+/g, ' ').trim();
    if (text.length <= maxLength) {
      return text;
    }
    const cut = text.slice(0, maxLength);
    const lastSpace = cut.lastIndexOf(' ');
    return `${(lastSpace > maxLength * 0.6 ? cut.slice(0, lastSpace) : cut).trimEnd()}…`;
  }
}
