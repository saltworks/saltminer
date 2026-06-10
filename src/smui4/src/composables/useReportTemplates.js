import { ref } from 'vue'
import apiClient from '../services/api.js'

export function useReportTemplates() {
  const templates = ref([])
  const loading = ref(false)
  const error = ref(null)

  async function fetchTemplates() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/report-templates')
      templates.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function uploadTemplate(file) {
    loading.value = true
    error.value = null
    try {
      const formData = new FormData()
      formData.append('file', file)
      await apiClient.post('/report-templates', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      await fetchTemplates()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function deleteTemplate(filename) {
    loading.value = true
    error.value = null
    try {
      await apiClient.delete(`/report-templates/${encodeURIComponent(filename)}`)
      await fetchTemplates()
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  return {
    templates,
    loading,
    error,
    fetchTemplates,
    uploadTemplate,
    deleteTemplate,
  }
}
