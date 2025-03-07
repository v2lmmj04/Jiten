<script setup lang="ts">
  import type { Word } from '~/types';
  import { formatPercentageApprox } from '~/utils/formatPercentageApprox';
  import { convertToRuby } from '~/utils/convertToRuby';

  const route = useRoute();
  const id = route.params.wordId;
  const readingIndex: number = parseInt(route.params.readingIndex);

  const url = computed(() => `vocabulary/${id}/${readingIndex}`);

  const { data: response, status, error } = await useApiFetch<Word>(url.value);

  useHead(() => {
    return {
      title: response.value?.mainReading.text,
    };
  });

  const getSortedReadings = () => {
    return response.value?.alternativeReadings.sort((a, b) => b.frequencyPercentage - a.frequencyPercentage) || [];
  };

  const sortedReadings = computed(() => getSortedReadings());
</script>

<template>
  <Card class="p-4">
    <template v-if="response" #content>
      <div class="flex flex-col gap-4">
        <div class="flex justify-between">
          <div class="text-3xl font-noto-sans" v-html="convertToRuby(response.mainReading.text)"></div>
          <div class="italic">#{{ response.mainReading.frequencyRank }}</div>
        </div>

        <div>
          <h1 class="text-gray-500 text-sm">Meanings</h1>
          <div class="pl-2">
            <VocabularyDefinitions :definitions="response.definitions" :is-compact="false" />
          </div>
        </div>

        <div>
          <h1 class="text-gray-500 font-noto-sans text-sm">Readings</h1>
          <div class="pl-2 flex flex-row gap-8">
            <span v-for="reading in sortedReadings" :key="reading.readingIndex">
              <span :class="reading.readingIndex === readingIndex ? 'font-bold' : ''">
                <router-link :to="`/vocabulary/${id}/${reading.readingIndex}`">
                  <div class="text-center font-noto-sans">
                    {{ reading.text }}
                    <div class="text-xs">({{ formatPercentageApprox(reading.frequencyPercentage) }})</div>
                  </div>
                </router-link>
              </span>
            </span>
          </div>
        </div>

        <div>
          <Accordion value="0" lazy>
            <AccordionPanel value="1">
              <AccordionHeader>
                <div class="cursor-pointer">
                  Used in <b>{{ response.mainReading.usedInMediaAmount }} medias</b>
                </div>
              </AccordionHeader>
              <AccordionContent>
                <MediaList :word="response" />
              </AccordionContent>
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </template>
  </Card>
</template>

<style scoped></style>
