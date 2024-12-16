<script setup lang="ts">
  import type { DeckVocabularyList } from '~/types';

  const route = useRoute();
  const id = route.params.id;

  const offset = computed(() => (route.query.offset ? Number(route.query.offset) : 0));
  const url = computed(() => `media-deck/${id}/vocabulary`);

  const {
    data: response,
    status,
    error,
  } = await useApiFetchPaginated<DeckVocabularyList>(url.value, { query: { offset: offset }, watch: [offset] });

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

  useHead(() => {
    return {
      title: `${response.value?.data.title} - Vocabulary`,
    };
  });
</script>

<template>
  <div class="flex flex-col gap-1">
    <div class="flex justify-between">
      <div class="flex gap-8">
        <NuxtLink :to="previousLink" :class="previousLink == null ? 'text-gray-500 pointer-events-none' : ''">
          Previous
        </NuxtLink>
        <NuxtLink :to="nextLink" :class="nextLink == null ? 'text-gray-500 pointer-events-none' : ''"> Next</NuxtLink>
      </div>
      <div>viewing words {{ start }}-{{ end }} from {{ totalItems }} total</div>
    </div>
    <div v-if="status === 'pending'" class="flex flex-col gap-4">
      <Card v-for="i in 10" :key="i" class="p-2">
        <template #content>
          <Skeleton width="100%" height="50px" />
        </template>
      </Card>
    </div>
    <div v-else-if="error">Error: {{ error }}</div>
    <div v-else>
      <VocabularyEntry v-for="word in response.data.words" :key="word.wordId" :word="word" :is-compact="true" />
    </div>
  </div>
</template>

<style scoped></style>
