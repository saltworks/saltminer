<template>
  <div>
    <h1 class="text-h4 mb-1">Scanning Jobs</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Manage and monitor your security scanning jobs
    </p>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <v-card>
      <v-data-table
        :headers="headers"
        :items="jobs"
        :loading="loading"
        hover
      >
        <template #item.name="{ item }">
          <span class="text-primary font-weight-medium">{{ item.name }}</span>
        </template>
        <template #item.nextRun="{ item }">
          <div>
            <div>{{ item.nextRun }}</div>
            <div v-if="item.timezone" class="text-caption text-medium-emphasis">{{ item.timezone }}</div>
          </div>
        </template>
        <template #item.lastRun="{ item }">
          <div>
            <div>{{ item.lastRun }}</div>
            <div v-if="item.timezone" class="text-caption text-medium-emphasis">{{ item.timezone }}</div>
          </div>
        </template>
        <template #item.status="{ item }">
          <v-chip
            :color="statusColor(item.status)"
            size="small"
            label
          >
            <v-icon start size="14">{{ statusIcon(item.status) }}</v-icon>
            {{ item.status }}
          </v-chip>
        </template>
      </v-data-table>
    </v-card>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useScanning } from '../../composables/useScanning.js'

const { jobs, loading, error, fetchJobs } = useScanning()

const headers = [
  { title: 'Job Name', key: 'name' },
  { title: 'Scanner', key: 'scanner' },
  { title: 'Next Run', key: 'nextRun', width: '180px' },
  { title: 'Last Run', key: 'lastRun', width: '180px' },
  { title: 'Last Run Status', key: 'status', width: '150px' },
]

function statusColor(status) {
  const colors = {
    Success: 'success',
    Warning: 'warning',
    Failed: 'error',
    Running: 'info',
  }
  return colors[status] || 'grey'
}

function statusIcon(status) {
  const icons = {
    Success: 'mdi-check-circle',
    Warning: 'mdi-alert',
    Failed: 'mdi-close-circle',
    Running: 'mdi-progress-clock',
  }
  return icons[status] || 'mdi-circle'
}

onMounted(fetchJobs)
</script>
