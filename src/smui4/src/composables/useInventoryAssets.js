import { ref } from 'vue'
import legacyApi from '../services/legacyApi.js'

export function useInventoryAssets() {
  const assets = ref([])
  const total = ref(0)
  const searchFilters = ref([])
  const assetDetail = ref(null)
  const attributeDefinitions = ref([])
  const loading = ref(false)
  const error = ref(null)

  async function fetchPrimer() {
    loading.value = true
    error.value = null
    try {
      const response = await legacyApi.get('/InventoryAsset/primer')
      searchFilters.value = response.data?.searchFilters || []
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function searchAssets({ field = 'all', value = '', page = 1, size = 10, sortFilters = {} } = {}) {
    loading.value = true
    error.value = null
    try {
      const body = {
        searchFilters: [{ field: field.toLowerCase(), value }],
        pager: { page, size, sortFilters },
      }
      const response = await legacyApi.post('/InventoryAsset/search', body)
      assets.value = response.data || []
      total.value = response.pager?.total || 0
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function fetchAssetForEdit(id) {
    loading.value = true
    error.value = null
    try {
      const response = await legacyApi.get(`/InventoryAsset/${encodeURIComponent(id)}/edit/primer`)
      assetDetail.value = response.data?.inventoryAsset || null
      attributeDefinitions.value = response.data?.attributeDefinitions || []
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function fetchCreatePrimer() {
    loading.value = true
    error.value = null
    try {
      // The main primer returns the empty asset template and attribute defs for new assets
      const response = await legacyApi.get('/InventoryAsset/primer')
      assetDetail.value = response.data?.inventoryAsset || null
      attributeDefinitions.value = response.data?.attributeDefinitions || []
    } catch (e) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function saveAsset(asset) {
    loading.value = true
    error.value = null
    try {
      await legacyApi.post('/InventoryAsset', asset)
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function deleteAssets(ids) {
    loading.value = true
    error.value = null
    try {
      await legacyApi.post('/InventoryAsset/delete', { ids })
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  return {
    assets,
    total,
    searchFilters,
    assetDetail,
    attributeDefinitions,
    loading,
    error,
    fetchPrimer,
    searchAssets,
    fetchAssetForEdit,
    fetchCreatePrimer,
    saveAsset,
    deleteAssets,
  }
}
