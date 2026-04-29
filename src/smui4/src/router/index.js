import { createRouter, createWebHistory } from 'vue-router'
import DefaultLayout from '../layouts/DefaultLayout.vue'

const routes = [
  {
    path: '/',
    component: DefaultLayout,
    children: [
      { path: '', redirect: '/dashboards' },
      {
        path: 'dashboards',
        name: 'dashboards-overview',
        component: () => import('../views/dashboards/DashboardsOverview.vue'),
      },
      {
        path: 'dashboards/executive',
        name: 'dashboard-executive',
        component: () => import('../views/dashboards/ExecutiveView.vue'),
      },
      {
        path: 'dashboards/development',
        name: 'dashboard-development',
        component: () => import('../views/dashboards/DevelopmentView.vue'),
      },
      {
        path: 'dashboards/k-development',
        name: 'dashboard-k-development',
        component: () => import('../views/dashboards/KibanaDevelopmentView.vue'),
      },
      {
        path: 'dashboards/security',
        name: 'dashboard-security',
        component: () => import('../views/dashboards/SecurityView.vue'),
      },
      {
        path: 'dashboards/operations',
        name: 'dashboard-operations',
        component: () => import('../views/dashboards/OperationsView.vue'),
      },
      {
        path: 'integrations',
        name: 'integrations-overview',
        component: () => import('../views/integrations/OverviewView.vue'),
      },
      {
        path: 'integrations/available',
        name: 'integrations-available',
        component: () => import('../views/integrations/AvailableView.vue'),
      },
      {
        path: 'integrations/configured',
        name: 'integrations-configured',
        component: () => import('../views/integrations/ConfiguredView.vue'),
      },
      {
        path: 'integrations/configured/:instance',
        name: 'integrations-configured-detail',
        component: () => import('../views/integrations/ConfiguredView.vue'),
        props: true,
      },
      {
        path: 'scanning',
        name: 'scanning-overview',
        component: () => import('../views/scanning/ScanningOverview.vue'),
      },
      {
        path: 'scanning/jobs',
        name: 'scanning-jobs',
        component: () => import('../views/scanning/JobsView.vue'),
      },
      {
        path: 'scanning/schedule',
        name: 'scanning-schedule',
        component: () => import('../views/scanning/ScheduleView.vue'),
      },
      {
        path: 'scanning/scanners',
        name: 'scanning-scanners',
        component: () => import('../views/scanning/ScannersView.vue'),
      },
      {
        path: 'scanning/scanners/:scanner',
        name: 'scanning-scanner-detail',
        component: () => import('../views/scanning/ScannersView.vue'),
        props: true,
      },
      {
        path: 'inventory',
        name: 'inventory-assets',
        component: () => import('../views/inventory/InventoryAssetsView.vue'),
      },
      {
        path: 'inventory/create',
        name: 'inventory-asset-create',
        component: () => import('../views/inventory/InventoryAssetEditView.vue'),
      },
      {
        path: 'inventory/:id',
        name: 'inventory-asset-edit',
        component: () => import('../views/inventory/InventoryAssetEditView.vue'),
        props: true,
      },
      {
        path: 'settings',
        name: 'settings',
        component: () => import('../views/settings/GeneralView.vue'),
      },
      {
        path: 'settings/logs',
        name: 'settings-logs',
        component: () => import('../views/settings/LogsView.vue'),
      },
    ],
  },
]

const router = createRouter({
  history: createWebHistory('/smui4/'),
  routes,
})

export default router
