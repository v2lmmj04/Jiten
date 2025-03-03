<script setup lang="ts">
  import type { Deck } from '~/types';
  import Card from 'primevue/card';
  import { getMediaTypeText } from '~/utils/mediaTypeMapper';
  import { getLinkTypeText } from '~/utils/linkTypeMapper';

  const props = defineProps<{
    deck: Deck;
  }>();

  const showDownloadDialog = ref(false);
</script>

<template>
  <Card class="p-2">
    <template #title>{{ deck.originalTitle }}</template>
    <template #subtitle>{{ getMediaTypeText(deck.mediaType) }}</template>
    <template #content>
      <div class="flex-gap-6">
        <div class="flex-1">
          <div class="flex flex-col md:flex-row gap-x-8 gap-y-2">
            <div><img :src="deck.coverName ?? '/img/nocover.jpg'" :alt="deck.originalTitle" class="h-48 w-34" /></div>
            <div>
              <div class="flex flex-col md:flex-row gap-x-8 gap-y-2">
                <div class="w-full md:w-64">
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Character count</span>
                    <span class="ml-8 tabular-nums">{{ deck.characterCount.toLocaleString() }}</span>
                  </div>
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Word count</span>
                    <span class="ml-8 tabular-nums">{{ deck.wordCount.toLocaleString() }}</span>
                  </div>
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Unique words</span>
                    <span class="ml-8 tabular-nums">{{ deck.uniqueWordCount.toLocaleString() }}</span>
                  </div>
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Unique words used once</span>
                    <span class="ml-8 tabular-nums">{{ deck.uniqueWordUsedOnceCount.toLocaleString() }}</span>
                  </div>
                </div>

                <div class="w-full md:w-64">
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Unique kanji</span>
                    <span class="ml-8 tabular-nums">{{ deck.uniqueKanjiCount.toLocaleString() }}</span>
                  </div>
                  <div class="flex justify-between mb-2">
                    <span class="text-gray-600">Unique kanji used once</span>
                    <span class="ml-8 tabular-nums">{{ deck.uniqueKanjiUsedOnceCount.toLocaleString() }}</span>
                  </div>
                  <div v-if="deck.averageSentenceLength !== 0" class="flex justify-between mb-2">
                    <span class="text-gray-600">Average sentence length</span>
                    <span class="ml-8 tabular-nums">{{ deck.averageSentenceLength.toFixed(1) }}</span>
                  </div>
                  <div class="flex justify-between mb-2">
                    <span
                      class="text-gray-600"
                      v-tooltip="
                        'This is a work in progress.\nThe current analysis only takes into account the vocabulary and not the grammar patterns, which might make some works easier or harder than the score they\'re given.'
                      "
                    >
                      Difficulty
                      <span class="text-purple-500 text-xs align-super"> beta </span>
                    </span>
                    <span class="ml-8 tabular-nums">{{ deck.difficulty }}</span>
                  </div>
                </div>
              </div>

              <div class="mt-4 flex flex-col md:flex-row gap-4">
                <a v-for="link in deck.links" :href="link.url" target="_blank">{{ getLinkTypeText(link.linkType) }}</a>
              </div>
              <div class="mt-4">
                <div class="flex flex-wrap gap-4">
                  <Button
                    as="router-link"
                    :to="`/decks/medias/${deck.deckId}/vocabulary`"
                    label="View vocabulary"
                    class=""
                  />

                  <Button @click="showDownloadDialog = true" label="Download deck" class="" />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </Card>

  <MediaDeckDownloadDialog :deck="deck" :visible="showDownloadDialog" @update:visible="showDownloadDialog = $event" />
</template>

<style scoped></style>
