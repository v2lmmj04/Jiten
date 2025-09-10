<script setup lang="ts">
  import VocabularyImport from '~/components/VocabularyImport.vue';
  import AnkiConnectImport from '~/components/AnkiConnectImport.vue';
  definePageMeta({
    middleware: ['auth'],
  });
</script>

<template>
  <div class="container mx-auto p-2 md:p-4">
    <VocabularyImport />
  </div>
</template>

<style scoped></style>
