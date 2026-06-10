import { ref } from 'vue'
import legacyApi from '../services/legacyApi.js'
import apiClient from '../services/api.js'

export function useCustomJobs() {
  const jobs = ref([])
  const primer = ref(null)
  const scripts = ref([])
  const loading = ref(false)
  const error = ref(null)

  async function fetchJobs() {
    loading.value = true
    error.value = null
    try {
      const response = await legacyApi.post('/admin/servicejob/search', {
        searchFilters: [{ field: 'all', value: '' }],
        pager: { size: 100, page: 1 },
      })
      jobs.value = response.data || []
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function fetchPrimer() {
    loading.value = true
    error.value = null
    try {
      const response = await legacyApi.get('/admin/servicejob/primer')
      primer.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function fetchScripts() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/custom-jobs/scripts')
      scripts.value = response.data || []
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function saveJob(job) {
    loading.value = true
    error.value = null
    try {
      await legacyApi.post('/admin/servicejob', job)
      await fetchJobs()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function deleteJobs(ids) {
    loading.value = true
    error.value = null
    try {
      await legacyApi.post('/admin/servicejob/delete', { ids })
      await fetchJobs()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  return {
    jobs,
    primer,
    scripts,
    loading,
    error,
    fetchJobs,
    fetchPrimer,
    fetchScripts,
    saveJob,
    deleteJobs,
  }
}
