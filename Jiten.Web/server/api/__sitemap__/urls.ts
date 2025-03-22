// server/api/__sitemap__/urls.ts
import { defineEventHandler } from 'h3'
import type { SitemapUrl } from '@nuxtjs/sitemap/dist/runtime/types'

export default defineEventHandler(async (event) => {
    const config = useRuntimeConfig()

    try {
        const decks = await $fetch<{ id: number }[]>(`${config.public.baseURL}media-deck/get-media-decks-id`)

        return decks.map((p): SitemapUrl => ({
            loc: `/decks/medias/${p}/detail`,
            _sitemap: 'pages'
        }))
    } catch (error) {
        console.error('Error fetching sitemap data:', error)
        return []
    }
})
