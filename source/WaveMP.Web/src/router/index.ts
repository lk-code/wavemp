import { createRouter, createWebHistory } from 'vue-router'
import { createRoutes, devRoutes } from './routes'

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: createRoutes(devRoutes),
})

export default router
