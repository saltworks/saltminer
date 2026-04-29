<template>
  <div>
    <h1 class="text-h4 mb-1">Settings</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Manage your application configuration and preferences
    </p>

    <v-card>
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2">mdi-text-box-outline</v-icon>
        Application Logs
        <v-spacer />
        <v-btn
         
          size="small"
          prepend-icon="mdi-refresh"
          :loading="loading"
          @click="fetchLogs"
        >
          Refresh
        </v-btn>
      </v-card-title>

      <v-alert v-if="error" type="error" closable class="mx-4" @click:close="error = null">
        {{ error }}
      </v-alert>

      <v-data-table
        :headers="headers"
        :items="logs"
        :loading="loading"
        density="compact"
        class="text-body-2"
      >
        <template #item.level="{ item }">
          <v-chip
            :color="levelColor(item.level)"
            size="small"
            label
          >
            {{ item.level }}
          </v-chip>
        </template>
      </v-data-table>
    </v-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import apiClient from '../../services/api.js'

const logs = ref([])
const loading = ref(false)
const error = ref(null)

const headers = [
  { title: 'Timestamp', key: 'timestamp', width: '200px' },
  { title: 'Level', key: 'level', width: '120px' },
  { title: 'Message', key: 'message' },
]

function levelColor(level) {
  const colors = {
    ERROR: 'error',
    WARN: 'warning',
    INFO: 'info',
    DEBUG: 'grey',
  }
  return colors[level] || 'grey'
}

async function fetchLogs() {
  loading.value = true
  error.value = null
  try {
    const response = await apiClient.get('/settings/logs')
    logs.value = response
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

onMounted(fetchLogs)
</script>
