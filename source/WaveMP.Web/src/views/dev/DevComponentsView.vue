<script setup lang="ts">
// Internal component overview, only part of the development build (see devRoutes). It shows every
// part of the theme API in the current theme and both layouts of the real app side by side.
// Its own structure uses dev-* parts, which are not part of the theme API.
import '@/styles/dev.css'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { CacheUsage, Job, Scan, ScanIssue } from '@/api/generated/models'
import AdminNav from '@/components/admin/AdminNav.vue'
import CacheUsagePanel from '@/components/admin/CacheUsage.vue'
import JobList from '@/components/admin/JobList.vue'
import ScanHistory from '@/components/admin/ScanHistory.vue'
import ScanIssues from '@/components/admin/ScanIssues.vue'
import ScanPanel from '@/components/admin/ScanPanel.vue'
import ScanSummary from '@/components/admin/ScanSummary.vue'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseCheckbox from '@/components/base/BaseCheckbox.vue'
import BaseColorPicker from '@/components/base/BaseColorPicker.vue'
import BaseContextMenu, { type MenuItem } from '@/components/base/BaseContextMenu.vue'
import BaseDialog from '@/components/base/BaseDialog.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import BaseMeter from '@/components/base/BaseMeter.vue'
import BaseProgress from '@/components/base/BaseProgress.vue'
import BaseRoundButton from '@/components/base/BaseRoundButton.vue'
import BaseSearchField from '@/components/base/BaseSearchField.vue'
import BaseSelect from '@/components/base/BaseSelect.vue'
import BaseSlider from '@/components/base/BaseSlider.vue'
import BaseTabs from '@/components/base/BaseTabs.vue'
import BaseTextField from '@/components/base/BaseTextField.vue'
import BaseTree from '@/components/base/BaseTree.vue'
import type { TreeNode } from '@/components/base/BaseTreeItem.vue'
import UserList, { type UserListItem } from '@/components/admin/UserList.vue'
import BaseCover from '@/components/base/BaseCover.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import DetailHeader from '@/components/library/DetailHeader.vue'
import LibraryEmpty from '@/components/library/LibraryEmpty.vue'
import LoadMore from '@/components/library/LoadMore.vue'
import MetaItem from '@/components/library/MetaItem.vue'
import TileList from '@/components/library/TileList.vue'
import TrackFileList from '@/components/library/TrackFileList.vue'
import TrackGroupList from '@/components/library/TrackGroupList.vue'
import ProfileSelect from '@/components/profiles/ProfileSelect.vue'
import ProfileSummary from '@/components/profiles/ProfileSummary.vue'
import type { ProfileTileData } from '@/components/profiles/ProfileTile.vue'
import AppBottomNav from '@/components/shell/AppBottomNav.vue'
import AppHeader from '@/components/shell/AppHeader.vue'
import AppSidebar from '@/components/shell/AppSidebar.vue'
import AppToolbar from '@/components/shell/AppToolbar.vue'
import AppTransport from '@/components/shell/AppTransport.vue'
import type { TransportState } from '@/components/shell/transport'
import { useFormat } from '@/composables/useFormat'
import { setLocale } from '@/i18n'
import { supportedLocales, type SupportedLocale } from '@/i18n/locales'
import { groupTracksByAlbum } from '@/library/grouping'
import { sortItemId, sortLabelKey, sortOptions } from '@/library/sorting'
import { libraryTree, mainNavigation, type NavigationTreeNode } from '@/navigation/libraryTree'
import { profileColorOptions } from '@/profiles/presentation'
import { iconSlotNames, iconSlots } from '@/themes/iconSlots'
import { parts } from '@/themes/parts'
import NotFoundView from '@/views/NotFoundView.vue'
import NowPlayingView from '@/views/NowPlayingView.vue'
import PlaylistsView from '@/views/PlaylistsView.vue'
import { sampleFiles, sampleTiles, sampleTracks } from './samples'

const i18n = useI18n({ useScope: 'global' })
const { t } = i18n
const format = useFormat()

function languageName(locale: SupportedLocale): string {
    return new Intl.DisplayNames([locale], { type: 'language' }).of(locale) ?? locale
}

function switchLanguage(locale: SupportedLocale): void {
    setLocale(i18n, locale)
}

// Buttons, tabs and tree
const tabs = computed(() =>
    mainNavigation.map((item) => ({
        id: item.id,
        label: t(item.labelKey),
        icon: item.icon,
        to: item.to,
    })),
)

function toTreeNode(node: NavigationTreeNode): TreeNode {
    return {
        id: node.id,
        label: t(node.labelKey),
        icon: node.icon,
        to: node.to,
        disabled: node.disabled,
        children: node.children?.map(toTreeNode),
    }
}

const treeNodes = computed(() => libraryTree.map(toTreeNode))
const treeExpanded = ref<string[]>(['playlists', 'library'])
const treeCollapsed = ref<string[]>([])

// List, library and sliders and fields
const selectedTrack = ref<string | undefined>('sample-track-2')
const sampleGroups = computed(() => groupTracksByAlbum(sampleTracks(t)))
const tiles = computed(() => sampleTiles(t))
const files = sampleFiles()
const sortSampleMenu = ref<InstanceType<typeof BaseContextMenu> | null>(null)
const sortSampleItems = computed<MenuItem[]>(() => [
    ...sortOptions('tracks', true).map((key) => ({
        id: sortItemId(key),
        label: t(sortLabelKey(key)),
        checked: key === 'Title',
    })),
    { id: 'separator', separator: true },
    { id: 'direction-asc', label: t('sort.ascending'), checked: true },
    { id: 'direction-desc', label: t('sort.descending'), checked: false },
])

function openSortSample(event: MouseEvent): void {
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect()
    void sortSampleMenu.value?.openAt(rect.left, rect.bottom + 4)
}

/** A picture of the theme stands in for a cover from the server. */
const sampleCover = '/themes/midnight/images/cover-placeholder.svg'
const sliders = ref([0, 0.5, 1])
const disabledSlider = ref(0.3)
const textEmpty = ref('')
const textFilled = ref('')
const searchEmpty = ref('')
const searchFilled = ref('')
const textInvalid = ref('')
const yearSample = ref('2006')
const numberSample = ref('1')
const selected = ref('one')
const selectOptions = computed(() => [
    { value: 'one', label: t('dev.samples.optionOne') },
    { value: 'two', label: t('dev.samples.optionTwo') },
])
textFilled.value = t('dev.samples.textValue')
searchFilled.value = t('dev.samples.searchValue')

// Dialog and context menu
const dialogOpen = ref(false)
const menu = ref<InstanceType<typeof BaseContextMenu> | null>(null)
const menuItems = computed<MenuItem[]>(() => [
    { id: 'edit', label: t('actions.edit'), icon: 'edit' },
    { id: 'delete', label: t('actions.delete'), icon: 'delete' },
    { id: 'separator-1', separator: true },
    { id: 'add', label: t('actions.add'), icon: 'add' },
    { id: 'settings', label: t('actions.settings'), icon: 'settings' },
    { id: 'profile', label: t('actions.profile'), icon: 'profile', disabled: true },
    { id: 'separator-2', separator: true },
    { id: 'view-video', label: t('actions.showVideo'), icon: 'view-video' },
    { id: 'view-visualization', label: t('actions.showVisualization'), icon: 'view-visualization' },
    { id: 'view-cover', label: t('actions.showCover'), icon: 'view-cover' },
])

function openMenuAtElement(event: MouseEvent): void {
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect()
    void menu.value?.openAt(rect.left, rect.bottom + 4)
}

function openMenuAtPointer(event: MouseEvent): void {
    void menu.value?.openAt(event.clientX, event.clientY)
}

// Transport in all states (fixtures)
const idle: TransportState = {
    playback: 'idle',
    position: 0,
    duration: 0,
    shuffle: false,
    repeat: 'off',
    volume: 0.8,
    muted: false,
}
const track = computed(() => ({
    trackId: 'dev-track',
    title: t('placeholders.track', { number: 1 }),
    artist: t('placeholders.artist', { number: 1 }),
    position: 65,
    duration: 225,
}))
const transports = computed(() => [
    { id: 'idle', label: t('dev.transportStates.idle'), state: idle, expanded: false },
    {
        id: 'playing',
        label: t('dev.transportStates.playing'),
        state: { ...idle, ...track.value, playback: 'playing' as const },
        expanded: false,
    },
    {
        id: 'paused',
        label: t('dev.transportStates.paused'),
        state: { ...idle, ...track.value, playback: 'paused' as const },
        expanded: false,
    },
    {
        id: 'shuffle',
        label: t('dev.transportStates.shuffle'),
        state: { ...idle, ...track.value, playback: 'playing' as const, shuffle: true },
        expanded: false,
    },
    {
        id: 'repeat-all',
        label: t('dev.transportStates.repeatAll'),
        state: { ...idle, ...track.value, playback: 'playing' as const, repeat: 'all' as const },
        expanded: false,
    },
    {
        id: 'repeat-one',
        label: t('dev.transportStates.repeatOne'),
        state: { ...idle, ...track.value, playback: 'playing' as const, repeat: 'one' as const },
        expanded: false,
    },
    {
        id: 'muted',
        label: t('dev.transportStates.muted'),
        state: { ...idle, ...track.value, playback: 'paused' as const, muted: true },
        expanded: false,
    },
    {
        id: 'loading',
        label: t('dev.transportStates.loading'),
        state: { ...idle, ...track.value, playback: 'playing' as const, loading: true },
        expanded: false,
    },
    {
        id: 'message',
        label: t('dev.transportStates.message'),
        state: {
            ...idle,
            ...track.value,
            playback: 'playing' as const,
            message: 'unsupported' as const,
        },
        expanded: false,
    },
    {
        id: 'expanded',
        label: t('dev.transportStates.expanded'),
        state: { ...idle, ...track.value, playback: 'playing' as const },
        expanded: true,
    },
])

const libraryLocation = { name: 'library-section', params: { section: 'songs' } }

// Profiles (fixtures; the colors are part of the palette of the server)
const adminProfile = computed<ProfileTileData>(() => ({
    id: 'admin',
    name: t('dev.samples.adminName'),
    initials: t('dev.samples.adminInitials'),
    color: '#1f6fd6',
    isBuiltIn: true,
}))
const sampleProfiles = computed<ProfileTileData[]>(() => [
    {
        id: 'first',
        name: t('dev.samples.profileName'),
        initials: t('dev.samples.profileInitials'),
        color: '#2e7d32',
        isBuiltIn: false,
    },
    {
        id: 'second',
        name: t('dev.samples.secondProfileName'),
        initials: t('dev.samples.secondProfileInitials'),
        color: '#c2185b',
        isBuiltIn: false,
    },
    adminProfile.value,
])
const firstProfile = computed(() => ({ ...sampleProfiles.value[0]!, role: 'Admin' as const }))
const color = ref<string | null>('#2e7d32')
const colorOptions = computed(() =>
    profileColorOptions.map((option) => ({ value: option.value, label: t(option.labelKey) })),
)
const sampleUsers = computed<UserListItem[]>(() =>
    sampleProfiles.value.map((profile, index) => ({
        ...profile,
        isAdmin: profile.isBuiltIn || index === 1,
        createdAt: new Date(Date.UTC(2026, 10, 1 + index)),
    })),
)

// Administration (fixtures of the scan and the jobs)
const checkboxOn = ref(true)
const checkboxOff = ref(false)
const sampleTime = new Date(Date.UTC(2026, 10, 11, 9, 30))
const sampleIssues: ScanIssue[] = [
    { path: 'Artist/Album/03 Broken.mp3', code: 'file.probe_failed' },
    { path: 'Artist/Album/04 Locked.flac', code: 'file.tags_unreadable' },
]
const sampleProcessing: Scan = {
    id: 'processing',
    status: 'Running',
    trigger: 'Manual',
    queuedAt: sampleTime,
    startedAt: sampleTime,
    progress: {
        phase: 'Processing',
        directoriesFound: 42,
        filesFound: 512,
        directoriesToProcess: 6,
        directoriesProcessed: 2,
        filesToRead: 40,
        filesRead: 17,
    },
    issues: [],
}
const sampleDiscovering: Scan = {
    ...sampleProcessing,
    id: 'discovering',
    progress: { ...sampleProcessing.progress, phase: 'Discovering' },
}
const sampleFinished: Scan = {
    id: 'finished',
    status: 'Succeeded',
    trigger: 'Schedule',
    queuedAt: sampleTime,
    startedAt: sampleTime,
    finishedAt: new Date(sampleTime.getTime() + 83_000),
    durationMilliseconds: 83_000,
    isPartial: false,
    statistics: {
        directoriesScanned: 42,
        directoriesChanged: 3,
        filesFound: 512,
        filesAdded: 12,
        filesUpdated: 3,
        filesRemoved: 1,
        filesUnchanged: 496,
        filesFailed: 2,
        tracksAdded: 6,
        tracksUpdated: 2,
        tracksRemoved: 1,
        tracksTotal: 230,
        albumsTotal: 21,
        artistsTotal: 14,
    },
    issues: sampleIssues,
}
const sampleHistory: Scan[] = [
    sampleFinished,
    {
        ...sampleFinished,
        id: 'partial',
        trigger: 'FileWatcher',
        isPartial: true,
        issues: [],
    },
    {
        id: 'failed',
        status: 'Failed',
        trigger: 'Manual',
        queuedAt: sampleTime,
        startedAt: sampleTime,
        finishedAt: sampleTime,
        durationMilliseconds: 120,
        errorCode: 'scan.media_folder_empty',
        issues: [],
    },
    { id: 'interrupted', status: 'Interrupted', trigger: 'Schedule', queuedAt: sampleTime },
]
const sampleIdleJob: Job[] = [
    {
        id: 'library-scan',
        state: 'Idle',
        intervalMinutes: 360,
        nextRunAt: new Date(sampleTime.getTime() + 6 * 3_600_000),
        lastRun: {
            id: 'last',
            jobId: 'library-scan',
            status: 'Succeeded',
            trigger: 'Schedule',
            queuedAt: sampleTime,
            startedAt: sampleTime,
            durationMilliseconds: 83_000,
        },
    },
]
const sampleRunningJob: Job[] = [
    {
        ...sampleIdleJob[0],
        state: 'Running',
        lastRun: { ...sampleIdleJob[0]!.lastRun, status: 'Running', durationMilliseconds: null },
    },
]
const sampleCoverJob: Job[] = [
    {
        id: 'cover-cache',
        state: 'Idle',
        lastRun: {
            id: 'covers',
            jobId: 'cover-cache',
            status: 'Failed',
            trigger: 'LibraryScan',
            queuedAt: sampleTime,
            startedAt: sampleTime,
            durationMilliseconds: 1_200,
            errorCode: 'cache.ffmpeg_unavailable',
        },
    },
]
const sampleCacheUsage: CacheUsage = {
    isAvailable: true,
    sizeBytes: 1_240_000_000,
    entryCount: 5_321,
    maxSizeBytes: 2_000_000_000,
    covers: { sizeBytes: 1_240_000_000, entryCount: 5_321 },
    transcodes: { sizeBytes: 0, entryCount: 0 },
}
</script>

<template>
    <div data-part="dev-components">
        <header data-part="dev-header">
            <h1 data-part="dev-title">{{ t('dev.title') }}</h1>
            <p data-part="dev-intro">{{ t('dev.intro') }}</p>
            <div data-part="dev-language" role="group" :aria-label="t('dev.language')">
                <BaseButton
                    v-for="locale in supportedLocales"
                    :key="locale"
                    :label="languageName(locale)"
                    :pressed="i18n.locale.value === locale"
                    @click="switchLanguage(locale)"
                />
            </div>
        </header>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.buttons') }}</h2>
            <div data-part="dev-samples">
                <BaseButton :label="t('dev.samples.button')" />
                <BaseButton variant="primary" icon="add" :label="t('dev.samples.primary')" />
                <BaseButton :label="t('dev.samples.disabled')" disabled />
                <BaseButton :label="t('dev.samples.pressed')" :pressed="true" />
                <BaseButton variant="icon" icon="settings" :label="t('actions.settings')" />
                <BaseButton variant="icon" icon="edit" :label="t('actions.edit')" :pressed="true" />
                <BaseButton
                    variant="icon"
                    icon="view-details"
                    dropdown
                    :label="t('toolbar.view')"
                />
            </div>
            <div data-part="dev-samples" data-tone="dark">
                <BaseRoundButton icon="back" :label="t('nav.back')" />
                <BaseRoundButton icon="forward" :label="t('nav.forward')" :pressed="true" />
                <BaseRoundButton icon="back" :label="t('dev.samples.disabled')" disabled />
                <BaseButton part="btn-play" variant="round" icon="play" :label="t('player.play')" />
                <BaseButton
                    part="btn-play"
                    variant="round"
                    icon="pause"
                    :label="t('player.pause')"
                    state="playing"
                />
                <BaseButton
                    part="btn-play"
                    variant="round"
                    icon="play"
                    :label="t('dev.samples.disabled')"
                    disabled
                />
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.tabs') }}</h2>
            <div data-part="dev-samples" data-tone="dark">
                <BaseTabs :items="tabs" :label="t('nav.main')" active="library" />
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.tree') }}</h2>
            <div data-part="dev-samples">
                <div data-part="dev-sample">
                    <BaseTree
                        v-model:expanded="treeExpanded"
                        :nodes="treeNodes"
                        :label="t('sidebar.label')"
                        active="songs"
                    />
                </div>
                <div data-part="dev-sample">
                    <BaseTree
                        v-model:expanded="treeCollapsed"
                        :nodes="treeNodes"
                        :label="t('sidebar.label')"
                    />
                </div>
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.list') }}</h2>
            <p data-part="placeholder-notice" data-state="placeholder" role="note">
                <BaseIcon name="info" />
                <span data-part="label">{{ t('library.placeholderNotice') }}</span>
            </p>
            <TrackGroupList
                v-model:selected="selectedTrack"
                :groups="sampleGroups"
                :label="t('library.songs')"
                state="placeholder"
            />
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.library') }}</h2>
            <div data-part="view" data-view="library" data-section="albums">
                <TileList :tiles="tiles" :label="t('library.albums')" />
                <LoadMore />
                <LoadMore loading />
            </div>
            <div data-part="view" data-view="library-album">
                <DetailHeader :title="t('placeholders.album', { number: 1 })">
                    <template #media>
                        <BaseCover :src="sampleCover" />
                    </template>
                    <MetaItem field="artist" :label="t('list.artist')">
                        <BaseLink
                            :to="{ name: 'library-artist', params: { id: 'sample-artist-1' } }"
                        >
                            {{ t('placeholders.artist', { number: 1 }) }}
                        </BaseLink>
                    </MetaItem>
                    <MetaItem field="year" :label="t('list.year')">{{
                        format.year(2006)
                    }}</MetaItem>
                    <MetaItem field="tracks" :label="t('list.songs')">{{
                        format.number(4)
                    }}</MetaItem>
                    <template #actions>
                        <BaseButton
                            data-item="edit-metadata"
                            icon="edit"
                            :label="t('trackMetadata.edit')"
                        />
                    </template>
                </DetailHeader>
                <section data-part="section" data-item="files">
                    <h2 data-part="section-title">{{ t('track.filesTitle') }}</h2>
                    <TrackFileList :files="files" :label="t('files.label')" />
                </section>
            </div>
            <div data-part="dev-samples">
                <BaseButton
                    part="sort"
                    variant="icon"
                    icon="sort"
                    dropdown
                    :label="t('toolbar.sort')"
                    aria-haspopup="menu"
                    @click="openSortSample"
                />
            </div>
            <BaseContextMenu
                ref="sortSampleMenu"
                :items="sortSampleItems"
                :label="t('sort.menu')"
            />
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.sliders') }}</h2>
            <div data-part="dev-samples">
                <div v-for="(_, index) in sliders" :key="index" data-part="dev-sample">
                    <BaseSlider
                        :model-value="sliders[index] ?? 0"
                        :label="t('dev.samples.slider', { value: index + 1 })"
                        :value-text="format.percent(sliders[index] ?? 0)"
                        @update:model-value="sliders[index] = $event"
                    />
                </div>
                <div data-part="dev-sample">
                    <BaseSlider
                        v-model="disabledSlider"
                        :label="t('dev.samples.sliderDisabled')"
                        disabled
                    />
                </div>
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.fields') }}</h2>
            <div data-part="dev-samples">
                <BaseTextField v-model="textEmpty" :label="t('dev.samples.textField')" />
                <BaseTextField v-model="textFilled" :label="t('dev.samples.textField')" />
                <BaseTextField v-model="textFilled" :label="t('dev.samples.disabled')" disabled />
                <BaseSearchField
                    v-model="searchEmpty"
                    :label="t('toolbar.search')"
                    :placeholder="t('toolbar.searchPlaceholder')"
                    :clear-label="t('toolbar.clearSearch')"
                />
                <BaseSearchField
                    v-model="searchFilled"
                    :label="t('toolbar.search')"
                    :placeholder="t('toolbar.searchPlaceholder')"
                    :clear-label="t('toolbar.clearSearch')"
                />
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.forms') }}</h2>
            <div data-part="dev-samples">
                <ProfileSummary
                    :name="firstProfile.name"
                    :initials="firstProfile.initials"
                    :color="firstProfile.color"
                />
            </div>
            <form data-part="form" @submit.prevent>
                <BaseTextField
                    v-model="textFilled"
                    :label="t('dev.samples.textField')"
                    :hint="t('dev.samples.hint')"
                    required
                    :maxlength="50"
                />
                <BaseTextField
                    v-model="textInvalid"
                    :label="t('dev.samples.textField')"
                    :error="t('dev.samples.error')"
                />
                <BaseSelect
                    v-model="selected"
                    :label="t('dev.samples.selectLabel')"
                    :options="selectOptions"
                    :hint="t('dev.samples.hint')"
                />
                <BaseSelect
                    v-model="selected"
                    :label="t('dev.samples.selectLabel')"
                    :options="selectOptions"
                    :error="t('dev.samples.error')"
                />
                <BaseColorPicker
                    v-model="color"
                    :label="t('dev.samples.colorLabel')"
                    :options="colorOptions"
                />
                <div data-part="field-row">
                    <BaseTextField
                        v-model="yearSample"
                        :label="t('trackMetadata.fields.year')"
                        inputmode="numeric"
                        :maxlength="4"
                    />
                    <BaseTextField
                        v-model="numberSample"
                        :label="t('trackMetadata.fields.trackNumber')"
                        inputmode="numeric"
                        :maxlength="4"
                    />
                    <BaseTextField
                        v-model="numberSample"
                        :label="t('trackMetadata.fields.discNumber')"
                        inputmode="numeric"
                        :maxlength="4"
                        disabled
                    />
                </div>
                <p data-part="form-note">{{ t('trackMetadata.emptyRemoves') }}</p>
            </form>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.admin') }}</h2>
            <div data-part="view" data-view="admin-users">
                <div data-part="view-actions">
                    <BaseButton variant="primary" icon="add" :label="t('adminUsers.add')" />
                </div>
                <UserList :users="sampleUsers" />
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.administration') }}</h2>
            <div data-part="view" data-view="admin-library">
                <AdminNav />
                <section data-part="section">
                    <h2 data-part="section-title">{{ t('adminLibrary.scanTitle') }}</h2>
                    <ScanPanel :scan="null" activity="idle" />
                    <ScanPanel :scan="sampleDiscovering" activity="running" />
                    <ScanPanel :scan="sampleProcessing" activity="running" />
                    <ScanSummary :scan="sampleFinished" />
                    <ScanIssues :issues="sampleIssues" :failed="3" open />
                </section>
                <section data-part="section">
                    <h2 data-part="section-title">{{ t('adminLibrary.historyTitle') }}</h2>
                    <ScanHistory :scans="sampleHistory" has-more />
                </section>
                <section data-part="section">
                    <h2 data-part="section-title">{{ t('adminLibrary.settingsTitle') }}</h2>
                    <form data-part="form" @submit.prevent>
                        <BaseCheckbox
                            v-model="checkboxOn"
                            :label="t('adminLibrary.fileWatcher')"
                            :hint="t('adminLibrary.fileWatcherHint')"
                        />
                        <BaseCheckbox
                            v-model="checkboxOff"
                            :label="t('dev.samples.textField')"
                            :error="t('dev.samples.error')"
                        />
                        <BaseProgress :label="t('adminLibrary.progressLabel')" :value="0.4" />
                        <BaseProgress :label="t('adminLibrary.progressLabel')" />
                    </form>
                </section>
            </div>
            <div data-part="view" data-view="admin-jobs">
                <JobList :jobs="sampleIdleJob" />
                <JobList :jobs="sampleRunningJob" />
                <JobList :jobs="sampleCoverJob" />
            </div>
            <div data-part="view" data-view="admin-cache">
                <section data-part="section" data-item="usage">
                    <h2 data-part="section-title">{{ t('adminCache.usageTitle') }}</h2>
                    <CacheUsagePanel :usage="sampleCacheUsage" />
                    <BaseMeter :label="t('adminCache.usageLabel')" :value="0.95" />
                </section>
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.dialog') }}</h2>
            <div data-part="dev-samples">
                <BaseButton :label="t('dev.samples.openDialog')" @click="dialogOpen = true" />
            </div>
            <BaseDialog v-model:open="dialogOpen" :title="t('dev.samples.dialogTitle')">
                <p>{{ t('dev.samples.dialogText') }}</p>
                <template #footer>
                    <BaseButton :label="t('actions.cancel')" @click="dialogOpen = false" />
                    <BaseButton
                        variant="primary"
                        :label="t('actions.confirm')"
                        @click="dialogOpen = false"
                    />
                </template>
            </BaseDialog>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.contextMenu') }}</h2>
            <div data-part="dev-samples">
                <BaseButton
                    :label="t('dev.samples.openContextMenu')"
                    aria-haspopup="menu"
                    @click="openMenuAtElement"
                />
                <div data-part="dev-context-area" @contextmenu.prevent="openMenuAtPointer">
                    {{ t('dev.samples.contextMenuArea') }}
                </div>
            </div>
            <BaseContextMenu
                ref="menu"
                :items="menuItems"
                :label="t('dev.samples.openContextMenu')"
            />
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.alerts') }}</h2>
            <div data-part="dev-samples">
                <BaseAlert tone="error" :message="t('dev.samples.alertError')" />
                <BaseAlert tone="success" :message="t('dev.samples.alertSuccess')" />
                <BaseAlert tone="info" :message="t('dev.samples.alertInfo')" />
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.profiles') }}</h2>
            <div data-part="dev-sample">
                <ProfileSelect
                    :profiles="sampleProfiles"
                    state="ready"
                    current-id="first"
                    :error="t('profiles.selectError')"
                />
            </div>
            <div data-part="dev-samples">
                <div data-part="dev-sample">
                    <p data-part="dev-sample-title">{{ t('dev.samples.firstStart') }}</p>
                    <ProfileSelect :profiles="[adminProfile]" state="ready" />
                </div>
                <div data-part="dev-sample">
                    <p data-part="dev-sample-title">{{ t('dev.samples.loadError') }}</p>
                    <ProfileSelect :profiles="[]" state="error" />
                </div>
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.header') }}</h2>
            <AppHeader active-tab="library" :profile="firstProfile" />
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.toolbar') }}</h2>
            <AppToolbar :location="libraryLocation" />
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.sidebar') }}</h2>
            <div data-part="dev-sample">
                <AppSidebar :location="libraryLocation" />
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.transport') }}</h2>
            <div v-for="transport in transports" :key="transport.id" data-part="dev-sample">
                <p data-part="dev-sample-title">{{ transport.label }}</p>
                <AppTransport :state="transport.state" :expanded="transport.expanded" />
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.bottomNav') }}</h2>
            <div data-part="dev-sample">
                <AppBottomNav active-tab="library" />
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.states') }}</h2>
            <div data-part="dev-samples">
                <div data-part="dev-sample">
                    <div data-part="view" data-view="library" data-section="albums">
                        <LibraryEmpty />
                    </div>
                </div>
                <div data-part="dev-sample">
                    <NowPlayingView />
                </div>
                <div data-part="dev-sample">
                    <PlaylistsView />
                </div>
                <div data-part="dev-sample">
                    <NotFoundView />
                </div>
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.previews') }}</h2>
            <div data-part="dev-previews">
                <figure data-part="dev-preview" data-device="mobile">
                    <figcaption data-part="dev-sample-title">
                        {{ t('dev.previews.mobile') }}
                    </figcaption>
                    <iframe
                        data-part="dev-frame"
                        data-device="mobile"
                        src="/library/songs"
                        :title="t('dev.previews.mobile')"
                    ></iframe>
                </figure>
                <figure data-part="dev-preview" data-device="desktop">
                    <figcaption data-part="dev-sample-title">
                        {{ t('dev.previews.desktop') }}
                    </figcaption>
                    <iframe
                        data-part="dev-frame"
                        data-device="desktop"
                        src="/library/songs"
                        :title="t('dev.previews.desktop')"
                    ></iframe>
                </figure>
            </div>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.parts') }}</h2>
            <ul data-part="dev-part-list" :aria-label="t('dev.table.part')">
                <li v-for="part in parts" :key="part" data-part="dev-part">
                    <code>{{ part }}</code>
                </li>
            </ul>
        </section>

        <section data-part="dev-section">
            <h2 data-part="dev-section-title">{{ t('dev.sections.icons') }}</h2>
            <table data-part="dev-table">
                <thead>
                    <tr>
                        <th scope="col">{{ t('dev.table.preview') }}</th>
                        <th scope="col">{{ t('dev.table.slot') }}</th>
                        <th scope="col">{{ t('dev.table.file') }}</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="slot in iconSlotNames" :key="slot">
                        <td><BaseIcon :name="slot" /></td>
                        <td>
                            <code>{{ slot }}</code>
                        </td>
                        <td>
                            <code>{{ iconSlots[slot].file }}</code>
                        </td>
                    </tr>
                </tbody>
            </table>
        </section>
    </div>
</template>
