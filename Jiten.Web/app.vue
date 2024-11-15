<script setup lang="ts">
useSeoMeta({
  title: 'Jiten',
  description: 'Get vocabulary lists for all your Japanese medias.'
})
</script>

<template>
  <div id="app">
    <NuxtLoadingIndicator/>
    <Header/>
    <div class="container mx-auto p-4 max-w-6xl">
      <NuxtPage/>
    </div>
  </div>
</template>
