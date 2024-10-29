<script setup lang="ts">
import {useApiFetch} from "~/composables/useApiFetch";
import type {Deck} from "~/types";

const {data: medias, pending, error} = await useApiFetch<Deck[]>("MediaDeck/GetAll");
</script>

<template>
  <div>
    <h1>Medias</h1>
    <div v-if="pending">Loading...</div>
    <div v-else-if="error">Error: {{ error }}</div>
    <div v-else>
      Found {{ medias.length }} medias
      <div class="flex flex-col gap-4">
        <MediaDeckCard v-for="deck in medias" :deck="deck" :key="deck.id"/>
      </div>
    </div>
  </div>
</template>

<style scoped>

</style>
