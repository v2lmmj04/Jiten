<script setup lang="ts">
import type {Word} from "~/types";
import Card from 'primevue/card';
import Button from 'primevue/button';

const props = defineProps<{
  word: Word,
  isCompact: boolean
}>()

const isCompact = ref(props.isCompact);

const toggleCompact = () => {
  isCompact.value = !isCompact.value;
};

const definitionsWithPartsOfSpeech = computed(() => {
  let previousPartOfSpeech = ref<string | null>(null);

  return props.word.definitions.map(definition => {
    const isDifferentPartOfSpeech = JSON.stringify(previousPartOfSpeech.value) !== JSON.stringify(definition.partsOfSpeech);
    previousPartOfSpeech.value = [...definition.partsOfSpeech];
    return {
      ...definition,
      isDifferentPartOfSpeech
    };
  });
});
</script>

<template>
  <Card class="">
    <template #title><span class="font-noto-sans">{{ word.mainReading.text }}</span>
      <Button @click="toggleCompact" text size="small">{{ isCompact ? 'Expand' : 'Compact' }}</Button>

    </template>
    <template #subtitle></template>
    <template #content v-if="!isCompact">
      <ul>
        <li v-for="definition in definitionsWithPartsOfSpeech" :key="definition.index">
          <div v-if="definition.isDifferentPartOfSpeech" class="font-bold">{{
              definition.partsOfSpeech.join(',')
            }}
          </div>
          <span class="text-gray-400">{{ definition.index }}.</span> {{ definition.meanings.join('; ') }}
        </li>
      </ul>
    </template>
    <template #content v-else>
      <span v-for="definition in definitionsWithPartsOfSpeech.slice(0, 10)" :key="definition.index">
        {{ definition.meanings.join('; ') }}
        <span v-if="definition.index !== Math.min(definitionsWithPartsOfSpeech.length, 10)">; </span>
      </span>
    </template>
  </Card>
</template>

<style scoped>

</style>
