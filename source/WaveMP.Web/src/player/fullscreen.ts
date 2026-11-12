// Full screen for the video of the Now Playing stage. Browsers with the Fullscreen API show the
// whole video stage (with its buttons) in full screen; Safari on the iPhone only knows the full
// screen of the video element itself, with its own controls.

/** The full screen of a video element in Safari on the iPhone. */
interface WebkitVideo {
    webkitEnterFullscreen?(): void
    webkitExitFullscreen?(): void
    readonly webkitDisplayingFullscreen?: boolean
}

function webkit(video: HTMLVideoElement): WebkitVideo {
    return video as unknown as WebkitVideo
}

/** Whether the stage or at least the video can be shown in full screen. */
export function canFullscreen(container: HTMLElement, video: HTMLVideoElement): boolean {
    return (
        (container.ownerDocument.fullscreenEnabled === true &&
            typeof container.requestFullscreen === 'function') ||
        typeof webkit(video).webkitEnterFullscreen === 'function'
    )
}

/** Whether the stage or its video is shown in full screen. */
export function isFullscreen(container: HTMLElement, video: HTMLVideoElement): boolean {
    return (
        container.ownerDocument.fullscreenElement === container ||
        webkit(video).webkitDisplayingFullscreen === true
    )
}

/** Shows the stage in full screen, or the video where only that is possible. */
export async function enterFullscreen(
    container: HTMLElement,
    video: HTMLVideoElement,
): Promise<void> {
    if (
        container.ownerDocument.fullscreenEnabled === true &&
        typeof container.requestFullscreen === 'function'
    ) {
        // Refused without a user gesture or by the browser: the stage stays as it is.
        await container.requestFullscreen().catch(() => undefined)
        return
    }

    webkit(video).webkitEnterFullscreen?.()
}

/** Leaves the full screen of the stage or the video. */
export async function exitFullscreen(
    container: HTMLElement,
    video: HTMLVideoElement,
): Promise<void> {
    const document = container.ownerDocument
    if (document.fullscreenElement === container) {
        await document.exitFullscreen().catch(() => undefined)
        return
    }

    if (webkit(video).webkitDisplayingFullscreen === true) {
        webkit(video).webkitExitFullscreen?.()
    }
}
