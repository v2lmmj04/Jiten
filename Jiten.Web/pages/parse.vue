<script setup lang="ts">
  import { useApiFetch } from '~/composables/useApiFetch';
  import type { DeckWord } from '~/types';
  import WordSearch from '~/components/WordSearch.vue';

  const route = useRoute();

  const url = computed(() => `vocabulary/parse`);

  const {
    data: response,
    status,
    error,
  } = await useApiFetch<DeckWord[]>(url.value, { query: { text: route.query.text } });

  const words = computed(() => response.value);
  console.log(words.value);
</script>

<template>
  <WordSearch />
  <div v-for="word in words" :key="word.wordId">
    {{ word.originalText }}
   <VocabularyEntry word="word" />
  </div>
</template>

<style scoped></style>
