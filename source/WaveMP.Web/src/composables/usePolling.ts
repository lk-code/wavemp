import { getCurrentScope, onScopeDispose, readonly, ref, type Ref } from 'vue'

export interface Polling {
    /** True while polling is started (also while it pauses for a hidden page). */
    readonly active: Readonly<Ref<boolean>>
    start(): void
    stop(): void
}

/**
 * Calls the callback every interval while started. Polling pauses while the page is hidden and
 * calls the callback at once when the page becomes visible again. A call that is still running is
 * never overlapped. Stops with the component (or effect scope) that uses it.
 */
export function usePolling(callback: () => Promise<void> | void, intervalMs: number): Polling {
    const active = ref(false)
    let timer: ReturnType<typeof setTimeout> | undefined
    let running = false

    function schedule(): void {
        clearTimeout(timer)
        timer = undefined
        if (active.value && !document.hidden) {
            timer = setTimeout(() => void tick(), intervalMs)
        }
    }

    async function tick(): Promise<void> {
        if (running || !active.value) {
            return
        }

        running = true
        try {
            await callback()
        } catch {
            // The callback shows its own errors; polling goes on.
        } finally {
            running = false
            schedule()
        }
    }

    function onVisibilityChange(): void {
        if (!active.value) {
            return
        }

        if (document.hidden) {
            clearTimeout(timer)
            timer = undefined
        } else {
            void tick()
        }
    }

    function start(): void {
        if (active.value) {
            return
        }

        active.value = true
        document.addEventListener('visibilitychange', onVisibilityChange)
        schedule()
    }

    function stop(): void {
        active.value = false
        clearTimeout(timer)
        timer = undefined
        document.removeEventListener('visibilitychange', onVisibilityChange)
    }

    if (getCurrentScope()) {
        onScopeDispose(stop)
    }

    return { active: readonly(active), start, stop }
}
