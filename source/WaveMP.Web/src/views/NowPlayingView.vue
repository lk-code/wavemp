<script setup lang="ts">
import NowPlayingStage from '@/components/player/NowPlayingStage.vue'
import PlayQueue from '@/components/player/PlayQueue.vue'
import { usePlayerStore } from '@/stores/player'

/** Now Playing: the stage with the current track (cover or video) and the queue of the player. */
const player = usePlayerStore()
</script>

<template>
    <div data-part="view" data-view="now-playing">
        <NowPlayingStage
            :track="player.current?.track"
            :video-mode="player.viewMode === 'Video'"
            @toggle-video="player.selectViewMode(player.viewMode === 'Video' ? 'Cover' : 'Video')"
            @attach-video="player.attachVideo"
            @detach-video="player.detachVideo"
        />
        <PlayQueue
            v-if="player.queue.items.length > 0"
            :items="player.queue.items"
            :current-index="player.queue.index"
            @play="player.playItem"
            @move="player.moveItem"
            @remove="player.removeItem"
            @clear="player.clearQueue"
        />
    </div>
</template>
