import { defineStore } from 'pinia';
import { TitleLanguage } from '~/types';

export const useJitenStore = defineStore('jiten', () => {
  const titleLanguageCookie = useCookie<TitleLanguage>('jiten-title-language', {
    default: () => TitleLanguage.Romaji,
    watch: true,
    maxAge: 60 * 60 * 24 * 365, // 1 year
    path: '/',
  });

  const titleLanguage = ref<TitleLanguage>(titleLanguageCookie.value);

  watch(titleLanguage, (newValue) => {
    titleLanguageCookie.value = newValue;
  });

  const displayFuriganaCookie = useCookie<boolean>('jiten-display-furigana', {
    default: () => true,
    watch: true,
    maxAge: 60 * 60 * 24 * 365, // 1 year
    path: '/',
  });

  const displayFurigana = ref<boolean>(displayFuriganaCookie.value);

  watch(displayFurigana, (newValue) => {
    displayFuriganaCookie.value = newValue;
  });

  const darkModeCookie = useCookie<boolean>('jiten-dark-mode', {
    default: () => false,
    watch: true,
    maxAge: 60 * 60 * 24 * 365, // 1 year
    path: '/',
  });

  const darkMode = ref<boolean>(darkModeCookie.value);

  watch(darkMode, (newValue) => {
    darkModeCookie.value = newValue;
  });

  const displayAdminFunctionsCookie = useCookie<boolean>('jiten-display-admin-functions', {
    default: () => false,
    watch: true,
    maxAge: 60 * 60 * 24 * 365, // 1 year
    path: '/',
  });

  const displayAdminFunctions = ref<boolean>(displayAdminFunctionsCookie.value);

  watch(displayAdminFunctions, (newValue) => {
    displayAdminFunctionsCookie.value = newValue;
  });

  const readingSpeedCookie = useCookie<number>('jiten-reading-speed', {
    default: () => 14000,
    watch: true,
    maxAge: 60 * 60 * 24 * 365, // 1 year
    path: '/',
  });

  const readingSpeed = ref<number>(readingSpeedCookie.value);

  watch(readingSpeed, (newValue) => {
    readingSpeedCookie.value = newValue;
  });

  const displayAllNsfwCookie = useCookie<boolean>('jiten-display-all-nsfw', {
    default: () => false,
    watch: true,
    maxAge: 60 * 60 * 24 * 365, // 1 year
    path: '/',
  });

  const displayAllNsfw = ref<boolean>(displayAllNsfwCookie.value);

  watch(displayAllNsfw, (newValue) => {
    displayAllNsfwCookie.value = newValue;
  });

  const hideVocabularyDefinitionsCookie = useCookie<boolean>('jiten-hide-vocabulary-definitions', {
    default: () => false,
    watch: true,
    maxAge: 60 * 60 * 24 * 365, // 1 year
    path: '/',
  });

  const hideVocabularyDefinitions = ref<boolean>(hideVocabularyDefinitionsCookie.value);

  watch(hideVocabularyDefinitions, (newValue) => {
    hideVocabularyDefinitionsCookie.value = newValue;
  });

  const getKnownWordIds = (): number[] => {
    if (import.meta.client) {
      try {
        const stored = localStorage.getItem('jiten-known-word-ids');
        return stored ? JSON.parse(stored) : [];
      } catch (error) {
        console.error('Error reading known word IDs from localStorage:', error);
        return [];
      }
    }
    return [];
  };

  // const setKnownWordIds = (wordIds: number[]) => {
  //   if (import.meta.client) {
  //     try {
  //       localStorage.setItem('jiten-known-word-ids', JSON.stringify(wordIds));
  //     } catch (error) {
  //       console.error('Error saving known word IDs to localStorage:', error);
  //     }
  //   }
  // };

  const knownWordIds = ref<number[]>([]);
  let isInitialized = false;

  const ensureInitialized = () => {
    if (!isInitialized && import.meta.client) {
      knownWordIds.value = getKnownWordIds();
      isInitialized = true;
    }
  };

  onMounted(() => {
    ensureInitialized();
  });

  // watch(
  //   knownWordIds,
  //   (newValue) => {
  //     if (isInitialized) {
  //       setKnownWordIds(newValue);
  //     }
  //   },
  //   { deep: true }
  // );

  // function addKnownWordIds(wordIds: number[]) {
  //   const uniqueWordIds = [...new Set([...knownWordIds.value, ...wordIds])];
  //   knownWordIds.value = uniqueWordIds;
  //   console.log(`Added ${wordIds.length} word IDs. Total: ${uniqueWordIds.length}`);
  // }
  //
  // function removeKnownWordId(wordId: number) {
  //   knownWordIds.value = knownWordIds.value.filter((id) => id !== wordId);
  // }
  //
  // function isWordKnown(wordId: number): boolean {
  //   ensureInitialized();
  //   return knownWordIds.value.includes(wordId);
  // }

  return {
    // actions
    getKnownWordIds,
    // addKnownWordIds,
    // removeKnownWordId,
    // setKnownWordIds,
    // isWordKnown,

    // state
    titleLanguage,
    displayFurigana,
    darkMode,
    displayAdminFunctions,
    readingSpeed,
    knownWordIds,
    displayAllNsfw,
    hideVocabularyDefinitions
  };
});
