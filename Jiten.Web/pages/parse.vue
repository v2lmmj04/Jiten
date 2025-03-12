<script setup lang="ts">
  import { useApiFetch } from '~/composables/useApiFetch';
  import type { DeckWord } from '~/types';
  import WordSearch from '~/components/WordSearch.vue';
  import { select } from '@clack/prompts';

  const route = useRoute();

  const url = computed(() => `vocabulary/parse`);

  const searchContent = ref(route.query.text || '');

  useHead(() => {
    return {
      title: 'Search ' + searchContent.value,
    };
  });

  const {
    data: response,
    status,
    error,
  } = await useApiFetch<DeckWord[]>(url.value, { query: { text: searchContent }, watch: [searchContent] });

  watch(
    () => route.query.text,
    (newText) => {
      if (newText) {
        response.value = [];
        searchContent.value = newText;
        selectedWord.value = response.value?.find((word) => word.wordId != 0);
      }
    }
  );

  const words = computed<DeckWord[]>(() => response.value || []);
  const selectedWord = ref<DeckWord | undefined>();

  if (status.value === 'success') {
    selectedWord.value = response.value?.find((word) => word.wordId != 0);
  }

  watch(
    () => status.value,
    (newStatus) => {
      if (newStatus === 'success') {
        selectedWord.value = response.value?.find((word) => word.wordId != 0);
      }
    }
  );
</script>

<template>
  <div>
    <WordSearch />
    <span v-for="word in words" :key="word.wordId" class="pr-2 font-noto-sans">
      <span
        v-if="word.wordId != 0"
        class="text-purple-600 text-lg underline underline-offset-4 cursor-pointer hover:font-bold"
        @click="selectedWord = word"
      >
        {{ word.originalText }}
      </span>
      <span v-else>{{ word.originalText }}</span>
      <!--   <VocabularyEntry word="word" />-->
    </span>

    <div v-if="selectedWord">
      <Transition name="fade" mode="out-in">
        <VocabularyDetail
          :key="`${selectedWord.wordId}-${selectedWord.readingIndex}`"
          :word-id="selectedWord.wordId"
          :reading-index="selectedWord.readingIndex"
        />
      </Transition>
    </div>
  </div>
</template>

<style scoped>

</style>
