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
    'cover-image',

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

    // Library
    'tile-list',
    'tile-item',
    'tile',
    'tile-title',
    'tile-subtitle',
    'detail-header',
    'detail-info',
    'detail-title',
    'detail-actions',
    'load-more',
    'file-list',
    'file-item',
    'file-path',
    'file-meta',

    // Transport
    'transport',
    'seek',
    'now-playing-mini',
    'now-playing-meta',
    'now-playing-title',
    'now-playing-artist',
    'transport-message',
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

    // Profile menu and settings
    'btn-profile',
    'profile-summary',

    // Profile administration
    'view-actions',
    'user-list',
    'user-item',
    'user-info',
    'user-name',
    'user-meta',
    'user-actions',

    // Administration
    'admin-nav',
    'admin-nav-item',
    'section',
    'section-title',
    'scan-status',
    'progress',
    'progress-track',
    'progress-fill',
    'stat-list',
    'stat-item',
    'stat-value',
    'run-list',
    'run-item',
    'run-meta',
    'issue-list',
    'issue-item',
    'issue-path',
    'disclosure',
    'disclosure-summary',
    'job-list',
    'job-item',
    'job-info',
    'job-name',
    'job-meta',
    'job-actions',
    'cache-usage',
    'meter',
    'meter-track',
    'meter-fill',

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
    'checkbox',
    'form',
    'field-row',
    'form-note',
    'select',
    'field-hint',
    'field-error',
    'color-picker',
    'color-swatch',
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
