export type Mood = 'Great' | 'Good' | 'Neutral' | 'Bad' | 'Awful';

export interface MoodOption {
  readonly value: Mood;
  readonly label: string;
  readonly emoji: string;
}

export const MOOD_OPTIONS: readonly MoodOption[] = [
  { value: 'Great', label: 'Great', emoji: '😄' },
  { value: 'Good', label: 'Good', emoji: '🙂' },
  { value: 'Neutral', label: 'Neutral', emoji: '😐' },
  { value: 'Bad', label: 'Bad', emoji: '🙁' },
  { value: 'Awful', label: 'Awful', emoji: '😣' },
];

export function isMood(value: unknown): value is Mood {
  return MOOD_OPTIONS.some((option) => option.value === value);
}

export function moodOption(mood: Mood): MoodOption {
  return MOOD_OPTIONS.find((option) => option.value === mood) ?? MOOD_OPTIONS[2];
}
