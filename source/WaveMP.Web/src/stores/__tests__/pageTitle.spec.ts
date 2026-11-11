import { beforeEach, describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { usePageTitleStore } from '../pageTitle'

describe('page title store', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('holds the title of a detail page until it is cleared', () => {
        const store = usePageTitleStore()

        store.set('First Album')
        const set = store.title
        store.clear()

        expect(set).toBe('First Album')
        expect(store.title).toBeNull()
    })
})
