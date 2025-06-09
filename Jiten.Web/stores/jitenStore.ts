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

  return { titleLanguage, displayFurigana, darkMode, displayAdminFunctions, readingSpeed };
});
