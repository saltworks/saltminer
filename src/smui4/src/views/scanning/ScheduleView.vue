<template>
  <div>
    <h1 class="text-h4 mb-1">Scan Schedule</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Configure and manage automated scanning schedules
    </p>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <div v-for="schedule in schedules" :key="schedule.name" class="mb-6">
      <v-card class="pa-6">
        <div class="d-flex justify-space-between align-center mb-4">
          <div>
            <h3 class="text-h6">{{ schedule.name }}</h3>
            <p class="text-body-2 text-medium-emphasis">Scanner: {{ schedule.scanner }}</p>
          </div>
          <v-chip
            v-if="schedule.status"
            :color="statusColor(schedule.status)"
            size="small"
            label
          >
            <v-icon start size="14">{{ statusIcon(schedule.status) }}</v-icon>
            {{ schedule.status }}
          </v-chip>
        </div>

        <v-row class="mb-4">
          <v-col cols="12" md="4">
            <v-select
              :model-value="schedule.frequency"
              :items="['Daily', 'Weekly', 'Bi-weekly', 'Monthly']"
              label="Frequency"
             
              prepend-inner-icon="mdi-clock-outline"
              readonly
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-text-field
              :model-value="schedule.nextRunDate"
              label="Next Run Date"
             
              prepend-inner-icon="mdi-calendar"
              readonly
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-text-field
              :model-value="schedule.startTime"
              label="Start Time (America/New_York)"
             
              prepend-inner-icon="mdi-clock-outline"
              readonly
            />
          </v-col>
        </v-row>

        <v-alert
          v-if="schedule.frequency && schedule.nextRunDate"
          type="info"
          variant="tonal"
          density="compact"
          class="mb-4"
        >
          Schedule: {{ schedule.frequency }} starting on
          <strong>{{ schedule.nextRunDate }}</strong> at {{ schedule.startTime || '00:00' }}
          <span v-if="schedule.timezone">({{ schedule.timezone }})</span>
        </v-alert>

        <div class="d-flex justify-end">
          <v-btn color="primary" prepend-icon="mdi-content-save">
            Save Schedule
          </v-btn>
        </div>
      </v-card>
    </div>

    <v-card v-if="!loading && schedules.length === 0" class="pa-6 text-center text-medium-emphasis">
      No scan schedules configured yet.
    </v-card>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useScanning } from '../../composables/useScanning.js'

const { schedules, loading, error, fetchSchedules } = useScanning()

function statusColor(status) {
  const colors = { Success: 'success', Warning: 'warning', Failed: 'error', Running: 'info' }
  return colors[status] || 'grey'
}

function statusIcon(status) {
  const icons = { Success: 'mdi-check-circle', Warning: 'mdi-alert', Failed: 'mdi-close-circle', Running: 'mdi-progress-clock' }
  return icons[status] || 'mdi-circle'
}

onMounted(fetchSchedules)
</script>
