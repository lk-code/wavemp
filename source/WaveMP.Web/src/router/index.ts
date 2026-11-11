import { createRouter, createWebHistory } from 'vue-router'
import { resolveAccess } from './guards'
import { createRoutes, devRoutes } from './routes'
import { useSessionStore } from '@/stores/session'

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: createRoutes(devRoutes),
})

// Pinia is installed before the router, so the store is available for the first navigation.
router.beforeEach((to) => resolveAccess(to, useSessionStore()))

export default router
