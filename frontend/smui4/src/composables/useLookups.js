import { ref } from 'vue'
import legacyApi from '../services/legacyApi.js'

export function useLookups() {
  const lookups = ref([])
  const loading = ref(false)
  const error = ref(null)

  async function fetchLookups() {
    loading.value = true
    error.value = null
    try {
      const response = await legacyApi.post('/admin/lookups/search', {})
      lookups.value = response.data || []
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function saveLookup(lookup) {
    loading.value = true
    error.value = null
    try {
      const payload = { ...lookup, timestamp: new Date().toISOString() }
      await legacyApi.post('/admin/lookups', payload)
      await fetchLookups()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  return {
    lookups,
    loading,
    error,
    fetchLookups,
    saveLookup,
  }
}
