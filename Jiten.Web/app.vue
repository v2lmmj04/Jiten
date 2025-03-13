<script setup lang="ts">
  useSeoMeta({
    title: 'Jiten',
    description: 'Get vocabulary lists for all your Japanese medias.',
  });

  useHead({
    titleTemplate: (titleChunk) => {
      return titleChunk ? `${titleChunk} - Jiten` : 'Jiten';
    },
  });

  onMounted(() => {
    // Trick from https://github.com/primefaces/primevue/issues/5899#issuecomment-2585781190
    // TODO remove after primevue fix
    document.documentElement.classList.add('loaded');
  });
</script>

<template>
  <div id="app">
    <NuxtLoadingIndicator />
    <AppHeader />
    <div class="container mx-auto pl-4 pr-4 max-w-6xl">
      <NuxtPage />
    </div>
  </div>
</template>
