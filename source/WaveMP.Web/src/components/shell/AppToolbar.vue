<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRoute } from 'vue-router'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import BaseSearchField from '@/components/base/BaseSearchField.vue'
import { buildBreadcrumb, type RouteInfo } from '@/navigation/breadcrumb'

const props = defineProps<{
    /** Location of the breadcrumb; by default the current route. */
    location?: RouteInfo
}>()

const { t } = useI18n()
const route = useRoute()

const crumbs = computed(() => buildBreadcrumb(props.location ?? route))

// The search needs the library API (step 6); until then the field only holds its text.
const query = ref('')
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
                            <span data-part="label">{{ t(crumb.labelKey) }}</span>
                        </a>
                    </RouterLink>
                    <span v-else data-part="breadcrumb-link" aria-current="page">
                        <BaseIcon v-if="index === 0" name="breadcrumb-root" />
                        <span data-part="label">{{ t(crumb.labelKey) }}</span>
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
        <!-- View and sort menus need the library API (step 6). -->
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
            disabled
        />
        <BaseSearchField
            v-model="query"
            :label="t('toolbar.search')"
            :placeholder="t('toolbar.searchPlaceholder')"
            :clear-label="t('toolbar.clearSearch')"
        />
    </div>
</template>
