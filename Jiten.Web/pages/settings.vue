<script setup lang="ts">
  import Card from 'primevue/card';
  import Button from 'primevue/button';
  import { useToast } from 'primevue/usetoast';

  definePageMeta({
    middleware: ['auth'],
  });

  const toast = useToast();
  const { $api } = useNuxtApp();

  // Optional: interact with local store to send known IDs
  const { useJitenStore } = await import('~/stores/jitenStore');
  const store = useJitenStore();
  const isLoading = ref(false);
  const uploadedCount = ref<number | null>(null);
  const addedCount = ref<number | null>(null);
  const skippedCount = ref<number | null>(null);

  async function handleAnkiFileSelect(event: any) {
    const file = event.files?.[0];
    if (!file) {
      toast.add({ severity: 'error', summary: 'Error', detail: 'No file selected.', life: 5000 });
      return;
    }
    if (file.type !== 'text/plain') {
      toast.add({ severity: 'error', summary: 'Error', detail: 'Please upload a TXT file.', life: 5000 });
      return;
    }
    try {
      isLoading.value = true;
      const formData = new FormData();
      formData.append('file', file);

      const result = await $api<{ parsed: number; added: number; skipped: number }>('user/known/add-from-anki-txt', {
        method: 'POST',
        body: formData,
      });

      if (result) {
        uploadedCount.value = result.parsed;
        addedCount.value = result.added;
        skippedCount.value = result.skipped;
        toast.add({
          severity: 'success',
          summary: 'Known words updated',
          detail: `Parsed ${result.parsed}, added ${result.added}, skipped ${result.skipped}.`,
          life: 6000,
        });
      }
    } catch (e) {
      console.error(e);
      toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to upload file.', life: 5000 });
    } finally {
      isLoading.value = false;
    }
  }

  async function sendLocalKnownIds() {
    try {
      const ids = store.getKnownWordIds();
      if (!ids || ids.length === 0) {
        toast.add({ severity: 'info', summary: 'No data', detail: 'No known word IDs found in local storage.', life: 4000 });
        return;
      }
      isLoading.value = true;

      const bodyPayload = ids || [];

      const result = await $api<{ added: number; skipped: number }>('user/vocabulary/import-from-ids', {
        method: 'POST',
        body: JSON.stringify(bodyPayload),
        headers: {
          'Content-Type': 'application/json',
        },
      });
      if (result) {
        addedCount.value = result.added;
        skippedCount.value = result.skipped;
        toast.add({ severity: 'success', summary: 'Known words saved', detail: `Added ${result.added}, skipped ${result.skipped}.`, life: 6000 });
      }
    } catch (e) {
      console.error(e);
      toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to send known word IDs.', life: 5000 });
    } finally {
      isLoading.value = false;
    }
  }
</script>

<template>
  <div class="container mx-auto p-2 md:p-4">
    <Toast />

    <Card class="mb-4">
      <template #title>User Settings</template>
      <template #subtitle>Manage your known words synced with your Jiten account.</template>
      <template #content>
        <div class="flex flex-col gap-3">
          <div class="flex flex-col gap-2">
            <h3 class="text-lg font-semibold">Upload Anki Deck (TXT)</h3>
            <p class="text-sm text-gray-600 dark:text-gray-400">
              Exports: Notes in Plain Text (.txt), untick all boxes. This will parse the first token per line on the server and add matching words to your
              account.
            </p>
            <FileUpload
              mode="basic"
              name="ankiFile"
              accept=".txt"
              :custom-upload="true"
              :auto="true"
              :choose-label="'Select anki .txt File'"
              class="mb-2 w-full md:w-auto"
              @select="handleAnkiFileSelect"
            />
          </div>

          <div class="flex flex-col gap-2">
            <h3 class="text-lg font-semibold">Sync Local Known IDs to Account</h3>
            <p class="text-sm text-gray-600 dark:text-gray-400">
              Send the known word IDs currently stored in your browser to the server to save them in your account.
            </p>
            <div class="flex items-center gap-2">
              <Button icon="pi pi-upload" label="Send Local Known IDs" class="w-full md:w-auto" :loading="isLoading" @click="sendLocalKnownIds" />
              <span class="text-sm text-gray-600"
                >Local IDs: <strong>{{ store.getKnownWordIds().length }}</strong></span
              >
            </div>
          </div>

          <div v-if="addedCount !== null || skippedCount !== null" class="text-sm text-gray-700 dark:text-gray-300 mt-2">
            <div v-if="uploadedCount !== null">
              Parsed from file: <strong>{{ uploadedCount }}</strong>
            </div>
            <div v-if="addedCount !== null">
              Added: <strong class="text-green-600">{{ addedCount }}</strong>
            </div>
            <div v-if="skippedCount !== null">
              Already present: <strong class="text-amber-600">{{ skippedCount }}</strong>
            </div>
          </div>
        </div>

        <div v-if="isLoading" class="mt-4 flex items-center gap-2 text-sm"><i class="pi pi-spin pi-spinner" /> <span>Working...</span></div>
      </template>
    </Card>
  </div>
</template>

<style scoped></style>
