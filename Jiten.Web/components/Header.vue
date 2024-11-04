<script setup lang="ts">
import Button from 'primevue/button';

function toggleDarkMode() {
  document.documentElement.classList.toggle('dark-mode');
}
</script>

<template>
  <header>
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">Jiten <span class="text-red-600 text-xs">beta</span></h1>
      <nav class="space-x-6">
        <nuxt-link to="/">Home</nuxt-link>
        <nuxt-link to="/decks/medias">Medias</nuxt-link>
        <Button label="Toggle Dark Mode" icon="github" @click="toggleDarkMode()" severity="secondary" outlined>
          <Icon name="line-md:light-dark"/>
        </Button>
      </nav>
    </div>
  </header>
</template>

<style scoped>

</style>
