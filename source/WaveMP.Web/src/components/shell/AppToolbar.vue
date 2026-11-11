<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseContextMenu, { type MenuItem } from '@/components/base/BaseContextMenu.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import BaseSearchField from '@/components/base/BaseSearchField.vue'
import { useSearchField } from '@/composables/useSearchField'
import { queryText } from '@/library/search'
import {
    effectiveSort,
    parseSort,
    sortContext,
    sortItemId,
    sortLabelKey,
    sortOptions,
} from '@/library/sorting'
import { buildBreadcrumb, type RouteInfo } from '@/navigation/breadcrumb'
import { usePageTitleStore } from '@/stores/pageTitle'

const props = defineProps<{
    /** Location of the breadcrumb and the sort options; by default the current route. */
    location?: RouteInfo
}>()

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const pageTitle = usePageTitleStore()

const crumbs = computed(() => buildBreadcrumb(props.location ?? route, pageTitle.title))

// Search: typing opens the search page of the library.
const { text: searchText, submit: submitSearch } = useSearchField()

// Sort options of the list of the current page; the choice is part of the query of the route.
const sortMenu = ref<InstanceType<typeof BaseContextMenu> | null>(null)
const sortList = computed(() => sortContext(props.location ?? route))
const hasSearch = computed(() => queryText(route.query) !== '')
const current = computed(() =>
    sortList.value
        ? effectiveSort(
              sortList.value,
              parseSort(sortList.value, route.query, hasSearch.value),
              hasSearch.value,
          )
        : null,
)
const sortItems = computed<MenuItem[]>(() => {
    const list = sortList.value
    if (!list) {
        return []
    }

    const keys = sortOptions(list, hasSearch.value)
    const directions: MenuItem[] = [
        {
            id: 'direction-asc',
            label: t('sort.ascending'),
            checked: current.value?.direction === 'Asc',
        },
        {
            id: 'direction-desc',
            label: t('sort.descending'),
            checked: current.value?.direction === 'Desc',
        },
    ]
    return keys.length === 0
        ? directions
        : [
              ...keys.map((key) => ({
                  id: sortItemId(key),
                  label: t(sortLabelKey(key)),
                  checked: current.value?.sort === key,
              })),
              { id: 'separator', separator: true },
              ...directions,
          ]
})

function openSort(event: MouseEvent): void {
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect()
    void sortMenu.value?.openAt(rect.left, rect.bottom + 4)
}

function chooseSort(id: string): void {
    const list = sortList.value
    if (!list) {
        return
    }

    const query = { ...route.query }
    if (id === 'direction-asc' || id === 'direction-desc') {
        query.direction = id === 'direction-asc' ? 'Asc' : 'Desc'
    } else {
        const key = sortOptions(list, hasSearch.value).find((option) => sortItemId(option) === id)
        if (!key) {
            return
        }

        // A new key starts in its own default direction.
        query.sort = key
        delete query.direction
    }

    void router.replace({ query })
}
</script>

<template>
    <div data-part="toolbar">
        <nav data-part="breadcrumb" :aria-label="t('breadcrumb.label')">
            <ol data-part="breadcrumb-list">
                <li
                    v-for="(crumb, index) in crumbs"
                    :key="crumb.id"
                    data-part="breadcrumb-item"
                    :data-state="crumb.to ? undefined : 'active'"
                >
                    <RouterLink v-if="crumb.to" v-slot="{ href, navigate }" :to="crumb.to" custom>
                        <a data-part="breadcrumb-link" :href="href" @click="navigate">
                            <BaseIcon v-if="index === 0" name="breadcrumb-root" />
                            <span data-part="label">{{ crumb.label ?? t(crumb.labelKey) }}</span>
                        </a>
                    </RouterLink>
                    <span v-else data-part="breadcrumb-link" aria-current="page">
                        <BaseIcon v-if="index === 0" name="breadcrumb-root" />
                        <span data-part="label">{{ crumb.label ?? t(crumb.labelKey) }}</span>
                    </span>
                    <span
                        v-if="index < crumbs.length - 1"
                        data-part="breadcrumb-separator"
                        aria-hidden="true"
                    >
                        <BaseIcon name="breadcrumb-separator" />
                    </span>
                </li>
            </ol>
        </nav>
        <!-- One view per list for now; tiles and details as options follow later. -->
        <BaseButton
            part="view-switch"
            variant="icon"
            icon="view-details"
            dropdown
            :label="t('toolbar.view')"
            aria-haspopup="menu"
            disabled
        />
        <BaseButton
            part="sort"
            variant="icon"
            icon="sort"
            dropdown
            :label="t('toolbar.sort')"
            aria-haspopup="menu"
            :disabled="!sortList"
            @click="openSort"
        />
        <BaseContextMenu
            ref="sortMenu"
            :items="sortItems"
            :label="t('sort.menu')"
            @select="chooseSort"
        />
        <BaseSearchField
            v-model="searchText"
            :label="t('toolbar.search')"
            :placeholder="t('toolbar.searchPlaceholder')"
            :clear-label="t('toolbar.clearSearch')"
            @keydown.enter="submitSearch()"
        />
    </div>
</template>
