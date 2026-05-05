import { ref } from 'vue'
import apiClient from '../services/api.js'

export function useDashboard() {
  const dashboard = ref(null)
  const loading = ref(false)
  const error = ref(null)

  async function fetchDashboard(type) {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get(`/dashboards/${type}`)
      dashboard.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  return { dashboard, loading, error, fetchDashboard }
}
