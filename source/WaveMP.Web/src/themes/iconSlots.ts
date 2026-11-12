// Icon slots of the theme API. Components name a slot (data-icon); the theme decides the graphic
// through the variable --wmp-icon-<slot>. The defaults are Phosphor icons under /icons/phosphor:
// "fill" in dark areas and toolbars, "duotone" in the sidebar, "regular" for small hints, menus
// and dialogs. src/styles/tokens.css sets these defaults, src/styles/icons.css maps the slots.

export interface IconSlot {
    /** Default graphic, root-absolute. */
    readonly file: string
    /** Area the slot is meant for, documented in THEMING.md. */
    readonly area: 'header' | 'toolbar' | 'sidebar' | 'content' | 'transport' | 'general'
    /** Reserved for a later step; already part of the theme API. */
    readonly reserved?: boolean
}

const fill = (name: string) => `/icons/phosphor/fill/${name}-fill.svg`
const duotone = (name: string) => `/icons/phosphor/duotone/${name}-duotone.svg`
const regular = (name: string) => `/icons/phosphor/regular/${name}.svg`

export const iconSlots = {
    // Header, tabs and bottom navigation
    back: { file: fill('arrow-left'), area: 'header' },
    forward: { file: fill('arrow-right'), area: 'header' },
    'tab-indicator': { file: fill('caret-down'), area: 'header' },
    menu: { file: fill('list'), area: 'header' },
    home: { file: fill('house'), area: 'header' },
    'now-playing': { file: fill('music-notes'), area: 'header' },
    library: { file: fill('books'), area: 'header' },
    playlists: { file: fill('playlist'), area: 'header' },

    // Toolbar
    'breadcrumb-root': { file: fill('music-notes'), area: 'toolbar' },
    'breadcrumb-separator': { file: fill('caret-right'), area: 'toolbar' },
    'view-tiles': { file: fill('squares-four'), area: 'toolbar', reserved: true },
    'view-details': { file: fill('rows'), area: 'toolbar' },
    sort: { file: fill('sort-ascending'), area: 'toolbar' },
    dropdown: { file: fill('caret-down'), area: 'toolbar' },
    search: { file: fill('magnifying-glass'), area: 'toolbar' },
    clear: { file: fill('x'), area: 'toolbar' },

    // Sidebar
    'tree-playlists': { file: duotone('playlist'), area: 'sidebar' },
    'tree-create-playlist': { file: duotone('list-plus'), area: 'sidebar' },
    'tree-library': { file: duotone('books'), area: 'sidebar' },
    'tree-recently-added': { file: duotone('clock-counter-clockwise'), area: 'sidebar' },
    'tree-artists': { file: duotone('microphone-stage'), area: 'sidebar' },
    'tree-albums': { file: duotone('vinyl-record'), area: 'sidebar' },
    'tree-songs': { file: duotone('music-note'), area: 'sidebar' },
    'tree-genres': { file: duotone('guitar'), area: 'sidebar' },
    'tree-years': { file: duotone('calendar-blank'), area: 'sidebar' },
    'tree-recently-played': { file: duotone('headphones'), area: 'sidebar' },
    'tree-playlist': { file: duotone('music-notes-simple'), area: 'sidebar' },
    'tree-favorites': { file: duotone('heart'), area: 'sidebar' },
    'tree-expand': { file: regular('plus-square'), area: 'sidebar' },
    'tree-collapse': { file: regular('minus-square'), area: 'sidebar' },

    // Content
    'has-video': { file: regular('video-camera'), area: 'content' },
    info: { file: regular('info'), area: 'content' },
    'cover-placeholder': { file: fill('music-notes'), area: 'content' },
    'admin-library': { file: regular('folder-open'), area: 'content' },
    'admin-jobs': { file: regular('clock-clockwise'), area: 'content' },
    'admin-cache': { file: regular('hard-drives'), area: 'content' },
    'admin-transcoding': { file: regular('film-reel'), area: 'content' },
    album: { file: fill('vinyl-record'), area: 'content' },
    artist: { file: fill('microphone-stage'), area: 'content' },
    genre: { file: fill('guitar'), area: 'content' },
    year: { file: fill('calendar-blank'), area: 'content' },
    playlist: { file: fill('playlist'), area: 'content' },
    'recently-played': { file: fill('headphones'), area: 'content' },
    'drag-handle': { file: regular('dots-six-vertical'), area: 'content' },
    track: { file: fill('music-note'), area: 'content' },
    favorite: { file: fill('heart'), area: 'content' },
    'favorite-off': { file: regular('heart'), area: 'content' },
    'rating-star': { file: fill('star'), area: 'content' },
    'rating-star-off': { file: regular('star'), area: 'content' },
    fullscreen: { file: fill('corners-out'), area: 'content' },
    'exit-fullscreen': { file: fill('corners-in'), area: 'content' },

    // Transport
    shuffle: { file: fill('shuffle'), area: 'transport' },
    repeat: { file: fill('repeat'), area: 'transport' },
    'repeat-one': { file: fill('repeat-once'), area: 'transport' },
    stop: { file: fill('stop'), area: 'transport' },
    previous: { file: fill('skip-back'), area: 'transport' },
    play: { file: fill('play'), area: 'transport' },
    pause: { file: fill('pause'), area: 'transport' },
    next: { file: fill('skip-forward'), area: 'transport' },
    volume: { file: fill('speaker-high'), area: 'transport' },
    muted: { file: fill('speaker-x'), area: 'transport' },
    compact: { file: fill('corners-in'), area: 'transport' },
    'collapse-player': { file: fill('caret-down'), area: 'transport' },

    // General (menus and dialogs)
    settings: { file: regular('gear-six'), area: 'general' },
    profile: { file: regular('user-circle'), area: 'general' },
    'switch-profile': { file: regular('user-switch'), area: 'general' },
    'manage-profiles': { file: regular('users'), area: 'general' },
    administration: { file: regular('wrench'), area: 'general' },
    scan: { file: regular('arrows-clockwise'), area: 'general' },
    'run-job': { file: regular('play'), area: 'general' },
    edit: { file: regular('pencil-simple'), area: 'general' },
    delete: { file: regular('trash'), area: 'general' },
    add: { file: regular('plus'), area: 'general' },
    'view-video': { file: regular('film-strip'), area: 'general' },
    'view-visualization': { file: regular('waveform'), area: 'general' },
    'view-cover': { file: regular('image'), area: 'general' },
    'go-to-album': { file: regular('vinyl-record'), area: 'general' },
    'go-to-artist': { file: regular('microphone-stage'), area: 'general' },
    'play-now': { file: regular('play'), area: 'general' },
    'play-next': { file: regular('queue'), area: 'general' },
    'add-to-queue': { file: regular('list-plus'), area: 'general' },
    'add-to-playlist': { file: regular('playlist'), area: 'general' },
    'remove-from-playlist': { file: regular('minus-circle'), area: 'general' },
    'change-cover': { file: regular('image-square'), area: 'general' },
    'add-favorite': { file: regular('heart'), area: 'general' },
    'remove-favorite': { file: regular('heart-break'), area: 'general' },
    'shuffle-play': { file: regular('shuffle'), area: 'general' },
    'move-up': { file: regular('caret-up'), area: 'general' },
    'move-down': { file: regular('caret-down'), area: 'general' },
    'remove-from-queue': { file: regular('x'), area: 'general' },
    'clear-queue': { file: regular('trash'), area: 'general' },
    close: { file: regular('x'), area: 'general' },
} as const satisfies Record<string, IconSlot>

export type IconSlotName = keyof typeof iconSlots

export const iconSlotNames = Object.keys(iconSlots) as IconSlotName[]
