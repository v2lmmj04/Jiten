import { defineStore } from 'pinia';
import { TitleLanguage } from '~/types';

export const useJitenStore = defineStore('jiten', () => {
  const titleLanguageCookie = useCookie<TitleLanguage>('jiten-title-language', {
    default: () => TitleLanguage.Original,
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

  return { titleLanguage, displayFurigana };
});
