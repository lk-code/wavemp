<script setup lang="ts">
import ProfileAvatar from './ProfileAvatar.vue'

/** Avatar and name of a profile, e.g. of the current profile on the settings page. */
defineProps<{
    name: string
    initials: string
    color: string
}>()
</script>

<template>
    <div data-part="profile-summary">
        <ProfileAvatar :initials="initials" :color="color" />
        <span data-part="label">{{ name }}</span>
    </div>
</template>
