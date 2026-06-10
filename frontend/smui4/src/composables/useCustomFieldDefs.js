import { ref } from 'vue'
import legacyApi from '../services/legacyApi.js'

export function useCustomFieldDefs() {
  const definitions = ref([])
  const loading = ref(false)
  const error = ref(null)

  async function fetchDefinitions() {
    loading.value = true
    error.value = null
    try {
      const response = await legacyApi.get('/admin/attributes/primer')
      definitions.value = response.data?.attributeDefinitions || []
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function saveDefinition(definition) {
    loading.value = true
    error.value = null
    try {
      await legacyApi.post('/admin/attributes', definition)
      await fetchDefinitions()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  return {
    definitions,
    loading,
    error,
    fetchDefinitions,
    saveDefinition,
  }
}
