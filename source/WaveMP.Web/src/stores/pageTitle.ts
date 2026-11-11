import { defineStore } from 'pinia'
import { ref } from 'vue'

/**
 * The title of a detail page (the name of an album, artist, genre, year or track). The app shell
 * shows it as page title and the breadcrumb as its last entry; without it they show the name of
 * the section.
 */
export const usePageTitleStore = defineStore('pageTitle', () => {
    const title = ref<string | null>(null)

    function set(value: string | null): void {
        title.value = value
    }

    function clear(): void {
        title.value = null
    }

    return { title, set, clear }
})
