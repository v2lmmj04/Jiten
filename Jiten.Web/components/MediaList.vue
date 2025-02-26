<script setup lang="ts">
  import { useApiFetchPaginated } from '~/composables/useApiFetch';
  import { type Deck, type Definition, MediaType, type Reading } from '~/types';
  import Skeleton from 'primevue/skeleton';
  import Card from 'primevue/card';
  import InputText from 'primevue/inputtext';

  const props = defineProps<{
    word?: Word;
  }>();

  const route = useRoute();

  const offset = computed(() => (route.query.offset ? Number(route.query.offset) : 0));
  const mediaType = computed(() => (route.query.mediaType ? route.query.mediaType : null));

  const url = computed(() => `media-deck/get-media-decks`);

  const {
    data: response,
    status,
    error,
  } = await useApiFetchPaginated<Deck[]>(url, {
    query: {
      offset: offset,
      mediaType: mediaType,
      wordId: props?.word?.wordId,
      readingIndex: props?.word?.mainReading?.readingIndex,
    },
    watch: [offset, mediaType],
  });

  const currentPage = computed(() => response.value?.currentPage);
  const pageSize = computed(() => response.value?.pageSize);
  const totalItems = computed(() => response.value?.totalItems);

  const start = computed(() => (currentPage.value - 1) * pageSize.value + 1);
  const end = computed(() => Math.min(currentPage.value * pageSize.value, totalItems.value));

  const previousLink = computed(() => {
    return response.value?.hasPreviousPage ? { query: { offset: response.value.previousOffset } } : null;
  });
  const nextLink = computed(() => {
    return response.value?.hasNextPage ? { query: { offset: response.value.nextOffset } } : null;
  });
</script>

<template>
  <div class="flex flex-col gap-2">
    <Card>
      <template #content>
        <div class="flex flex-row flex-wrap justify-around">
          <NuxtLink :to="{ query: { mediaType: MediaType.Anime } }">Anime</NuxtLink>
          <NuxtLink :to="{ query: { mediaType: MediaType.Drama } }">Dramas</NuxtLink>
          <NuxtLink :to="{ query: { mediaType: MediaType.Movie } }">Movies</NuxtLink>
          <NuxtLink :to="{ query: { mediaType: MediaType.Novel } }">Novels</NuxtLink>
          <NuxtLink :to="{ query: { mediaType: MediaType.NonFiction } }">Non-Fiction</NuxtLink>
          <NuxtLink :to="{ query: { mediaType: MediaType.VideoGame } }">Video Games</NuxtLink>
          <NuxtLink :to="{ query: { mediaType: MediaType.VisualNovel } }">Visual Novels</NuxtLink>
          <NuxtLink :to="{ query: { mediaType: MediaType.WebNovel } }">Web Novels</NuxtLink>
        </div>
      </template>
    </Card>
    <div>
      <InputText v-model="nameSearch" type="text" placeholder="Search by name" class="w-full" />
    </div>
    <div>
      <div class="flex flex-col gap-1">
        <div class="flex justify-between">
          <div class="flex gap-8">
            <NuxtLink :to="previousLink" :class="previousLink == null ? 'text-gray-500 pointer-events-none' : ''">
              Previous
            </NuxtLink>
            <NuxtLink :to="nextLink" :class="nextLink == null ? 'text-gray-500 pointer-events-none' : ''">
              Next
            </NuxtLink>
          </div>
          <div>viewing decks {{ start }}-{{ end }} from {{ totalItems }} total</div>
        </div>

        <div v-if="status === 'pending'" class="flex flex-col gap-4">
          <Card v-for="i in 5" :key="i" class="p-2">
            <template #content>
              <Skeleton width="100%" height="250px" />
            </template>
          </Card>
        </div>

        <div v-else-if="error">Error: {{ error }}</div>

        <div v-else class="flex flex-col gap-4">
          <MediaDeckCard v-for="deck in response.data" :key="deck.id" :deck="deck" />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped></style>
