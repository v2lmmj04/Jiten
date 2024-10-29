<script setup lang="ts">
import {useApiFetch} from "~/composables/useApiFetch";
import type {Word} from "~/types";

const route = useRoute();
const id = route.params.id;

const {data: words, pending, error} = await useApiFetch<Word[]>(`MediaDeck/${id}/vocabulary`);

</script>

<template>
  <!-- Display the words with their propeties -->
  <div class="flex flex-col gap-1">
    <VocabularyEntry v-for="word in words" :word="word"/>
  </div>
</template>

<style scoped>

</style>
