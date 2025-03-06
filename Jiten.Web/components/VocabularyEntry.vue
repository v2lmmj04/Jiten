<script setup lang="ts">
  import type { Word } from '~/types';
  import Card from 'primevue/card';
  import Button from 'primevue/button';
  import VocabularyDefinitions from '~/components/VocabularyDefinitions.vue';
  import {convertToRuby} from "../utils/convertToRuby";

  const props = defineProps<{
    word: Word;
    isCompact: boolean;
  }>();

  const isCompact = ref(props.isCompact);

  const toggleCompact = () => {
    isCompact.value = !isCompact.value;
  };
</script>

<template>
  <Card class="">
    <template #title>
      <router-link class="font-noto-sans font-bold" :to="`/vocabulary/${word.wordId}/${word.mainReading.readingIndex}`"
                   v-html="convertToRuby(word.mainReading.text)">
      </router-link>
      <Button text size="small" @click="toggleCompact">{{ isCompact ? 'Expand' : 'Compact' }}</Button>
    </template>
    <template #subtitle />
    <template #content>
      <VocabularyDefinitions :definitions="word.definitions" :is-compact="isCompact" />
    </template>
  </Card>
</template>

<style scoped></style>
