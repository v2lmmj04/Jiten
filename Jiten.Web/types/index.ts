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

export interface MediaType {
    mediaTypeId: number;
    name: string;
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
