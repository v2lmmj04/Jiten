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

  return { titleLanguage };
});
