<script setup lang="ts">
import Button from 'primevue/button';

function toggleDarkMode() {
  document.documentElement.classList.toggle('dark-mode');
}
</script>

<template>
  <header>
    <div class="bg-indigo-900">
    <div class="flex justify-between items-center mb-6 mx-auto p-4 max-w-6xl">
      <h1 class="text-2xl font-bold text-white">Jiten <span class="text-red-600 text-xs">beta</span></h1>
      <nav class="space-x-6">
        <nuxt-link to="/" class="text-white">Home</nuxt-link>
        <nuxt-link to="/decks/medias" class="text-white">Medias</nuxt-link>
        <Button label="Toggle Dark Mode" icon="github" @click="toggleDarkMode()" severity="secondary" >
          <Icon name="line-md:light-dark"/>
        </Button>
      </nav>
    </div>
    </div>
  </header>
</template>

<style scoped>

</style>
