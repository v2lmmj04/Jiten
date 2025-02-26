import type { MediaType } from '~/types';

export interface Deck {
  deckId: number;
  coverName?: string;
  mediaType: MediaType;
  originalTitle: string;
  romajiTitle?: string;
  englishTitle?: string;
  characterCount: number;
  wordCount: number;
  uniqueWordCount: number;
  uniqueWordUsedOnceCount: number;
  uniqueKanjiCount: number;
  uniqueKanjiUsedOnceCount: number;
  difficulty: number;
  averageSentenceLength: number;
  parentDeckId: number;
  deckWords: DeckWord[];
  links: Link[];
}

export interface DeckVocabularyList {
  deckId: number;
  title: string;
  words: DeckWord[];
}

export interface DeckWord {
  deckId: number;
  wordId: number;
  readingType: string;
  readingIndex: number;
}

export interface Link {
  linkId: number;
  url: string;
  linkType: string;
  deckId: number;
}

export interface Word {
  wordId: number;
  mainReading: Reading;
  alternativeReadings: Reading[];
  partsOfSpeech: string[];
  definitions: Definition[];
}

export interface Reading {
  text: string;
  readingType: ReadingType;
  readingIndex: number;
  frequencyRank: number;
  frequencyPercentage: number;
  usedInMediaAmount: number;
}

export interface Definition {
  index: number;
  meanings: string[];
  partsOfSpeech: string[];
}

export class PaginatedResponse<T> {
  constructor(
    public readonly data: T,
    public readonly totalItems: number,
    public readonly pageSize: number,
    public readonly currentOffset: number
  ) {}

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize);
  }

  get currentPage(): number {
    return Math.floor(this.currentOffset / this.pageSize) + 1;
  }

  get hasPreviousPage(): boolean {
    return this.currentPage > 1;
  }

  get hasNextPage(): boolean {
    return this.currentPage < this.totalPages;
  }

  get previousOffset(): number | null {
    return this.hasPreviousPage ? Math.max(0, this.currentOffset - this.pageSize) : null;
  }

  get nextOffset(): number | null {
    return this.hasNextPage ? Math.min(this.totalItems, this.currentOffset + this.pageSize) : null;
  }
}
