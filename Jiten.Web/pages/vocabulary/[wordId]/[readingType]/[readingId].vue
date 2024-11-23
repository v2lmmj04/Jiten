<script setup lang="ts">
import {useApiFetchPaginated} from "~/composables/useApiFetch";
import type {Word} from "~/types";

const route = useRoute();
const id = route.params.wordId;
const readingType = route.params.readingType;
const readingId = route.params.readingId;

const url = computed(() => `media-deck/${id}/vocabulary`);

const {
  data: response,
  status,
  error
} = useApiFetch<Word[]>(url, {query: {offset: offset}, watch: [offset]});

</script>

<template>

</template>

<style scoped>

</style>
