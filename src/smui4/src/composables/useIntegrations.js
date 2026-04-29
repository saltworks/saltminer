import { ref } from 'vue'
import apiClient from '../services/api.js'

export function useIntegrations() {
  const configured = ref([])
  const available = ref([])
  const instanceDetail = ref(null)
  const loading = ref(false)
  const error = ref(null)

  async function fetchConfigured() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/integrations/configured')
      configured.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function fetchAvailable() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/integrations/available')
      available.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function fetchInstanceSettings(instance) {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get(`/integrations/configured/${encodeURIComponent(instance)}`)
      instanceDetail.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function saveInstanceSettings(instance, updates) {
    loading.value = true
    error.value = null
    try {
      await apiClient.put(`/integrations/configured/${encodeURIComponent(instance)}`, updates)
      await fetchInstanceSettings(instance)
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function deleteInstance(instance) {
    loading.value = true
    error.value = null
    try {
      await apiClient.delete(`/integrations/configured/${encodeURIComponent(instance)}`)
      await fetchConfigured()
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function createInstance(adapterName, instanceName) {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.post('/integrations/configured', {
        adapterName,
        instanceName,
      })
      return response.data
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function updateAdapterTemplate(adapter, data) {
    loading.value = true
    error.value = null
    try {
      await apiClient.put(`/integrations/available/${encodeURIComponent(adapter)}`, data)
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function createAdapter(data) {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.post('/integrations/available', data)
      await fetchAvailable()
      return response.data
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function deleteAdapterTemplate(adapter) {
    loading.value = true
    error.value = null
    try {
      await apiClient.delete(`/integrations/available/${encodeURIComponent(adapter)}`)
      await fetchAvailable()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function propagateTemplate(adapter, fields) {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.post(
        `/integrations/available/${encodeURIComponent(adapter)}/propagate`,
        { fields },
      )
      return response.data
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  return {
    configured,
    available,
    instanceDetail,
    loading,
    error,
    fetchConfigured,
    fetchAvailable,
    fetchInstanceSettings,
    saveInstanceSettings,
    deleteInstance,
    createInstance,
    updateAdapterTemplate,
    createAdapter,
    deleteAdapterTemplate,
    propagateTemplate,
  }
}
