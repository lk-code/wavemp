# WaveMP Theme API

Version 2

This document describes how themes change the look of the WaveMP web app: the parts of the DOM a theme can address, their states, the CSS variables and the icons. It is meant for theme authors.

## Overview

A theme is exactly one CSS file plus its graphics. The themes shipped with WaveMP live in `/assets/web/themes/<id>/` and are served as `/themes/<id>/theme.css`, their graphics under `/themes/<id>/images/`. The app loads the active theme with a `<link id="wavemp-theme">` element.

The DOM is the same for every theme. Every element a theme can address has a stable `data-part` attribute, for example `data-part="transport"` or `data-part="btn-play"`. States are expressed with `data-state` tokens and ARIA attributes. Icons and labels are always separate child elements (`data-part="icon"`, `data-part="label"`), so the pseudo-elements `::before` and `::after` of all areas and buttons are free for the theme, for example for gloss and glow effects.

The root element carries the active theme and the version of this API:

```html
<html lang="en" data-theme="midnight" data-theme-api="2">
```

A theme can change everything: colors, graphics, fonts, sizes and the desktop layout. Only the mobile layout is fixed (see [Responsive layout](#responsive-layout)).

Selectors should use attribute selectors with the part names, for example:

```css
[data-part='transport'] [data-part='btn-play']:hover {
    --wmp-image-play-button-hover: url('/themes/example/images/play-hover.svg') center / 100% 100% no-repeat;
}
```

## Cascade

The base styles of WaveMP live completely in cascade layers (`wavemp.reset`, `wavemp.base`, `wavemp.components`, `wavemp.layout`, `wavemp.dev`, `wavemp.mobile`). A theme has no layer, so its rules always win against the base styles, independent of specificity and loading order. Rules of the theme are therefore simple and need no `!important`.

Rules for theme files:

- No `@layer` rules: the theme must stay outside the layers of the base.
- No `@import` rules and no external URLs (`http:`, `https:` or `//`): a theme only uses its own files.
- Every `url()` is root-absolute, for example `url('/themes/midnight/images/glass-button.svg')`. Relative URLs inside CSS variables are resolved against the stylesheet that uses the variable (the base CSS of the app), not against the theme file, so they would point to the wrong place.
- `[hidden]` always hides an element; this cannot be overridden.

## Responsive layout

The layout uses CSS Grid with `grid-template-areas`. The names of the grid areas are the part names, for example `header`, `toolbar`, `sidebar`, `content`, `transport` and `bottom-nav`.

- **Below 64rem** (`@media (width < 64rem)`), the fixed mobile base applies to every theme: a header with menu button, logo, server status and profile button, the search as toolbar, the content, the transport bar as mini player above the bottom navigation, the sidebar as drawer and the full-screen player (`transport` with the state `expanded`). Its layout properties are `!important` inside a cascade layer and cannot be changed by a theme, not even with `!important`. Colors, graphics, fonts and sizes still come from the theme. On detail pages (with a `detail-header`) the mobile base hides `content-title` visually, because `detail-title` shows the name.
- **From 64rem on** (`@media (width >= 64rem)`), the layout of the theme applies. The base provides a neutral desktop layout that a theme replaces in its own `@media (width >= 64rem)` rules. The breakpoint is fixed, because media queries cannot use variables.

The DOM is a superset: all areas exist on every device, including `tabs` and `bottom-nav`, `logo`, `content-title`, `btn-menu`, `sidebar-backdrop` and `btn-collapse`. The desktop layout of a theme decides which of them are visible. A part that is hidden in a grid layout should get `display: none`; parts that are not part of `grid-template-areas` would otherwise create implicit grid cells. To hide a label or a title but keep it for assistive technology, use the pattern from [Rules](#rules).

These parts are grids with named areas:

| Grid | Areas | Base desktop layout |
|---|---|---|
| `app` | `header`, `toolbar`, `sidebar`, `content`, `transport`, `bottom-nav` | `'header header' 'toolbar toolbar' 'sidebar content' 'transport transport'` |
| `header` | `btn-menu`, `logo`, `nav-history`, `tabs`, `server-status`, `btn-profile` | `'logo nav-history tabs server-status btn-profile'` |
| `toolbar` | `breadcrumb`, `view-switch`, `sort`, `search` | `'breadcrumb view-switch sort search'` |
| `transport` | `seek`, `now-playing-mini`, `transport-controls`, `mode-switch`, `btn-collapse` | `'seek seek seek' 'now-playing-mini transport-controls mode-switch'`; `now-playing-actions` shares the area `mode-switch` (`justify-self: start`, `mode-switch` with `justify-self: end`), so the transport controls stay in the middle |
| `list-group` | `list-group-header`, `list-group-info`, `list-group-rows` | `'list-group-header list-group-header' 'list-group-info list-group-rows'` |

Example of a desktop layout with the sidebar on the right:

```css
@media (width >= 64rem) {
    [data-part='app'] {
        grid-template-areas:
            'header header'
            'toolbar toolbar'
            'content sidebar'
            'transport transport';
        grid-template-columns: minmax(0, 1fr) var(--wmp-sidebar-width);
    }
}
```

## Parts

| Part | Element / role | Parent | Description |
|---|---|---|---|
| `app` | `div` | | Grid of all main areas |
| `header` | `header` | `app` | Top bar |
| `btn-menu` | `button` (`aria-expanded`, `aria-controls`) | `header` | Opens the sidebar as drawer (mobile) |
| `logo` | `span` | `header`, `profile-select` | App logo with `logo-image` and `label` |
| `logo-image` | `span` | `logo` | Graphic of the logo (`--wmp-image-logo`) |
| `nav-history` | `div` (`role="group"`) | `header` | Back and forward buttons |
| `btn-back` | `button` | `nav-history` | Back in the history |
| `btn-forward` | `button` | `nav-history` | Forward in the history |
| `tabs` | `nav` | `header` | Main navigation |
| `tab` | `a` (`aria-current`) | `tabs` | Entry of the main navigation with `icon`, `label` and `indicator` |
| `server-status` | `div` (`role="status"`) | `header` | Connection to the server with `status-indicator` and `label` |
| `status-indicator` | `span` | `server-status` | Dot that shows the state |
| `btn-profile` | `button` (`aria-haspopup="menu"`, `aria-expanded`) | `header` | Opens the profile menu (settings, switch profile); `avatar` and `label` with the name |
| `toolbar` | `div` | `app` | Bar below the header |
| `breadcrumb` | `nav` | `toolbar` | Location of the current page |
| `breadcrumb-list` | `ol` | `breadcrumb` | List of the breadcrumb items |
| `breadcrumb-item` | `li` | `breadcrumb-list` | One location with link and separator |
| `breadcrumb-link` | `a`, or `span` (`aria-current="page"`) for the current page | `breadcrumb-item` | Name of a location; the first one has the icon `breadcrumb-root` |
| `breadcrumb-separator` | `span` | `breadcrumb-item` | Separator icon between two items |
| `view-switch` | `button` (`aria-haspopup="menu"`) | `toolbar` | View options of the list (disabled in this version) |
| `sort` | `button` (`aria-haspopup="menu"`) | `toolbar` | Opens the sort options of the current list (a `context-menu` with `menuitemradio` entries); disabled where the list cannot be sorted, always hidden on mobile |
| `search` | `div` (`role="search"`) | `toolbar` | Search field with `icon`, `input` and `btn-clear` |
| `input` | `input` (`aria-invalid`) | `search`, `field` | Text input |
| `btn-clear` | `button` | `search` | Clears the search; `hidden` while the field is empty |
| `sidebar` | `aside` | `app` | Navigation tree; a drawer on mobile |
| `sidebar-backdrop` | `div` | `app` | Backdrop behind the drawer (mobile) |
| `tree` | `ul` (`role="tree"`) | `sidebar` | Tree view |
| `tree-node` | `li` (`role="none"`) | `tree`, `tree-group` | Node with its item and its children |
| `tree-item` | `a` or `span` (`role="treeitem"`) | `tree-node` | Row of a node with `tree-toggle`, `icon` and `label` |
| `tree-toggle` | `span` | `tree-item` | Expand and collapse icon of nodes with children |
| `tree-group` | `ul` (`role="group"`) | `tree-node` | Children of a node |
| `content` | `main` | `app` | Content of the current page |
| `content-title` | `h1` | `content` | Title of the page |
| `view` | `div` | `content` | Page, see `data-view` in [States](#states) |
| `placeholder-notice` | `p` (`role="note"`) | `view` | Notice that the content is a placeholder; currently only shown in the component overview |
| `empty-state` | `div` | `view`, `stage`, `profile-select`, `section` | Message for empty content, or that something could not be loaded |
| `empty-state-title` | `p` | `empty-state` | Title of the message |
| `empty-state-text` | `p` | `empty-state` | Text of the message |
| `link` | `a` | `empty-state`, `view`, `section`, `meta-value` | Text link in the content, for example to an album, an artist, a genre or a year |
| `stage` | `div` | `view` | Stage of the Now Playing page: the `cover`, or the `video-stage` of a song with a video while the video is switched on, then `stage-info` and, for songs with a video, `stage-actions` |
| `stage-info` | `div` | `stage` | Title, artist and album of the current song |
| `stage-title` | `p` | `stage-info` | Title of the current song |
| `stage-subtitle` | `p` | `stage-info` | Artist and album of the current song |
| `stage-actions` | `div` (`role="group"`) | `stage` | Buttons of the stage; only for songs with a video |
| `btn-video` | `button` (`aria-pressed`) | `stage-actions` | Shows the video of songs instead of their cover (pressed) or the cover only; the choice applies to all songs with a video, the visualization follows later |
| `video-stage` | `div` | `stage` | The video of the current song with `video` and `btn-fullscreen`; shown in full screen as a whole (a double click switches as well). When it closes, the song continues as audio at the same position |
| `video` | `video` (`aria-label`) | `video-stage` | The video element; without native controls, the transport bar controls it |
| `btn-fullscreen` | `button` | `video-stage` | Shows the `video-stage` in full screen (icon `fullscreen`) or leaves it (icon `exit-fullscreen`); disabled where the browser has no full screen. On the iPhone the video element itself goes full screen with the controls of the system |
| `cover` | `span` | `stage`, `list-group-info`, `now-playing-mini`, `tile`, `detail-header` | Cover: `cover-image`, or the placeholder icon (`data-state="placeholder"`); on tiles and detail pages of artists, genres and years the `icon` of their kind |
| `cover-image` | `img` (`alt=""`) | `cover` | Picture of the cover, loaded from the server |
| `list` | `div` (`role="grid"`) | `view` | Table of tracks grouped by album |
| `list-header` | `div` (`role="row"`) | `list` | Column headers |
| `list-column` | `span` (`role="columnheader"`) | `list-header` | Column header with `label` |
| `list-group` | `div` (`role="rowgroup"`) | `list` | Group of rows, for example an album |
| `list-group-header` | `div` (`role="row"`) | `list-group` | Heading row of the group |
| `list-group-title` | `span` (`role="rowheader"`) | `list-group-header` | Title of the group |
| `list-group-info` | `div` (`role="row"`) | `list-group` | Cover and details of the group |
| `list-group-meta` | `div` (`role="gridcell"`) | `list-group-info` | Details of the group |
| `meta-item` | `span` | `list-group-meta`, `detail-info` | One detail with `label` and `meta-value`, see `data-field` in [States](#states) |
| `meta-value` | `span` | `meta-item` | Value of a detail, often a `link` |
| `list-group-rows` | `div` (`role="presentation"`) | `list-group` | Rows of the group |
| `list-row` | `div` (`role="row"`, `aria-selected`) | `list-group-rows` | Row of a track |
| `list-cell` | `span` (`role="gridcell"`) | `list-row` | Cell of a row |
| `badge` | `span` | `list-cell`, `user-meta`, `scan-status`, `run-item`, `run-meta`, `job-meta`, `file-meta` | Small hint with `icon` and `label`, for example that a video exists; in `user-meta` the role, in `scan-status`, `run-item`, `run-meta` and `job-meta` a state, in `file-meta` the kind and use of a file, each with a visible `label` |
| `tile-list` | `ul` | `view`, `section` | Albums, artists, genres or years as tiles |
| `tile-item` | `li` | `tile-list` | One tile |
| `tile` | `a` | `tile-item` | Link to the page of the entry with `cover`, `tile-title` and optionally `tile-subtitle`, see `data-kind` in [States](#states) |
| `tile-title` | `span` | `tile` | Name of the entry |
| `tile-subtitle` | `span` | `tile` | Detail of the entry, for example the album artist or the number of songs |
| `detail-header` | `div` | `view` | Head of a detail page (album, artist, genre, year, song, playlist) with a `cover` (or `playlist-cover`) and `detail-info` |
| `detail-info` | `div` | `detail-header` | `detail-title`, the details as `meta-item` and optionally `detail-actions` |
| `detail-title` | `h2` | `detail-info` | Name of the album, artist, genre, year or song |
| `detail-actions` | `div` | `detail-info` | Actions of a detail page as `button`s, for example "Edit metadata" of a song for admins, see `data-item` in [States](#states) |
| `load-more` | `button` | `view`, `section` | Loads the next page of a list; also loads by itself when it scrolls into view |
| `file-list` | `ul` | `section` | Files of a song on its page, for admins |
| `file-item` | `li` | `file-list` | One file with `file-path` and `file-meta` |
| `file-path` | `span` | `file-item` | Path of the file relative to the media folder |
| `file-meta` | `span` | `file-item` | Kind, use and problem of the file as `badge` with a visible `label`, and its technical details (format, bitrate, resolution, size, ...) as `label`s |
| `playlist-cover` | `span` (`aria-hidden`, `data-count`) | `tile`, `detail-header`, `playlist-choice` | Cover of a playlist without a picture of its own: the covers of up to four of its songs (of different albums) as `playlist-cover-slide`s, shown one after the other; `data-count` holds their number. A playlist with its own picture shows a `cover` instead, one without any song cover a `cover` with the icon `playlist` |
| `playlist-cover-slide` | `span` | `playlist-cover` | One song cover (a `cover`) of a `playlist-cover`; the shown one has the state `active`. The base stacks the slides and fades between them; with `prefers-reduced-motion` the slides do not change |
| `playlist-choices` | `ul` (`aria-label`) | `dialog-body` | Playlists of the profile in the dialog "Add to playlist" |
| `playlist-choice` | `button` | `playlist-choices` (in a `li`) | One playlist with its cover, its name as `label` and `playlist-choice-count`; adds the songs at its end |
| `playlist-choice-count` | `span` | `playlist-choice` | Number of songs of the playlist |
| `drag-handle` | `span` | `list-cell` with `data-column="handle"` | Handle that drags an entry of a playlist to another place (mouse, pen and touch), with `icon` and a `label` for screen readers; the menu of the entry moves it with the keyboard |
| `btn-favorite` | `button` (`aria-pressed`) | `detail-actions`, `list-cell` with `data-column="favorite"`, `now-playing-actions` | Heart that marks a song, album or artist as favorite of the profile, with `icon` and `label`; in lists it is no tab stop (the menu of the song offers the same) |
| `rating` | `span` (`role="group"`) | `meta-value` of the `meta-item` `rating`, `list-cell` with `data-column="rating"`, `now-playing-actions` | Rating of a song with five `rating-star`s; `data-value` holds the stars (0 to 5) |
| `rating-star` | `button` (`aria-pressed` for the star of the rating) | `rating` | One star with `icon` and `label`; a click rates the song with it, a click on the star of the rating removes it; in lists no tab stop |
| `scope-switch` | `div` (`role="group"`) | `view` (library) | Switch between the whole library and the "my music" of the profile: two `button`s (`data-item` `all-music` and `my-music`, `aria-pressed`) |
| `shelf` | `section` | `view` (home) | One row of the home page with `shelf-header` and a `tile-list` of its first entries |
| `shelf-header` | `div` | `shelf` | `shelf-title` and `shelf-actions` |
| `shelf-title` | `h2` | `shelf-header` | Name of the row |
| `shelf-actions` | `div` | `shelf-header` | Actions of the row (`button` `play`) and a `link` to the whole list |
| `queue` | `section` (`aria-labelledby`) | `view` | Queue on the Now Playing page: a `section-title`, the `button` `clear-queue` and the `queue-list`; missing while the queue is empty. On the desktop the base shows it beside the stage |
| `queue-list` | `ol` | `queue` | Entries of the queue in playing order |
| `queue-item` | `li` (`aria-current`) | `queue-list` | One entry with `queue-item-info`, `queue-item-duration` and `queue-item-actions` |
| `queue-item-info` | `button` | `queue-item` | Title and artist of the entry; plays it |
| `queue-item-title` | `span` | `queue-item-info` | Title of the song |
| `queue-item-artist` | `span` | `queue-item-info` | Artist of the song |
| `queue-item-duration` | `span` | `queue-item` | Duration of the song |
| `queue-item-actions` | `div` (`role="group"`) | `queue-item` | Icon `button`s `move-up`, `move-down` and `remove` |
| `transport` | `section` | `app` | Transport bar; mini player and full-screen player on mobile |
| `seek` | `div` | `transport` | Progress bar with a `slider` |
| `now-playing-mini` | `button` | `transport` | Cover and title of the current track; opens Now Playing (desktop) or the full-screen player (mobile) |
| `now-playing-meta` | `span` | `now-playing-mini` | Title and artist |
| `now-playing-title` | `span` | `now-playing-meta` | Title of the current track |
| `now-playing-artist` | `span` | `now-playing-meta` | Artist of the current track |
| `now-playing-actions` | `div` (`role="group"`) | `transport` | `btn-favorite` and `rating` of the current song; empty (state `empty`) while nothing plays; on mobile only in the full-screen player |
| `transport-message` | `p` (`role="status"`) | `transport` | Notice of the player, for example that a song was skipped; empty while there is none. The base shows it above the transport bar, outside the grid |
| `transport-controls` | `div` (`role="group"`) | `transport` | Playback controls |
| `btn-shuffle` | `button` (`aria-pressed`) | `transport-controls` | Shuffle |
| `btn-repeat` | `button` (`aria-pressed`) | `transport-controls` | Repeat off, all or one |
| `btn-stop` | `button` | `transport-controls` | Stop |
| `btn-prev` | `button` | `transport-controls` | Previous track |
| `btn-play` | `button` | `transport-controls` | Play and pause |
| `btn-next` | `button` | `transport-controls` | Next track |
| `btn-mute` | `button` (`aria-pressed`) | `transport-controls` | Mute |
| `volume` | `div` | `transport-controls` | Volume with a `slider` |
| `mode-switch` | `div` (`role="group"`) | `transport` | Buttons for player modes |
| `btn-compact` | `button` | `mode-switch` | Compact mode (disabled in this version) |
| `btn-collapse` | `button` | `transport` | Closes the full-screen player (mobile) |
| `bottom-nav` | `nav` | `app` | Main navigation at the bottom (mobile) |
| `bottom-nav-item` | `a` (`aria-current`) | `bottom-nav` | Entry with `icon` and `label` |
| `profile-select` | `main` | `body` (without `app`) | Profile selection, shown before a profile is selected; with `logo`, title, `profile-list` and, on the first start, a hint |
| `profile-select-title` | `h1` | `profile-select` | Title of the profile selection |
| `profile-select-hint` | `p` (`role="note"`) | `profile-select` | Hint on the first start, when only the built-in admin profile exists |
| `profile-list` | `ul` | `profile-select` | Profiles to select |
| `profile-item` | `li` | `profile-list` | One profile |
| `profile-tile` | `button` | `profile-item` | Selects the profile; `avatar` and `label` (name) |
| `avatar` | `span` (`aria-hidden`) | `profile-tile`, any | Initials on the color of the profile (`--wmp-profile-color`) |
| `avatar-initials` | `span` | `avatar` | Initials of the profile |
| `profile-summary` | `div` | `view` | The current profile on the settings page: `avatar` and `label` with the name |
| `view-actions` | `div` | `view` | Actions of a page, for example "Add profile" |
| `user-list` | `ul` | `view` | Profiles on the admin page |
| `user-item` | `li` | `user-list` | One profile with `avatar`, `user-info` and `user-actions` |
| `user-info` | `div` | `user-item` | Name and details of the profile |
| `user-name` | `span` | `user-info` | Name of the profile |
| `user-meta` | `span` | `user-info` | Role and built-in state as `badge` with a visible `label`, and the creation date as `label` |
| `user-actions` | `div` | `user-item` | Buttons to edit and delete the profile |
| `admin-nav` | `nav` | `view` | Navigation between the pages of the administration (library, jobs, cache, profiles) |
| `admin-nav-item` | `a` (`aria-current`) | `admin-nav` | Page of the administration with `icon` and `label` |
| `section` | `section` (`aria-labelledby`) | `view` | Part of a page with a `section-title` |
| `section-title` | `h2` | `section`, `queue` | Title of the section or of the queue |
| `scan-status` | `div` | `section` | State of the library scan: `badge` with a visible `label`, while running the phase as `label`, a `progress` and its value as `label`, and the button that starts a scan |
| `progress` | `div` (`role="progressbar"`) | `scan-status`, any | Progress bar; the share done is `--wmp-progress-value` |
| `progress-track` | `div` | `progress` | Background of the bar |
| `progress-fill` | `div` | `progress-track` | Done part of the bar |
| `stat-list` | `dl` | `section` | Statistics of a scan |
| `stat-item` | `div` | `stat-list` | One statistic: `label` (`dt`) and `stat-value` |
| `stat-value` | `dd` | `stat-item` | Number of the statistic |
| `run-list` | `ul` | `section` | Earlier runs, for example the history of scans |
| `run-item` | `li` | `run-list` | One run with a status `badge` with a visible `label`, its time as `label` and `run-meta` |
| `run-meta` | `span`, or `div` above the statistics | `run-item`, `section` | Details of a run as `label`s: trigger, duration, changes, error; above the statistics also the status `badge` |
| `issue-list` | `ul` | `disclosure` | Files with problems |
| `issue-item` | `li` | `issue-list` | One file with `issue-path` and the problem as `label` |
| `issue-path` | `span` | `issue-item` | Path of the file relative to the media folder |
| `disclosure` | `details` | `section`, any | Content that opens on demand |
| `disclosure-summary` | `summary` | `disclosure` | Always visible line of a `disclosure` with `label` and optionally `field-hint` |
| `job-list` | `ul` | `view` | Background jobs |
| `job-item` | `li` | `job-list` | One job with `job-info` and `job-actions` |
| `job-info` | `div` | `job-item` | Name, details and the error of the last run as `label` |
| `job-name` | `span` | `job-info` | Name of the job |
| `job-meta` | `span` | `job-info` | Status `badge` of the last run with a visible `label`, and last run, next run and schedule as `label`s |
| `job-actions` | `div` | `job-item` | Button that runs the job now |
| `cache-usage` | `div` | `section` | Used space of the cache: a `meter`, the used space and the limit as `label`, a `stat-list` with the areas and the number of files, a hint as `label` and the button that clears the cache |
| `meter` | `div` (`role="meter"`) | `cache-usage`, any | Level within a range, for example the used space of the cache; the share is `--wmp-meter-value`. It uses the variables of `progress` |
| `meter-track` | `div` | `meter` | Background of the meter |
| `meter-fill` | `div` | `meter-track` | Filled part of the meter |
| `button` | `button` | any | Generic button, see `data-variant` in [States](#states) |
| `icon` | `span` (`aria-hidden`) | any | Icon wrapper with the slot in `data-icon`, see [Icons](#icons) |
| `icon-glyph` | `span` | `icon` | Masked shape of the icon |
| `label` | `span` | any | Text of a button, entry or area |
| `indicator` | `span` | `tab` | Small arrow of the active tab |
| `slider` | `div` | `seek`, `volume`, any | Slider with `slider-track` and `slider-thumb` |
| `slider-track` | `div` | `slider` | Track of the slider |
| `slider-fill` | `div` | `slider-track` | Filled part of the track |
| `slider-thumb` | `div` (`role="slider"`) | `slider` | Knob, focusable |
| `field` | `label`, or `div` around a `color-picker` | `form`, any | Text field or selection with `label`, `input`, `select` or `color-picker`, and optionally `field-hint` and `field-error`; with `data-variant="checkbox"` a `checkbox` before its `label` |
| `checkbox` | `input type="checkbox"` (`:checked`) | `field` | Checkbox of a field with `data-variant="checkbox"` |
| `form` | `form` | `view`, `dialog-body` | Form with fields |
| `field-row` | `div` | `form` | Short fields side by side, for example year, track and disc number in the metadata editor |
| `form-note` | `p` | `form` | Note of a form, for example the file the metadata editor writes |
| `select` | `select` (`aria-invalid`) | `field` | Selection list |
| `field-hint` | `span` | `field` | Help text of the field |
| `field-error` | `span` | `field` | Error message of the field |
| `color-picker` | `div` (`role="radiogroup"`) | `field` | Selection of a color |
| `color-swatch` | `button` (`role="radio"`, `aria-checked`) | `color-picker` | One color (`--wmp-swatch-color`) with a visually hidden `label` |
| `dialog` | `dialog` | any | Modal dialog |
| `dialog-header` | `div` | `dialog` | Title bar of the dialog |
| `dialog-title` | `h2` | `dialog-header` | Title of the dialog |
| `btn-close` | `button` | `dialog-header` | Closes the dialog |
| `dialog-body` | `div` | `dialog` | Content of the dialog |
| `dialog-footer` | `div` | `dialog` | Buttons of the dialog |
| `context-menu` | `div` (`role="menu"`, `popover`) | any | Context menu, shown in the top layer |
| `menu-item` | `button` (`role="menuitem"`, or `role="menuitemradio"` with `aria-checked` in choices such as the sort options) | `context-menu` | Entry with `icon` and `label` |
| `menu-separator` | `hr` | `context-menu` | Separator between entries |
| `alert` | `div` (`role="alert"` for errors, otherwise `role="status"`) | any | Message with `icon` and `label` |

The internal component overview (`/dev/components`) uses its own parts starting with `dev-`. They are not part of the theme API.

## States

### data-state

`data-state` holds space-separated tokens; check single tokens with `[data-state~='token']`.

| Part | Tokens |
|---|---|
| `transport` | `idle`, `playing` or `paused`; additionally `loading` while a song loads or waits for data, and `expanded` while the full-screen player is open (mobile) |
| `transport-message` | `empty` without a notice, otherwise its kind: `unsupported` (the browser cannot play the song and the server cannot transcode it), `unavailable` (it could not be loaded) or `failed` (it failed to play) |
| `btn-play` | `idle`, `playing` or `paused` |
| `btn-shuffle` | `on` or `off` |
| `btn-repeat` | `off`, `all` or `one` |
| `btn-mute` | `muted` or `unmuted` |
| `seek` | `disabled` while nothing plays |
| `volume` | `muted` |
| `slider` | `disabled`, `dragging` |
| `sidebar`, `sidebar-backdrop` | `open` or `closed` (drawer on mobile) |
| `tab`, `bottom-nav-item`, `breadcrumb-item`, `tree-item` | `active` for the current page |
| `tree-item` | additionally `expanded` or `collapsed` for nodes with children |
| `list` | `placeholder` while the list shows placeholder content (component overview); `busy` while a change of a playlist is being saved |
| `list-row` | `selected`; `has-video` for songs with a video, additionally `video-only` for songs with a video only (no audio); `playing` for the current song of the player |
| `btn-favorite` | `on` for a favorite, otherwise `off` |
| `rating` | `rated` or `unrated` |
| `rating-star` | `on` up to the rating, otherwise `off` |
| `now-playing-actions` | `empty` while nothing plays |
| `stage` | `video` while it shows the `video-stage`, otherwise `cover` |
| `video-stage` | `fullscreen` while it is shown in full screen |
| `queue-item` | `current` for the current song, `played` for the songs before it |
| `placeholder-notice`, `cover` | `placeholder` |
| `now-playing-mini` | `empty` while nothing plays |
| `server-status` | `checking`, `healthy`, `degraded`, `unhealthy` or `unreachable` |
| `search` | `empty` or `filled` |
| `context-menu` | `open` or `closed` |
| `badge` | the kind of hint, for example `has-video`, for songs with a video only additionally `video-only`; in `file-meta` `audio`, `video`, `image`, `primary`, `lossless`, `writable` or `issue`; in `user-meta` `admin` or `user`, and `built-in`; in `scan-status` `idle`, `queued` or `running`; in `run-item`, `run-meta` and `job-meta` the status of the run: `queued`, `running`, `succeeded`, `failed`, `canceled` or `interrupted` |
| `profile-select` | `loading`, `ready` or `error` (the profiles could not be loaded) |
| `profile-tile` | `built-in` for the built-in admin profile, additionally `secondary` as soon as other profiles exist; `current` for the profile of the current session |
| `alert` | `error`, `success` or `info` |
| `field` | `invalid` while the field has an error |
| `user-item` | `built-in` for the built-in admin profile |
| `admin-nav-item` | `active` for the current page |
| `scan-status`, `job-item` | `idle`, `queued` or `running` |
| `run-item` | `queued`, `running`, `succeeded`, `failed`, `canceled` or `interrupted` |
| `progress` | `indeterminate` while the share done is unknown |
| `load-more` | `loading` while the next page loads |
| `file-item` | `audio`, `video` or `image`; additionally `primary` for the file the song plays |
| `playlist-cover-slide` | `active` for the song cover that is shown |
| `playlist-choice` | `busy` while the songs are being added to this playlist |

### ARIA attributes and native states

| Attribute or state | Parts |
|---|---|
| `aria-current="page"` | `tab`, `bottom-nav-item`, `breadcrumb-link`, `tree-item`, `admin-nav-item` |
| `aria-current="true"` | `profile-tile` of the current profile, `queue-item` of the current song |
| `aria-busy="true"` | `profile-select` while a profile is being selected; `view` while its content loads; `load-more` while the next page loads |
| `aria-expanded` | `tree-item`, `btn-menu`, `btn-profile`, `now-playing-mini` (mobile) |
| `aria-invalid="true"` | `input` and `select` of a field with an error |
| `aria-checked` | `color-swatch`; `menu-item` with `role="menuitemradio"`, for example the chosen sort key and direction |
| `aria-selected` | `list-row` |
| `aria-pressed` | `btn-shuffle`, `btn-repeat`, `btn-mute`, `btn-favorite`, `rating-star`, `btn-video`, the `button`s of `scope-switch` and other toggle buttons |
| `aria-disabled="true"` | `tree-item`, `slider-thumb` |
| `aria-valuemin`, `aria-valuemax`, `aria-valuenow`, `aria-valuetext` | `slider-thumb`, `progress` (`aria-valuenow` from 0 to 100, missing while indeterminate), `meter` (`aria-valuenow` from 0 to 100) |
| `:disabled` | buttons and inputs |
| `:checked` | `checkbox` |
| `[open]` | `dialog`, `disclosure` |
| `:popover-open` | `context-menu` |

The base links states to graphics: buttons use the variables `--wmp-image-<variant>-button`, `-hover`, `-active` and `-disabled` for `:hover`, `:active` and `[aria-pressed='true']`, and `:disabled`. A theme can therefore change all button states by setting variables only.

### Other attributes

| Attribute | Parts | Values |
|---|---|---|
| `data-variant` | `button` and all `btn-*` parts | `default`, `primary`, `icon`, `round` |
| `data-variant` | `field` | `checkbox` |
| `data-icon` | `icon` | Icon slot, see [Icons](#icons) |
| `data-view` | `view` | `library`, `library-search`, `library-album`, `library-artist`, `library-genre`, `library-year`, `library-track`, `home`, `now-playing`, `playlists`, `playlist`, `recently-played`, `settings`, `admin-library`, `admin-jobs`, `admin-cache`, `admin-users`, `not-found` |
| `data-kind` | `tile` | `album`, `artist`, `track`, `genre`, `year`, `playlist`, `recently-played` |
| `data-section` | `view` (library) | `recently-added`, `favorites`, `artists`, `albums`, `songs`, `genres`, `years` |
| `data-value` | `rating` | Stars of the rating, `0` (none) to `5` |
| `data-column` | `list-column`, `list-cell` | `number`, `title`, `duration`, `artist`, `rating`, `favorite`; in playlists also `handle` (first column, only where entries can be moved), `album` and `detail` (in "Recently played" the time of the last play, in the favorites the time the song was marked) |
| `data-count` | `playlist-cover` | Number of song covers (1 to 4) |
| `data-field` | `meta-item` | `album`, `artist`, `genre`, `year`, `tracks`, `albums`, `duration`, `number`, `disc`, `media`, `rating` |
| `data-field` | `field` (metadata editor) | `title`, `artist`, `album`, `album-artist`, `year`, `number`, `disc`, `genre` |
| `data-field` | `field` (settings) | `language`, `replay-gain` |
| `data-item` | `menu-item`, `admin-nav-item`, `stat-item`, `job-item`, `section`, `shelf`, `button` in `view-actions`, `detail-actions`, `scope-switch`, `shelf-actions`, `queue` and `queue-item-actions` | Id of the entry: the menu entry (in the profile menu `settings`, `administration`, `switch-profile`; in the menu of a song `play`, `play-next` and `add-to-queue` for songs with audio, `add-to-playlist`, `favorite`, then `details`, `album`, `artist`, for admins `edit-metadata` and on the page of a playlist `move-up`, `move-down` and `remove-from-playlist`; in the sort options `sort-album-artist`, `sort-album`, `sort-title`, `sort-artist`, `sort-name`, `sort-duration`, `sort-year`, `sort-genre`, `sort-first-seen-at`, `sort-track-count`, `sort-rating`, `sort-relevance`, `direction-asc`, `direction-desc`), the page (`admin-library`, `admin-jobs`, `admin-cache`, `admin-users`), the statistic (for example `filesFound`, `tracksTotal`, `duration`; of the cache `covers`, `transcodes`, `entries`), the job (`library-scan`, `cover-cache`) or the section of the search, of an artist and of the favorites (`artists`, `albums`, `tracks`), the row of the home page (`playlists`, `recently-played`, `recently-added`, `favorites`, `top-rated`), of a song (`files`) and of the cache page (`usage`, `settings`), the action of a page (`play`, `shuffle`, `favorite`, `edit-metadata`; of the library `all-music` and `my-music`; of a row of the home page `play`; of a playlist also `rename`, `cover`, `remove-cover`, `delete`; on the playlist overview `create-playlist`) or of the queue (`clear-queue`; per entry `move-up`, `move-down`, `remove`) |

## Variables

All variables are set on `:root` with neutral defaults and can be overridden by the theme, also for single areas (for example `--wmp-icon-fill` inside `[data-part='header']`).

Graphics and backgrounds take any value of the CSS `background` shorthand: a color, a gradient or a root-absolute `url()` with position and size, also several layers. Example: `url('/themes/midnight/images/glass-button.svg') center / 100% 100% no-repeat`.

### Colors

| Variable | Default |
|---|---|
| `--wmp-color-accent` | `#1f6fd6` |
| `--wmp-color-accent-contrast` | `#ffffff` |
| `--wmp-color-focus` | `#1f6fd6` |
| `--wmp-color-text` | `#1b1d21` |
| `--wmp-color-text-muted` | `#5b6270` |
| `--wmp-color-text-disabled` | `#9aa0aa` |
| `--wmp-color-surface` | `#ffffff` |
| `--wmp-color-surface-raised` | `#f1f3f6` |
| `--wmp-color-border` | `#d3d8df` |
| `--wmp-color-hover` | `#e7f0fb` |
| `--wmp-color-selection` | `#cfe2fa` |
| `--wmp-color-selection-text` | `#1b1d21` |
| `--wmp-color-success` | `#2e9d4f` |
| `--wmp-color-warning` | `#c98a12` |
| `--wmp-color-danger` | `#c83737` |
| `--wmp-favorite-color` | `#d6336c` |
| `--wmp-rating-color` | `#e0a100` |
| `--wmp-backdrop-color` | `rgb(0 0 0 / 45%)` |

### Areas

Text color and background of each area.

| Variable | Default |
|---|---|
| `--wmp-header-color` | `var(--wmp-color-text)` |
| `--wmp-header-background` | `var(--wmp-color-surface-raised)` |
| `--wmp-toolbar-color` | `var(--wmp-color-text)` |
| `--wmp-toolbar-background` | `var(--wmp-color-surface)` |
| `--wmp-sidebar-color` | `var(--wmp-color-text)` |
| `--wmp-sidebar-background` | `var(--wmp-color-surface)` |
| `--wmp-content-color` | `var(--wmp-color-text)` |
| `--wmp-content-background` | `var(--wmp-color-surface)` |
| `--wmp-transport-color` | `var(--wmp-color-text)` |
| `--wmp-transport-background` | `var(--wmp-color-surface-raised)` |
| `--wmp-bottom-nav-color` | `var(--wmp-color-text)` |
| `--wmp-bottom-nav-background` | `var(--wmp-color-surface-raised)` |
| `--wmp-dialog-color` | `var(--wmp-color-text)` |
| `--wmp-dialog-background` | `var(--wmp-color-surface)` |
| `--wmp-menu-color` | `var(--wmp-color-text)` |
| `--wmp-menu-background` | `var(--wmp-color-surface)` |
| `--wmp-profile-select-color` | `var(--wmp-color-text)` |
| `--wmp-profile-select-background` | `var(--wmp-color-surface-raised)` |

### Profiles

| Variable | Default | Used for |
|---|---|---|
| `--wmp-avatar-color` | `#ffffff` | Text color of the initials on `avatar` |
| `--wmp-profile-secondary-opacity` | `0.55` | Opacity of the built-in admin `profile-tile` with the state `secondary` |

The background of `avatar` is the color of the profile, `--wmp-profile-color` (see [Runtime variables](#runtime-variables)). The app sets it on the `avatar` element itself, so themes use it in rules for `[data-part='avatar']`, for example `background: linear-gradient(rgb(255 255 255 / 40%), transparent 55%), var(--wmp-profile-color);`. A variable on `:root` that refers to it would not work.

### Graphics

| Variable | Default | Used for |
|---|---|---|
| `--wmp-image-logo` | `url('/brand/logo.svg') center / contain no-repeat` | `logo-image` |
| `--wmp-image-cover-placeholder` | `var(--wmp-color-surface-raised)` | `cover` without image |
| `--wmp-progress-track-background` | `var(--wmp-color-border)` | `progress-track`, `meter-track` |
| `--wmp-progress-fill-background` | `var(--wmp-color-accent)` | `progress-fill`, `meter-fill` |
| `--wmp-image-default-button` | `var(--wmp-color-surface)` | Buttons with `data-variant="default"` |
| `--wmp-image-default-button-hover` | `var(--wmp-color-hover)` | |
| `--wmp-image-default-button-active` | `var(--wmp-color-selection)` | Also `aria-pressed="true"` |
| `--wmp-image-default-button-disabled` | `var(--wmp-color-surface-raised)` | |
| `--wmp-image-primary-button` | `var(--wmp-color-accent)` | Buttons with `data-variant="primary"` |
| `--wmp-image-primary-button-hover` | `color-mix(in srgb, var(--wmp-color-accent), #ffffff 15%)` | |
| `--wmp-image-primary-button-active` | `color-mix(in srgb, var(--wmp-color-accent), #000000 15%)` | |
| `--wmp-image-primary-button-disabled` | `var(--wmp-color-border)` | |
| `--wmp-image-icon-button` | `transparent` | Buttons with `data-variant="icon"` |
| `--wmp-image-icon-button-hover` | `var(--wmp-color-hover)` | |
| `--wmp-image-icon-button-active` | `var(--wmp-color-selection)` | |
| `--wmp-image-icon-button-disabled` | `transparent` | |
| `--wmp-image-round-button` | `var(--wmp-color-surface)` | Buttons with `data-variant="round"` (round glass buttons) |
| `--wmp-image-round-button-hover` | `var(--wmp-color-hover)` | |
| `--wmp-image-round-button-active` | `var(--wmp-color-selection)` | |
| `--wmp-image-round-button-disabled` | `var(--wmp-color-surface-raised)` | |
| `--wmp-image-play-button` | `var(--wmp-color-accent)` | `btn-play` |
| `--wmp-image-play-button-hover` | `color-mix(in srgb, var(--wmp-color-accent), #ffffff 15%)` | |
| `--wmp-image-play-button-active` | `color-mix(in srgb, var(--wmp-color-accent), #000000 15%)` | |
| `--wmp-image-play-button-disabled` | `var(--wmp-color-border)` | |
| `--wmp-image-transport-controls` | `transparent` | `transport-controls` |
| `--wmp-image-tab-active` | `var(--wmp-color-selection)` | Active `tab` |
| `--wmp-image-tab-hover` | `var(--wmp-color-hover)` | `tab` on hover |
| `--wmp-image-slider-thumb` | `var(--wmp-color-accent)` | `slider-thumb` |
| `--wmp-slider-track-background` | `var(--wmp-color-border)` | `slider-track` |
| `--wmp-slider-fill-background` | `var(--wmp-color-accent)` | `slider-fill` |

### Icons

| Variable | Default | Used for |
|---|---|---|
| `--wmp-icon-fill` | `currentColor` | Fill of the icon shape: a color or a gradient |
| `--wmp-icon-size` | `1.25rem` | Size of icons |
| `--wmp-icon-size-s` | `1rem` | Small icons (tree toggles, search, separators) |
| `--wmp-icon-size-l` | `2rem` | Large icons |

The graphic of each icon slot is a variable as well, see [Icons](#icons).

### Spacing and radii

| Variable | Default |
|---|---|
| `--wmp-space-xs` | `0.25rem` |
| `--wmp-space-s` | `0.5rem` |
| `--wmp-space-m` | `0.75rem` |
| `--wmp-space-l` | `1rem` |
| `--wmp-space-xl` | `1.5rem` |
| `--wmp-radius-s` | `0.25rem` |
| `--wmp-radius-m` | `0.5rem` |
| `--wmp-radius-l` | `0.75rem` |
| `--wmp-radius-round` | `999px` |

### Fonts

| Variable | Default |
|---|---|
| `--wmp-font-family` | `system-ui, sans-serif` |
| `--wmp-font-size-xs` | `0.75rem` |
| `--wmp-font-size-s` | `0.8125rem` |
| `--wmp-font-size-m` | `0.9375rem` |
| `--wmp-font-size-l` | `1.125rem` |
| `--wmp-font-size-xl` | `1.5rem` |
| `--wmp-font-weight-normal` | `400` |
| `--wmp-font-weight-bold` | `600` |
| `--wmp-line-height` | `1.4` |

### Sizes

| Variable | Default | Used for |
|---|---|---|
| `--wmp-header-height` | `3rem` | Minimum height of `header` |
| `--wmp-toolbar-height` | `2.75rem` | Minimum height of `toolbar` |
| `--wmp-sidebar-width` | `15rem` | Width of the sidebar column (desktop) |
| `--wmp-transport-height` | `4.5rem` | Minimum height of `transport` (desktop) |
| `--wmp-seek-height` | `0.25rem` | Height of the progress line (mini player) |
| `--wmp-bottom-nav-height` | `3.5rem` | Height of `bottom-nav-item` |
| `--wmp-mini-player-height` | `3.5rem` | Minimum height of the mini player (mobile) |
| `--wmp-button-height` | `2rem` | Minimum height of buttons |
| `--wmp-field-height` | `2rem` | Minimum height of inputs |
| `--wmp-round-button-size` | `2rem` | Size of round buttons |
| `--wmp-play-button-size` | `3rem` | Size of `btn-play` |
| `--wmp-slider-thumb-size` | `0.875rem` | Size of `slider-thumb` |
| `--wmp-cover-size` | `8rem` | Size of covers in the list |
| `--wmp-cover-size-mini` | `2.5rem` | Size of the cover in `now-playing-mini` |
| `--wmp-cover-size-l` | `12rem` | Size of the cover in `detail-header` |
| `--wmp-tile-width` | `9rem` | Smallest width of a column of tiles |
| `--wmp-list-row-height` | `2rem` | Minimum height of `list-row` |
| `--wmp-list-columns` | `3rem minmax(0, 2fr) 4.5rem minmax(0, 1.5fr) 6.5rem 2rem` | `grid-template-columns` of `list-header` and `list-row` (number, title, duration, artist, rating, favorite) |
| `--wmp-list-columns-playlist` | `2rem 3rem minmax(0, 2fr) 4.5rem minmax(0, 1.5fr) minmax(0, 1.5fr) 6.5rem 2rem` | `--wmp-list-columns` of the list in `data-view="playlist"` (handle, number, title, duration, artist, album, rating, favorite) |
| `--wmp-list-columns-recently-played` | `3rem minmax(0, 2fr) 4.5rem minmax(0, 1.5fr) minmax(0, 1.5fr) 10rem 6.5rem 2rem` | `--wmp-list-columns` of the list in `data-view="recently-played"` and of the songs in `data-section="favorites"` (number, title, duration, artist, album, detail, rating, favorite) |
| `--wmp-shelf-tile-width` | `var(--wmp-tile-width)` | Width of a tile in a `shelf` of the home page |
| `--wmp-list-group-info-width` | `11rem` | Width of `list-group-info` (desktop) |
| `--wmp-avatar-size` | `2rem` | Size of `avatar` |
| `--wmp-avatar-size-l` | `6rem` | Size of `avatar` in `profile-tile` |
| `--wmp-swatch-size` | `2rem` | Size of `color-swatch` |
| `--wmp-progress-height` | `0.5rem` | Height of `progress-track` and `meter-track` |

On mobile, the base sets touch-friendly values for `--wmp-button-height`, `--wmp-round-button-size`, `--wmp-field-height` and `--wmp-swatch-size` on `app`, and its own `--wmp-list-columns` on `list` (in playlists with the handle); the columns `artist`, `album`, `detail` and `rating` are left out there.

### Runtime variables

The app sets these variables on single elements. Themes can read them, but never set them.

| Variable | Set on | Value |
|---|---|---|
| `--wmp-slider-value` | `slider` | Position of the value between 0 and 1 |
| `--wmp-tree-level` | `tree-item` | Level of the node, starting at 1 |
| `--wmp-menu-x` | `context-menu` | Horizontal position in pixels |
| `--wmp-menu-y` | `context-menu` | Vertical position in pixels |
| `--wmp-profile-color` | `avatar` | Color of the profile as `#rrggbb` |
| `--wmp-swatch-color` | `color-swatch` | Color of the swatch as `#rrggbb` |
| `--wmp-progress-value` | `progress` | Share of the work done between 0 and 1 (0 while indeterminate) |
| `--wmp-meter-value` | `meter` | Share of the range between 0 and 1 |

## Icons

Icons are masks. Every icon consists of two elements:

```html
<span data-part="icon" data-icon="play" aria-hidden="true">
    <span data-part="icon-glyph"></span>
</span>
```

- `icon-glyph` is masked with the SVG file of the slot (`mask-image`) and filled with `--wmp-icon-fill`. Only the shape of the SVG counts, never its color; semi-transparent layers (for example the back layer of duotone icons) appear as lighter shades of the fill. The fill can be a color or a gradient.
- Glow and shadow belong on the wrapper `icon`, for example `filter: drop-shadow(0 0 4px #4aa8ff)`. On `icon-glyph`, the mask would cut them off.
- A theme exchanges a graphic with the slot variable `--wmp-icon-<slot>`, for example `--wmp-icon-play: url('/themes/example/images/play.svg')`. Use longhand mask properties if a theme sets its own masks.

The defaults are icons of [Phosphor Icons](https://phosphoricons.com) (MIT) under `/icons/phosphor/<style>/`. The style follows the area: `fill` in dark areas and toolbars, `duotone` in the sidebar, `regular` for small hints in the content, in menus and dialogs. Slots marked as reserved are part of the API but not used yet.

| Slot | Variable | Default file | Area |
|---|---|---|---|
| `back` | `--wmp-icon-back` | `/icons/phosphor/fill/arrow-left-fill.svg` | Header |
| `forward` | `--wmp-icon-forward` | `/icons/phosphor/fill/arrow-right-fill.svg` | Header |
| `tab-indicator` | `--wmp-icon-tab-indicator` | `/icons/phosphor/fill/caret-down-fill.svg` | Header |
| `menu` | `--wmp-icon-menu` | `/icons/phosphor/fill/list-fill.svg` | Header |
| `home` | `--wmp-icon-home` | `/icons/phosphor/fill/house-fill.svg` | Tabs, bottom navigation |
| `now-playing` | `--wmp-icon-now-playing` | `/icons/phosphor/fill/music-notes-fill.svg` | Tabs, bottom navigation |
| `library` | `--wmp-icon-library` | `/icons/phosphor/fill/books-fill.svg` | Tabs, bottom navigation |
| `playlists` | `--wmp-icon-playlists` | `/icons/phosphor/fill/playlist-fill.svg` | Tabs, bottom navigation |
| `breadcrumb-root` | `--wmp-icon-breadcrumb-root` | `/icons/phosphor/fill/music-notes-fill.svg` | Toolbar |
| `breadcrumb-separator` | `--wmp-icon-breadcrumb-separator` | `/icons/phosphor/fill/caret-right-fill.svg` | Toolbar |
| `view-tiles` | `--wmp-icon-view-tiles` | `/icons/phosphor/fill/squares-four-fill.svg` | Toolbar (reserved) |
| `view-details` | `--wmp-icon-view-details` | `/icons/phosphor/fill/rows-fill.svg` | Toolbar |
| `sort` | `--wmp-icon-sort` | `/icons/phosphor/fill/sort-ascending-fill.svg` | Toolbar |
| `dropdown` | `--wmp-icon-dropdown` | `/icons/phosphor/fill/caret-down-fill.svg` | Toolbar |
| `search` | `--wmp-icon-search` | `/icons/phosphor/fill/magnifying-glass-fill.svg` | Toolbar |
| `clear` | `--wmp-icon-clear` | `/icons/phosphor/fill/x-fill.svg` | Toolbar |
| `tree-playlists` | `--wmp-icon-tree-playlists` | `/icons/phosphor/duotone/playlist-duotone.svg` | Sidebar |
| `tree-create-playlist` | `--wmp-icon-tree-create-playlist` | `/icons/phosphor/duotone/list-plus-duotone.svg` | Sidebar |
| `tree-library` | `--wmp-icon-tree-library` | `/icons/phosphor/duotone/books-duotone.svg` | Sidebar |
| `tree-recently-added` | `--wmp-icon-tree-recently-added` | `/icons/phosphor/duotone/clock-counter-clockwise-duotone.svg` | Sidebar |
| `tree-artists` | `--wmp-icon-tree-artists` | `/icons/phosphor/duotone/microphone-stage-duotone.svg` | Sidebar |
| `tree-albums` | `--wmp-icon-tree-albums` | `/icons/phosphor/duotone/vinyl-record-duotone.svg` | Sidebar |
| `tree-songs` | `--wmp-icon-tree-songs` | `/icons/phosphor/duotone/music-note-duotone.svg` | Sidebar |
| `tree-genres` | `--wmp-icon-tree-genres` | `/icons/phosphor/duotone/guitar-duotone.svg` | Sidebar |
| `tree-years` | `--wmp-icon-tree-years` | `/icons/phosphor/duotone/calendar-blank-duotone.svg` | Sidebar |
| `tree-recently-played` | `--wmp-icon-tree-recently-played` | `/icons/phosphor/duotone/headphones-duotone.svg` | Sidebar |
| `tree-playlist` | `--wmp-icon-tree-playlist` | `/icons/phosphor/duotone/music-notes-simple-duotone.svg` | Sidebar (each playlist of the profile) |
| `tree-favorites` | `--wmp-icon-tree-favorites` | `/icons/phosphor/duotone/heart-duotone.svg` | Sidebar |
| `tree-expand` | `--wmp-icon-tree-expand` | `/icons/phosphor/regular/plus-square.svg` | Sidebar |
| `tree-collapse` | `--wmp-icon-tree-collapse` | `/icons/phosphor/regular/minus-square.svg` | Sidebar |
| `has-video` | `--wmp-icon-has-video` | `/icons/phosphor/regular/video-camera.svg` | Content |
| `info` | `--wmp-icon-info` | `/icons/phosphor/regular/info.svg` | Content |
| `cover-placeholder` | `--wmp-icon-cover-placeholder` | `/icons/phosphor/fill/music-notes-fill.svg` | Content |
| `admin-library` | `--wmp-icon-admin-library` | `/icons/phosphor/regular/folder-open.svg` | Content (administration) |
| `admin-jobs` | `--wmp-icon-admin-jobs` | `/icons/phosphor/regular/clock-clockwise.svg` | Content (administration) |
| `admin-cache` | `--wmp-icon-admin-cache` | `/icons/phosphor/regular/hard-drives.svg` | Content (administration) |
| `album` | `--wmp-icon-album` | `/icons/phosphor/fill/vinyl-record-fill.svg` | Content (library: tiles and pages without a cover) |
| `artist` | `--wmp-icon-artist` | `/icons/phosphor/fill/microphone-stage-fill.svg` | Content (library: tiles and pages without a cover) |
| `genre` | `--wmp-icon-genre` | `/icons/phosphor/fill/guitar-fill.svg` | Content (library: tiles and pages without a cover) |
| `year` | `--wmp-icon-year` | `/icons/phosphor/fill/calendar-blank-fill.svg` | Content (library: tiles and pages without a cover) |
| `playlist` | `--wmp-icon-playlist` | `/icons/phosphor/fill/playlist-fill.svg` | Content (playlists without any cover) |
| `recently-played` | `--wmp-icon-recently-played` | `/icons/phosphor/fill/headphones-fill.svg` | Content (tile and page of "Recently played") |
| `drag-handle` | `--wmp-icon-drag-handle` | `/icons/phosphor/regular/dots-six-vertical.svg` | Content (`drag-handle` of playlist entries) |
| `track` | `--wmp-icon-track` | `/icons/phosphor/fill/music-note-fill.svg` | Content (tiles of songs without cover) |
| `favorite` | `--wmp-icon-favorite` | `/icons/phosphor/fill/heart-fill.svg` | Content (`btn-favorite` of a favorite) |
| `favorite-off` | `--wmp-icon-favorite-off` | `/icons/phosphor/regular/heart.svg` | Content (`btn-favorite` of anything else) |
| `rating-star` | `--wmp-icon-rating-star` | `/icons/phosphor/fill/star-fill.svg` | Content (`rating-star` up to the rating) |
| `rating-star-off` | `--wmp-icon-rating-star-off` | `/icons/phosphor/regular/star.svg` | Content (the other `rating-star`s) |
| `fullscreen` | `--wmp-icon-fullscreen` | `/icons/phosphor/fill/corners-out-fill.svg` | Content (`btn-fullscreen` of the video) |
| `exit-fullscreen` | `--wmp-icon-exit-fullscreen` | `/icons/phosphor/fill/corners-in-fill.svg` | Content (`btn-fullscreen` in full screen) |
| `shuffle` | `--wmp-icon-shuffle` | `/icons/phosphor/fill/shuffle-fill.svg` | Transport |
| `repeat` | `--wmp-icon-repeat` | `/icons/phosphor/fill/repeat-fill.svg` | Transport |
| `repeat-one` | `--wmp-icon-repeat-one` | `/icons/phosphor/fill/repeat-once-fill.svg` | Transport |
| `stop` | `--wmp-icon-stop` | `/icons/phosphor/fill/stop-fill.svg` | Transport |
| `previous` | `--wmp-icon-previous` | `/icons/phosphor/fill/skip-back-fill.svg` | Transport |
| `play` | `--wmp-icon-play` | `/icons/phosphor/fill/play-fill.svg` | Transport |
| `pause` | `--wmp-icon-pause` | `/icons/phosphor/fill/pause-fill.svg` | Transport |
| `next` | `--wmp-icon-next` | `/icons/phosphor/fill/skip-forward-fill.svg` | Transport |
| `volume` | `--wmp-icon-volume` | `/icons/phosphor/fill/speaker-high-fill.svg` | Transport |
| `muted` | `--wmp-icon-muted` | `/icons/phosphor/fill/speaker-x-fill.svg` | Transport |
| `compact` | `--wmp-icon-compact` | `/icons/phosphor/fill/corners-in-fill.svg` | Transport |
| `collapse-player` | `--wmp-icon-collapse-player` | `/icons/phosphor/fill/caret-down-fill.svg` | Transport |
| `settings` | `--wmp-icon-settings` | `/icons/phosphor/regular/gear-six.svg` | Menus and dialogs |
| `profile` | `--wmp-icon-profile` | `/icons/phosphor/regular/user-circle.svg` | Menus and dialogs |
| `switch-profile` | `--wmp-icon-switch-profile` | `/icons/phosphor/regular/user-switch.svg` | Menus and dialogs |
| `manage-profiles` | `--wmp-icon-manage-profiles` | `/icons/phosphor/regular/users.svg` | Menus and dialogs, administration |
| `administration` | `--wmp-icon-administration` | `/icons/phosphor/regular/wrench.svg` | Menus and dialogs |
| `scan` | `--wmp-icon-scan` | `/icons/phosphor/regular/arrows-clockwise.svg` | Menus and dialogs |
| `run-job` | `--wmp-icon-run-job` | `/icons/phosphor/regular/play.svg` | Menus and dialogs |
| `edit` | `--wmp-icon-edit` | `/icons/phosphor/regular/pencil-simple.svg` | Menus and dialogs |
| `delete` | `--wmp-icon-delete` | `/icons/phosphor/regular/trash.svg` | Menus and dialogs |
| `add` | `--wmp-icon-add` | `/icons/phosphor/regular/plus.svg` | Menus and dialogs |
| `view-video` | `--wmp-icon-view-video` | `/icons/phosphor/regular/film-strip.svg` | Menus and dialogs |
| `view-visualization` | `--wmp-icon-view-visualization` | `/icons/phosphor/regular/waveform.svg` | Menus and dialogs |
| `view-cover` | `--wmp-icon-view-cover` | `/icons/phosphor/regular/image.svg` | Menus and dialogs |
| `go-to-album` | `--wmp-icon-go-to-album` | `/icons/phosphor/regular/vinyl-record.svg` | Menus and dialogs |
| `go-to-artist` | `--wmp-icon-go-to-artist` | `/icons/phosphor/regular/microphone-stage.svg` | Menus and dialogs |
| `play-now` | `--wmp-icon-play-now` | `/icons/phosphor/regular/play.svg` | Menus and dialogs |
| `play-next` | `--wmp-icon-play-next` | `/icons/phosphor/regular/queue.svg` | Menus and dialogs |
| `add-to-queue` | `--wmp-icon-add-to-queue` | `/icons/phosphor/regular/list-plus.svg` | Menus and dialogs |
| `add-to-playlist` | `--wmp-icon-add-to-playlist` | `/icons/phosphor/regular/playlist.svg` | Menus and dialogs |
| `remove-from-playlist` | `--wmp-icon-remove-from-playlist` | `/icons/phosphor/regular/minus-circle.svg` | Menus and dialogs |
| `change-cover` | `--wmp-icon-change-cover` | `/icons/phosphor/regular/image-square.svg` | Menus and dialogs |
| `add-favorite` | `--wmp-icon-add-favorite` | `/icons/phosphor/regular/heart.svg` | Menus and dialogs |
| `remove-favorite` | `--wmp-icon-remove-favorite` | `/icons/phosphor/regular/heart-break.svg` | Menus and dialogs |
| `shuffle-play` | `--wmp-icon-shuffle-play` | `/icons/phosphor/regular/shuffle.svg` | Menus and dialogs |
| `move-up` | `--wmp-icon-move-up` | `/icons/phosphor/regular/caret-up.svg` | Menus and dialogs |
| `move-down` | `--wmp-icon-move-down` | `/icons/phosphor/regular/caret-down.svg` | Menus and dialogs |
| `remove-from-queue` | `--wmp-icon-remove-from-queue` | `/icons/phosphor/regular/x.svg` | Menus and dialogs |
| `clear-queue` | `--wmp-icon-clear-queue` | `/icons/phosphor/regular/trash.svg` | Menus and dialogs |
| `close` | `--wmp-icon-close` | `/icons/phosphor/regular/x.svg` | Menus and dialogs |

## Rules

- `::before` and `::after` of all parts belong to the theme; the base never uses them.
- Labels are never removed. To hide a label visually, keep it for assistive technology:

    ```css
    [data-part='btn-play'] > [data-part='label'] {
        position: absolute;
        inline-size: 1px;
        block-size: 1px;
        overflow: hidden;
        clip-path: inset(50%);
        white-space: nowrap;
    }
    ```

    The base already does this for buttons with `data-variant="icon"` or `"round"`; they show their label as tooltip.
- Keep the focus outline (`:focus-visible`) visible; `--wmp-color-focus` sets its color.
- Fonts: use fonts that are installed on the device or ship font files only under a license that allows it.
- Graphics: only own graphics or graphics under a compatible license. No logos, icons or other assets of third-party products.
- Do not set `display` on `context-menu` or `dialog` without `:popover-open` or `[open]`, otherwise they stay visible when closed.

## Preview

The internal component overview shows all parts in all states, the icon slots and the app at mobile and desktop size side by side. It is only available in the development server:

```bash
cd source/WaveMP.Web
npm run dev
```

Then open `http://localhost:5173/dev/components`.

## Versioning

The version of the theme API is in `data-theme-api` on the root element and at the top of this document. Changing or removing an existing part, state, attribute or variable increases the version. New parts, states, icon slots and variables do not.

### Changelog

- **Version 2:** favorites, ratings and the home page. Changed: the lists of songs have the new columns `rating` (stars) and `favorite` (heart) at their end, so the defaults of `--wmp-list-columns`, `--wmp-list-columns-playlist` and `--wmp-list-columns-recently-played` have two more columns; the new part `now-playing-actions` of `transport` takes the start of the grid area `mode-switch`, which now ends at the end of its cell (`justify-self: end`). Themes that set these variables have to add the new columns; themes that rearrange `transport` have to place `now-playing-actions`. Added: the parts `btn-favorite`, `rating`, `rating-star`, `scope-switch`, `now-playing-actions`, `shelf`, `shelf-header`, `shelf-title` and `shelf-actions`, their states, the attribute `data-value`, the home page (`data-view="home"`, tab and bottom navigation entry `home`), the library section `favorites`, the tile kind `track`, the `meta-item` `rating` on the page of a song, new values of `data-item` (menu entry `favorite`; detail action `favorite`; `all-music`, `my-music`; the rows `playlists`, `recently-played`, `recently-added`, `favorites`, `top-rated` of `shelf`), the sort key `sort-rating`, the variables `--wmp-favorite-color`, `--wmp-rating-color` and `--wmp-shelf-tile-width` and the icon slots `home`, `track`, `favorite`, `favorite-off`, `rating-star`, `rating-star-off`, `add-favorite` and `remove-favorite`; `tree-favorites` is no longer reserved.
    - Additions without a version change: videos. The `stage` shows the video of songs with one (`stage-actions`, `btn-video`, `video-stage`, `video`, `btn-fullscreen`, the states `video` and `cover` of `stage` and `fullscreen` of `video-stage`) and the icon slot `exit-fullscreen`; `fullscreen` is no longer reserved and belongs to the content. The notice `unsupported` of `transport-message` now means that the server cannot transcode the song.
- **Version 1:** first version with the app shell, the base components and the theme Midnight.
    - Additions without a version change: the profile selection (`profile-select`, `profile-select-title`, `profile-select-hint`, `profile-list`, `profile-item`, `profile-tile`, `avatar`, `avatar-initials`), the profile menu `btn-profile` with the new header area `btn-profile`, the settings page (`profile-summary`, `data-view="settings"`), the profile administration (`view-actions`, `user-list`, `user-item`, `user-info`, `user-name`, `user-meta`, `user-actions`, `data-view="admin-users"`), the base components `alert`, `form`, `select`, `field-hint`, `field-error`, `color-picker` and `color-swatch`, the icon slots `switch-profile` and `manage-profiles`, the variables `--wmp-profile-select-color`, `--wmp-profile-select-background`, `--wmp-avatar-color`, `--wmp-profile-secondary-opacity`, `--wmp-avatar-size`, `--wmp-avatar-size-l` and `--wmp-swatch-size` and the runtime variables `--wmp-profile-color` and `--wmp-swatch-color`.
    - Additions without a version change: the administration (`admin-nav`, `admin-nav-item`, `section`, `section-title`, `scan-status`, `progress`, `progress-track`, `progress-fill`, `stat-list`, `stat-item`, `stat-value`, `run-list`, `run-item`, `run-meta`, `issue-list`, `issue-item`, `issue-path`, `disclosure`, `disclosure-summary`, `job-list`, `job-item`, `job-info`, `job-name`, `job-meta`, `job-actions`, `data-view="admin-library"` and `data-view="admin-jobs"`), the base component `checkbox` with `data-variant="checkbox"` on `field`, the attribute `data-item` (`menu-item` already carried it, now it is documented), the states of `admin-nav-item`, `scan-status`, `job-item`, `run-item`, `progress` and the new tokens of `badge`, the icon slots `administration`, `admin-library`, `admin-jobs`, `scan` and `run-job`, the variables `--wmp-progress-height`, `--wmp-progress-track-background` and `--wmp-progress-fill-background` and the runtime variable `--wmp-progress-value`. The profile menu entry `manage-profiles` became `administration`.
    - Additions without a version change: the library with tiles (`tile-list`, `tile-item`, `tile`, `tile-title`, `tile-subtitle`, attribute `data-kind`), detail pages (`detail-header`, `detail-info`, `detail-title`, `data-view` `library-album`, `library-artist`, `library-genre`, `library-year`, `library-track`) and the search (`data-view="library-search"`), the part `cover-image`, the button `load-more` with the state `loading`, the files of a song for admins (`file-list`, `file-item`, `file-path`, `file-meta` and their states), the state `video-only` of `list-row` and `badge` (in addition to `has-video`, which keeps its meaning), `aria-busy` on `view`, `menuitemradio` entries with `aria-checked` in the sort options, further values of `data-field` and `data-item` (also on `section`), `link` in `meta-value`, `view` and `section`, the icon slots `album`, `artist`, `genre`, `year`, `go-to-album` and `go-to-artist` and the variables `--wmp-cover-size-l` and `--wmp-tile-width`. The sort button `sort` is active where a list can be sorted.
    - Additions without a version change: the metadata editor of a song for admins in a `dialog` (`detail-actions` with the `button` `edit-metadata`, `field-row`, `form-note`, `data-field` on its `field`s, the menu entry `edit-metadata` in the menu of a song). Disabled inputs (`input:disabled`) are greyed out by the base; an open `dialog` fits the screen and scrolls its `dialog-body`.
    - Additions without a version change: the cache page of the administration (`cache-usage`, `data-view="admin-cache"`, the entry `admin-cache` of `admin-nav`, further values of `data-item`) with the base component `meter` (`meter-track`, `meter-fill`, `role="meter"` with `aria-valuenow`), which uses the variables of `progress`, the runtime variable `--wmp-meter-value` and the icon slot `admin-cache`.
    - Additions without a version change: the player. The transport bar plays (`transport-message` with its states, the state `loading` of `transport`), Now Playing shows the current song (`stage-info`, `stage-title`, `stage-subtitle`) and the queue (`queue`, `queue-list`, `queue-item` with the states `current` and `played` and `aria-current`, `queue-item-info`, `queue-item-title`, `queue-item-artist`, `queue-item-duration`, `queue-item-actions`). The reserved state `playing` of `list-row` is now set. `data-field` on the `field`s of the settings, new values of `data-item` (menu entries `play`, `play-next`, `add-to-queue`; detail actions `play`, `shuffle`; queue buttons) and the icon slots `play-now`, `play-next`, `add-to-queue`, `shuffle-play`, `move-up`, `move-down`, `remove-from-queue` and `clear-queue`.
    - Additions without a version change: the playlists. Tiles of playlists (`data-kind` `playlist` and `recently-played`) with `playlist-cover` and `playlist-cover-slide`, the pages `data-view="playlist"` and `data-view="recently-played"` with `drag-handle` and the columns `handle`, `album` and `detail`, the dialog "Add to playlist" (`playlist-choices`, `playlist-choice`, `playlist-choice-count`), the states `busy` of `list` and `playlist-choice` and `active` of `playlist-cover-slide`, the attribute `data-count`, new values of `data-item` (menu entries `add-to-playlist`, `move-up`, `move-down`, `remove-from-playlist`; actions `rename`, `cover`, `remove-cover`, `delete`, `create-playlist`) the variables `--wmp-list-columns-playlist` and `--wmp-list-columns-recently-played` and the icon slots `tree-recently-played`, `tree-playlist`, `playlist`, `recently-played`, `drag-handle`, `add-to-playlist`, `remove-from-playlist` and `change-cover`.
