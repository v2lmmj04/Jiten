// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2024-04-03',
  devtools: { enabled: true },
  runtimeConfig: {
    public: {
      baseURL: process.env.BASE_URL || 'https://localhost:7299/api/',
    },
  },
  modules: ['@nuxtjs/tailwindcss'],
})
