// Parts of the theme API: every element a theme can address has a stable data-part attribute.
// THEMING.md documents each part; a unit test keeps this list, THEMING.md and the components in
// sync. Changing or removing an existing part increases the version, adding a new part does not.

/** Version of the theme API (data-theme-api on <html>). */
export const themeApiVersion = 1

export const parts = [
    // App
    'app',

    // Header
    'header',
    'btn-menu',
    'logo',
    'logo-image',
    'nav-history',
    'btn-back',
    'btn-forward',
    'tabs',
    'tab',
    'server-status',
    'status-indicator',

    // Toolbar
    'toolbar',
    'breadcrumb',
    'breadcrumb-list',
    'breadcrumb-item',
    'breadcrumb-link',
    'breadcrumb-separator',
    'view-switch',
    'sort',
    'search',
    'input',
    'btn-clear',

    // Sidebar
    'sidebar',
    'sidebar-backdrop',
    'tree',
    'tree-node',
    'tree-item',
    'tree-toggle',
    'tree-group',

    // Content
    'content',
    'content-title',
    'view',
    'placeholder-notice',
    'empty-state',
    'empty-state-title',
    'empty-state-text',
    'link',
    'stage',
    'cover',

    // List
    'list',
    'list-header',
    'list-column',
    'list-group',
    'list-group-header',
    'list-group-title',
    'list-group-info',
    'list-group-meta',
    'meta-item',
    'meta-value',
    'list-group-rows',
    'list-row',
    'list-cell',
    'badge',

    // Transport
    'transport',
    'seek',
    'now-playing-mini',
    'now-playing-meta',
    'now-playing-title',
    'now-playing-artist',
    'transport-controls',
    'btn-shuffle',
    'btn-repeat',
    'btn-stop',
    'btn-prev',
    'btn-play',
    'btn-next',
    'btn-mute',
    'volume',
    'mode-switch',
    'btn-compact',
    'btn-collapse',

    // Mobile
    'bottom-nav',
    'bottom-nav-item',

    // Profile selection
    'profile-select',
    'profile-select-title',
    'profile-select-hint',
    'profile-list',
    'profile-item',
    'profile-tile',
    'avatar',
    'avatar-initials',

    // Base components
    'button',
    'icon',
    'icon-glyph',
    'label',
    'indicator',
    'slider',
    'slider-track',
    'slider-fill',
    'slider-thumb',
    'field',
    'dialog',
    'dialog-header',
    'dialog-title',
    'btn-close',
    'dialog-body',
    'dialog-footer',
    'context-menu',
    'menu-item',
    'menu-separator',
    'alert',
] as const

export type PartName = (typeof parts)[number]

/** Parts of the internal component overview (/dev/components); not part of the theme API. */
export const devPartPrefix = 'dev-'
