<script setup lang="ts">
import {useApiFetch} from "~/composables/useApiFetch";

const { data: medias, pending, error } = await useApiFetch("MediaDeck/GetAll");
</script>

<template>
  <div>
    <h1>Medias</h1>
    <div v-if="pending">Loading...</div>
    <div v-else-if="error">Error: {{ error }}</div>
    <div v-else>
      <ul>
        {{ medias.length }} medias
        {{medias[0]}}
        <li v-for="media in medias" :key="media.id">
          {{ media.originalTitle }}
        </li>
      </ul>
    </div>
  </div>
</template>

<style scoped>

</style>
