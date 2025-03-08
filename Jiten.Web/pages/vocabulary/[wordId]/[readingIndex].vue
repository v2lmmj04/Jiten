<script setup lang="ts">
  import { MediaType, type Word } from '~/types';
  import { formatPercentageApprox } from '~/utils/formatPercentageApprox';
  import { convertToRuby } from '~/utils/convertToRuby';
  import { getMediaTypeText } from '~/utils/mediaTypeMapper';

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

  const mediaAmountUrl = 'media-deck/decks-count';
  const { data: mediaAmountResponse } = await useApiFetch<Record<MediaType, number>>(mediaAmountUrl);
</script>

<template>
  <Card class="p-4">
    <template v-if="response" #content>
      <div class="flex flex-col justify-between md:flex-row">
        <div class="flex flex-col gap-4">
          <div class="flex justify-between">
            <div class="text-3xl font-noto-sans" v-html="convertToRuby(response.mainReading.text)"></div>
            <div class="text-gray-500 text-right md:hidden">Rank #{{ response.mainReading.frequencyRank }}</div>
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
        </div>
        <div>
          <div class="text-gray-500 text-right hidden md:block">Rank #{{ response.mainReading.frequencyRank }}</div>
          <div class="text-right md:pt-4">
            Appears in <b>{{ response.mainReading.usedInMediaAmount }} medias</b>
          </div>
          <table>
            <thead>
              <tr>
                <th></th>
                <th class="text-gray-500 text-sm pl-4">Amount</th>
                <th class="text-gray-500 text-sm pl-4">% of total</th>
              </tr>
            </thead>
            <tr v-for="(amount, mediaType) in response.mainReading.usedInMediaAmountByType" :key="mediaType">
              <th class="text-right p-0.5 !font-bold">{{ getMediaTypeText(Number(mediaType)) }}</th>
              <th class="text-right p-0.5">{{ amount }}</th>
              <th class="text-right p-0.5">{{ (amount / mediaAmountResponse[Number(mediaType)] * 100).toFixed(0) }}%</th>
            </tr>
          </table>
        </div>
      </div>
      <Accordion value="0" lazy>
        <AccordionPanel value="1">
          <AccordionHeader>
            <div class="cursor-pointer">
              View the <b>{{ response.mainReading.usedInMediaAmount }}</b> medias it appears in
            </div>
          </AccordionHeader>
          <AccordionContent>
            <MediaList :word="response" />
          </AccordionContent>
        </AccordionPanel>
      </Accordion>
    </template>
  </Card>
</template>

<style scoped>
  th {
    font-weight: normal;
  }
</style>
