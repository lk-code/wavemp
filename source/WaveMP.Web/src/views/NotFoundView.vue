<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'

const { t } = useI18n()
</script>

<template>
    <div data-part="view" data-view="not-found">
        <div data-part="empty-state">
            <p data-part="empty-state-text">{{ t('notFound.text') }}</p>
            <RouterLink v-slot="{ href, navigate }" :to="{ name: 'library' }" custom>
                <a data-part="link" :href="href" @click="navigate">{{ t('notFound.toLibrary') }}</a>
            </RouterLink>
        </div>
    </div>
</template>
