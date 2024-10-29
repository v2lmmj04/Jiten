<script setup lang="ts">
import type {Word} from "~/types";
import Card from 'primevue/card';

const props = defineProps<{
  word: Word
}>()

const previousPartOfSpeech = ref<string | null>(null);

const definitionsWithPartsOfSpeech = computed(() => {
  return props.word.definitions.map(definition => {
    const isDifferentPartOfSpeech = previousPartOfSpeech.value !== definition.partsOfSpeech[0];
    previousPartOfSpeech.value = definition.partsOfSpeech[0];
    return {
      ...definition,
      isDifferentPartOfSpeech
    };
  });
});
</script>

<template>
  <Card class="">
    <template #title>{{ word.reading }}</template>
    <template #subtitle></template>
    <template #content>
      <ul>
        <li v-for="definition in definitionsWithPartsOfSpeech" :key="definition.index">
          <div v-if="definition.isDifferentPartOfSpeech" class="font-bold">{{ definition.partsOfSpeech[0] }}</div>
          <span class="text-gray-400">{{definition.index}}.</span> {{ definition.meanings.join('; ') }}
        </li>
      </ul>
    </template>
  </Card>
</template>

<style scoped>

</style>
