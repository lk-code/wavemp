import { getCurrentScope, onScopeDispose, readonly, ref, type Ref } from 'vue'

/** Reactive state of a CSS media query; false where matchMedia is not available. */
export function useMediaQuery(query: string): Readonly<Ref<boolean>> {
    const matches = ref(false)

    if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
        const list = window.matchMedia(query)
        const update = () => {
            matches.value = list.matches
        }
        update()
        list.addEventListener('change', update)
        if (getCurrentScope()) {
            onScopeDispose(() => list.removeEventListener('change', update))
        }
    }

    return readonly(matches)
}
