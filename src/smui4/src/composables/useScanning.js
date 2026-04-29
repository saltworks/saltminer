import { ref } from 'vue'
import apiClient from '../services/api.js'

export function useScanning() {
  const jobs = ref([])
  const scanners = ref([])
  const schedules = ref([])
  const scannerDetail = ref(null)
  const loading = ref(false)
  const error = ref(null)

  async function fetchJobs() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/scanning/jobs')
      jobs.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function fetchScanners() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/scanning/scanners')
      scanners.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function fetchScannerSettings(scanner) {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get(`/scanning/scanners/${scanner}`)
      scannerDetail.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function saveScannerSettings(scanner, updates) {
    loading.value = true
    error.value = null
    try {
      await apiClient.put(`/scanning/scanners/${scanner}`, updates)
      await fetchScannerSettings(scanner)
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function deleteScanner(scanner) {
    loading.value = true
    error.value = null
    try {
      await apiClient.delete(`/scanning/scanners/${scanner}`)
      await fetchScanners()
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function fetchSchedules() {
    loading.value = true
    error.value = null
    try {
      const response = await apiClient.get('/scanning/schedule')
      schedules.value = response.data
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  return {
    jobs,
    scanners,
    schedules,
    scannerDetail,
    loading,
    error,
    fetchJobs,
    fetchScanners,
    fetchScannerSettings,
    saveScannerSettings,
    deleteScanner,
    fetchSchedules,
  }
}
