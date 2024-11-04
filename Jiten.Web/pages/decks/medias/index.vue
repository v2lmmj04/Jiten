<script setup lang="ts">
import {useApiFetchPaginated} from "~/composables/useApiFetch";
import type {Deck} from "~/types";
import Skeleton from 'primevue/skeleton';
import Card from 'primevue/card';

const route = useRoute();

const offset = computed(() => route.query.offset ? Number(route.query.offset) : 0);
const mediaType = computed(() => route.query.mediaType ? route.query.mediaType : null);

const url = computed(() => `MediaDeck/GetMediaDecks`);

const {
  data: response,
  status,
  error
} = useApiFetchPaginated<Deck[]>(url, {
  query: {offset: offset, mediaType: mediaType},
  watch: [offset, mediaType]
});

const currentPage = computed(() => response.value?.currentPage);
const pageSize = computed(() => response.value?.pageSize);
const totalItems = computed(() => response.value?.totalItems);

const start = computed(() => (currentPage.value - 1) * pageSize.value + 1);
const end = computed(() => Math.min(currentPage.value * pageSize.value, totalItems.value));

const previousLink = computed(() => {
  return response.value?.hasPreviousPage ? {query: {offset: response.value.previousOffset}} : null;
});
const nextLink = computed(() => {
  return response.value?.hasNextPage ? {query: {offset: response.value.nextOffset}} : null;
});
</script>

<template>
  <div>
    <h1>Medias</h1>

    <div>
      <div class="flex flex-col gap-1">
        <div class="flex justify-between ">
          <div class="flex gap-8">
            <NuxtLink :to=previousLink :class="previousLink == null ? 'text-gray-500 pointer-events-none' : ''">
              Previous
            </NuxtLink>
            <NuxtLink :to="nextLink" :class="nextLink == null ? 'text-gray-500 pointer-events-none' : ''">
              Next
            </NuxtLink>
          </div>
          <div>
            viewing decks {{ start }}-{{ end }} from {{ totalItems }} total
          </div>
        </div>

        <div v-if="status === 'pending'" class="flex flex-col gap-4">
          <Card class="p-2" v-for="i in 5" :key="i">
            <template #content>
              <Skeleton width="100%" height="250px"></Skeleton>
            </template>
          </Card>
        </div>

        <div v-else-if="error">Error: {{ error }}</div>

        <div v-else class="flex flex-col gap-4">
          <MediaDeckCard v-for="deck in response.data" :deck="deck" :key="deck.id"/>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>

</style>
