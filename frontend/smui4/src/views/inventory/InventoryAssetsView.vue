<template>
  <div>
    <h1 class="text-h4 mb-1">Inventory Assets</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Applications, systems, and other inventory tracked by SaltMiner
    </p>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <v-card class="pa-4">
      <div class="d-flex flex-wrap align-center ga-3 mb-4">
        <v-btn
          color="primary"
          prepend-icon="mdi-plus"
          @click="router.push({ name: 'inventory-asset-create' })"
        >
          Add
        </v-btn>

        <v-btn
          color="error"
          variant="outlined"
          prepend-icon="mdi-delete"
          :disabled="selectedIds.length === 0"
          @click="confirmDelete"
        >
          Delete ({{ selectedIds.length }})
        </v-btn>

        <v-spacer />

        <v-select
          v-model="searchField"
          :items="searchFieldItems"
          label="Filter"
          density="compact"
          hide-details
          style="max-width: 200px;"
        />
        <v-text-field
          v-model="searchQuery"
          label="Search"
          density="compact"
          hide-details
          style="max-width: 300px;"
          prepend-inner-icon="mdi-magnify"
          clearable
          @keyup.enter="doSearch"
          @click:clear="onClearSearch"
        />
        <v-btn color="primary" variant="tonal" @click="doSearch">Search</v-btn>
      </div>

      <v-data-table-server
        v-model="selectedIds"
        :headers="headers"
        :items="tableRows"
        :items-length="total"
        :loading="loading"
        :page="page"
        :items-per-page="pageSize"
        show-select
        item-value="id"
        hover
        density="compact"
        @update:options="onTableUpdate"
        @click:row="onRowClick"
      />
    </v-card>

    <!-- Delete Confirmation -->
    <v-dialog v-model="showDeleteDialog" max-width="460">
      <v-card>
        <v-card-title>Delete Inventory Asset(s)?</v-card-title>
        <v-card-text>
          You are about to permanently delete {{ selectedIds.length }}
          {{ selectedIds.length === 1 ? 'asset' : 'assets' }}. This cannot be undone.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn color="error" :loading="loading" @click="handleDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useInventoryAssets } from '../../composables/useInventoryAssets.js'

const router = useRouter()
const {
  assets,
  total,
  searchFilters,
  loading,
  error,
  fetchPrimer,
  searchAssets,
  deleteAssets,
} = useInventoryAssets()

const headers = [
  { title: 'Key', key: 'key', sortable: true },
  { title: 'Name', key: 'name', sortable: true },
]

const searchField = ref('all')
const searchQuery = ref('')
const page = ref(1)
const pageSize = ref(10)
const selectedIds = ref([])
const sortFilters = ref({})
const showDeleteDialog = ref(false)

const searchFieldItems = computed(() =>
  searchFilters.value.map((f) => ({ title: f.value, value: f.field.toLowerCase() })),
)

// Each row has fields like {id, key: {value: "..."}, name: {value: "..."}}
// Flatten the value objects so the data table can display them directly
const tableRows = computed(() =>
  assets.value.map((a) => ({
    id: a.id,
    key: a.key?.value ?? a.key ?? '',
    name: a.name?.value ?? a.name ?? '',
  })),
)

function onTableUpdate({ page: newPage, itemsPerPage, sortBy }) {
  page.value = newPage
  pageSize.value = itemsPerPage
  sortFilters.value = {}
  if (Array.isArray(sortBy)) {
    sortBy.forEach(({ key, order }) => {
      const field = key === 'timestamp' ? 'date' : key
      sortFilters.value[field] = order !== 'desc'
    })
  }
  runSearch()
}

function doSearch() {
  page.value = 1
  runSearch()
}

function onClearSearch() {
  searchQuery.value = ''
  doSearch()
}

function runSearch() {
  searchAssets({
    field: searchField.value,
    value: searchQuery.value || '',
    page: page.value,
    size: pageSize.value,
    sortFilters: sortFilters.value,
  })
}

function onRowClick(_event, { item }) {
  if (!item?.id) return
  router.push({ name: 'inventory-asset-edit', params: { id: item.id } })
}

function confirmDelete() {
  if (selectedIds.value.length === 0) return
  showDeleteDialog.value = true
}

async function handleDelete() {
  try {
    await deleteAssets([...selectedIds.value])
    selectedIds.value = []
    showDeleteDialog.value = false
    runSearch()
  } catch {
    showDeleteDialog.value = false
  }
}

onMounted(async () => {
  await fetchPrimer()
  runSearch()
})
</script>
