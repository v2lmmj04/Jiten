export interface Deck {
    id: number;
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

export enum MediaType {
    Anime = 1,
    Drama = 2,
    Movie = 3,
    Novel = 4,
    NonFiction = 5,
    VideoGame = 6,
    VisualNovel = 7,
    WebNovel = 8
}

export enum LinkType
{
    Web = 1,
    Vndb = 2,
    Tmdb = 3,
    Anilist = 4,
    Mal = 5, // Myanimelist
    GoogleBooks = 6,
    Imdb = 7
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
    reading: string;
    alternativeReadings: string[];
    partsOfSpeech: string[];
    definitions: Definition[];
}

export interface Definition {
    index: number;
    meanings: string[];
    partsOfSpeech: string[]
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

