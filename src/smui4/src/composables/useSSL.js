import { ref } from 'vue'
import apiClient from '../services/api.js'

export function useSSL() {
  const certificate = ref(null)
  const loading = ref(false)
  const error = ref(null)

  async function fetchCertificate() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/ssl/certificate')
      certificate.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function uploadCertificate(certFile, keyFile) {
    loading.value = true
    error.value = null
    try {
      const formData = new FormData()
      formData.append('cert', certFile)
      formData.append('key', keyFile)
      const response = await apiClient.post('/ssl/certificate', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      await fetchCertificate()
      return response.data
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  return { certificate, loading, error, fetchCertificate, uploadCertificate }
}
