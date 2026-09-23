export interface EntryDto {
  id: string;
  title: string;
  content: string;
  mood: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface SaveEntryRequestDto {
  title: string;
  content: string;
  mood: string | null;
}
