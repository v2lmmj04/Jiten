// https://nuxt.com/docs/api/configuration/nuxt-config

import Lara from '@primevue/themes/lara';
import {definePreset} from "@primeuix/styled";

// Custom theming
const JitenPreset = definePreset(Lara, {});

export default defineNuxtConfig({
    compatibilityDate: '2024-04-03',
    devtools: {enabled: true},
    runtimeConfig: {
        public: {
            baseURL: process.env.BASE_URL || 'https://localhost:7299/api/',
        },
    },
    modules: ['@nuxtjs/tailwindcss', "@primevue/nuxt-module", '@nuxt/icon'],
    primevue: {
        options: {
            theme: {
                preset: JitenPreset,
                options: {
                    darkModeSelector: '.dark-mode',
                }
            }
        }
    },
    css: ['~/assets/css/main.css']
})
