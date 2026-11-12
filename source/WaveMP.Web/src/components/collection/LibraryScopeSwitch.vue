<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import BaseButton from '@/components/base/BaseButton.vue'
import { isMyMusic, withMyMusic } from '@/collection/presentation'

/**
 * Switch between the whole library and the "my music" of the profile (rated and favorite songs,
 * the songs of favorite albums and artists and of the own playlists). The choice is part of the
 * query of the route (?myMusic=true), so it combines with search and sort order and can be linked.
 */
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const myMusic = computed(() => isMyMusic(route.query))

function choose(value: boolean): void {
    if (value !== myMusic.value) {
        void router.replace({ query: withMyMusic(route.query, value) })
    }
}
</script>

<template>
    <div data-part="scope-switch" role="group" :aria-label="t('collection.scope.label')">
        <BaseButton
            data-item="all-music"
            :label="t('collection.scope.all')"
            :pressed="!myMusic"
            :state="myMusic ? undefined : 'active'"
            @click="choose(false)"
        />
        <BaseButton
            data-item="my-music"
            :label="t('collection.scope.myMusic')"
            :pressed="myMusic"
            :state="myMusic ? 'active' : undefined"
            @click="choose(true)"
        />
    </div>
</template>
