import { onBeforeUnmount, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { normalizeQuery, queryText, searchLocation } from '@/library/search'

/** Pause after typing before the search starts, in milliseconds. */
export const searchDelay = 300

/**
 * The search field of the toolbar: typing searches the library after a short pause, Enter at once.
 * The search opens the search page; while it is open, further searches replace its history entry,
 * so the back button leaves the search instead of stepping through every typed letter. The field
 * shows the text of the search page and is empty elsewhere.
 */
export function useSearchField(delay = searchDelay) {
    const route = useRoute()
    const router = useRouter()
    let timer: ReturnType<typeof setTimeout> | undefined

    function isSearchPage(): boolean {
        return route.name === 'library-search'
    }

    function routeText(): string {
        return isSearchPage() ? queryText(route.query) : ''
    }

    const text = ref(routeText())

    function submit(value: string = text.value): void {
        clearTimeout(timer)
        timer = undefined
        const q = normalizeQuery(value)
        const onSearchPage = isSearchPage()
        if ((!q && !onSearchPage) || (onSearchPage && queryText(route.query) === q)) {
            return
        }

        const target = searchLocation(q)
        void (onSearchPage ? router.replace(target) : router.push(target))
    }

    watch(text, (value) => {
        clearTimeout(timer)
        timer = setTimeout(() => submit(value), delay)
    })

    // Navigation (back, a link, leaving the search page) updates the field.
    watch(
        () => routeText(),
        (value) => {
            if (normalizeQuery(text.value) !== value) {
                text.value = value
            }
        },
    )

    onBeforeUnmount(() => clearTimeout(timer))

    return { text, submit }
}
