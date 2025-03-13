<script setup lang="ts">
  import Button from 'primevue/button';

  function toggleDarkMode() {
    document.documentElement.classList.toggle('dark-mode');
  }

  const settings = ref();
  const store = useJitenStore();

  const titleLanguageOptions = ref([
    { label: 'Japanese', value: 0 },
    { label: 'Romaji', value: 1 },
    { label: 'English', value: 2 },
  ]);

  const titleLanguage = ref(store.titleLanguage);

  watch(titleLanguage, (newValue) => {
    store.titleLanguage = newValue;
  });

  const isOverSettings = ref(false);
  const isSettingsInteracted = ref(false);

  const onSettingsMouseEnter = () => {
    isOverSettings.value = true;
  };

  const onSettingsMouseLeave = () => {
    isOverSettings.value = false;
    setTimeout(() => {
      if (!isOverSettings.value && !isSettingsInteracted.value) {
        settings.value.hide();
      }
    }, 750);
  };

  const toggleSettings = (event) => {
    settings.value.toggle(event);
  };

  const showSettings = (event) => {
    settings.value.show(event);
  };
</script>

<template>
  <header>
    <div class="bg-indigo-900">
      <div class="flex justify-between items-center mb-6 mx-auto p-4 max-w-6xl">
        <NuxtLink to="/" class="!no-underline">
          <h1 class="text-2xl font-bold text-white">
            Jiten <span class="text-red-600 text-xs align-super">beta</span>
          </h1>
        </NuxtLink>
        <nav class="space-x-6">
          <nuxt-link to="/" class="!text-white">Home</nuxt-link>
          <nuxt-link to="/decks/medias" class="!text-white">Medias</nuxt-link>
          <nuxt-link to="/other" class="!text-white">Other</nuxt-link>
          <Button
            type="button"
            label="Share"
            severity="secondary"
            @mouseover="showSettings($event)"
            @mouseleave="onSettingsMouseLeave"
            @click="toggleSettings($event)"
          >
            <Icon name="material-symbols-light:settings" />
          </Button>

          <Button label="Toggle Dark Mode" severity="secondary" @click="toggleDarkMode()">
            <Icon name="line-md:light-dark" />
          </Button>
        </nav>
      </div>
    </div>
  </header>

  <Popover ref="settings" @mouseenter="onSettingsMouseEnter" @mouseleave="onSettingsMouseLeave">
    <FloatLabel variant="on" class="">
      <Select
        v-model="titleLanguage"
        :options="titleLanguageOptions"
        option-label="label"
        option-value="value"
        placeholder="Titles Language"
        input-id="titleLanguage"
        @show="isSettingsInteracted = true"
        @hide="isSettingsInteracted = false"
        class=""
      />
      <label for="titleLanguage">Titles Language</label>
    </FloatLabel>
  </Popover>
</template>

<style scoped></style>
