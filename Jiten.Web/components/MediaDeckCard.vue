<script setup lang="ts">
  import type { Deck } from '~/types';
  import Card from 'primevue/card';
  import { getMediaTypeText } from '../utils/mediaTypeMapper';
  import { getLinkTypeText } from '../utils/linkTypeMapper';

  const props = defineProps<{
    deck: Deck;
  }>();
</script>

<template>
  <Card class="p-2">
    <template #title>{{ deck.originalTitle }}</template>
    <template #subtitle>{{ getMediaTypeText(deck.mediaType) }}</template>
    <template #content>
      <div class="flex-gap-6">
        <div class="flex-1">
          <div class="flex flex-col md:flex-row gap-x-8 gap-y-2">
            <div><img src="/img/nocover.jpg" alt="No cover" class="h-48 w-32" /></div>
            <div>
              <div class="flex flex-col md:flex-row gap-x-8 gap-y-2">
                <div class="w-full md:w-64">
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Character count</span>
                    <span class="ml-8 tabular-nums">{{ deck.characterCount }}</span>
                  </div>
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Word count</span>
                    <span class="ml-8 tabular-nums">{{ deck.wordCount }}</span>
                  </div>
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Unique words</span>
                    <span class="ml-8 tabular-nums">{{ deck.uniqueWordCount }}</span>
                  </div>
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Unique words used once</span>
                    <span class="ml-8 tabular-nums">{{ deck.uniqueWordUsedOnceCount }}</span>
                  </div>
                </div>

                <div class="w-full md:w-64">
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Unique kanji</span>
                    <span class="ml-8 tabular-nums">{{ deck.uniqueKanjiCount }}</span>
                  </div>
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Unique kanji used once</span>
                    <span class="ml-8 tabular-nums">{{ deck.uniqueKanjiUsedOnceCount }}</span>
                  </div>
                  <div v-if="deck.averageSentenceLength !== 0" class="flex justify-between mb-2">
                    <span class="text-gray-600">Average sentence length</span>
                    <span class="ml-8 tabular-nums">{{ deck.averageSentenceLength }}</span>
                  </div>
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Difficulty</span>
                    <span class="ml-8 tabular-nums">{{ deck.difficulty }}</span>
                  </div>
                </div>
              </div>

              <div class="mt-4 flex flex-col md:flex-row gap-4">
                <a v-for="link in deck.links" :href="link.url" target="_blank">{{ getLinkTypeText(link.linkType) }}</a>
              </div>
              <div>
                <Button
                  as="router-link"
                  :to="`/decks/medias/${deck.deckId}/vocabulary`"
                  label="View vocabulary"
                  class="mt-4"
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>

<style scoped></style>
