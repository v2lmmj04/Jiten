<script setup lang="ts">
import {useApiFetch} from "~/composables/useApiFetch";
import Button from 'primevue/button';

const url = "FrequencyList/GetGlobalFrequencyList"

const downloadFile = async () => {
  try {
    const {data: response, status, error} = await useApiFetch<File>(url);
    if (status.value === 'success' && response.value) {
      const blobUrl = window.URL.createObjectURL(new Blob([response.value], {type: response.value.type}));
      const link = document.createElement('a');
      link.href = blobUrl;
      link.setAttribute('download', 'frequency_list.csv'); // You can set the desired file name here
      document.body.appendChild(link);
      link.click();
      link.remove();
      document.body.removeChild(link);
    } else {
      console.error('Error downloading file:', error.value);
    }
  } catch (err) {
    console.error('Error:', err);
  }
}

</script>

<template>
  Last updated:

  <Button @click="downloadFile">Download Frequency List</Button>


  Global word frequency list goes here
  Kanji frequency list goes here
</template>

<style scoped>

</style>
