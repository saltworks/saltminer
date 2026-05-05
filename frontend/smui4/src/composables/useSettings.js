import { ref } from 'vue'
import apiClient from '../services/api.js'

export function useSettings() {
  const settings = ref([])
  const otherSettings = ref([])
  const pathsSettings = ref([])
  const loading = ref(false)
  const error = ref(null)

  async function fetchGeneralSettings() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/settings/general')
      settings.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function saveGeneralSettings(updates) {
    loading.value = true
    error.value = null
    try {
      await apiClient.put('/settings/general', updates)
      await fetchGeneralSettings()
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  function getSettingValue(propertyName) {
    const setting = settings.value.find((s) => s.property === propertyName)
    return setting?.value ?? null
  }

  async function fetchOtherSettings() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/settings/general/other')
      otherSettings.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function createOtherSetting(data) {
    loading.value = true
    error.value = null
    try {
      await apiClient.post('/settings/general/other', data)
      await fetchOtherSettings()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function updateOtherSetting(propertyName, data) {
    loading.value = true
    error.value = null
    try {
      await apiClient.put(`/settings/general/other/${encodeURIComponent(propertyName)}`, data)
      await fetchOtherSettings()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function deleteOtherSetting(propertyName) {
    loading.value = true
    error.value = null
    try {
      await apiClient.delete(`/settings/general/other/${encodeURIComponent(propertyName)}`)
      await fetchOtherSettings()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function fetchPathsSettings() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/settings/general/paths')
      pathsSettings.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function savePathsSettings(updates) {
    loading.value = true
    error.value = null
    try {
      await apiClient.put('/settings/general/paths', updates)
      await fetchPathsSettings()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  function getPathSettingValue(propertyName) {
    const setting = pathsSettings.value.find((s) => s.property === propertyName)
    return setting?.value ?? ''
  }

  return {
    settings,
    otherSettings,
    pathsSettings,
    loading,
    error,
    fetchGeneralSettings,
    saveGeneralSettings,
    getSettingValue,
    fetchOtherSettings,
    createOtherSetting,
    updateOtherSetting,
    deleteOtherSetting,
    fetchPathsSettings,
    savePathsSettings,
    getPathSettingValue,
  }
}
