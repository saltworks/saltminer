<template>
  <div>
    <h1 class="text-h4 mb-1">Scanning</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Manage security scanning operations
    </p>

    <v-row>
      <v-col cols="12" md="4">
        <v-card
         
          class="pa-6 cursor-pointer"
          @click="router.push({ name: 'scanning-jobs' })"
        >
          <v-icon color="info" size="32" class="mb-3">mdi-briefcase-outline</v-icon>
          <h3 class="text-h6 mb-1">Jobs</h3>
          <p class="text-body-2 text-medium-emphasis">Monitor and manage scanning jobs and their status</p>
        </v-card>
      </v-col>
      <v-col cols="12" md="4">
        <v-card
         
          class="pa-6 cursor-pointer"
          @click="router.push({ name: 'scanning-schedule' })"
        >
          <v-icon color="primary" size="32" class="mb-3">mdi-calendar-clock</v-icon>
          <h3 class="text-h6 mb-1">Schedule</h3>
          <p class="text-body-2 text-medium-emphasis">Configure automated scanning schedules</p>
        </v-card>
      </v-col>
      <v-col cols="12" md="4">
        <v-card
         
          class="pa-6 cursor-pointer"
          @click="router.push({ name: 'scanning-scanners' })"
        >
          <v-icon color="success" size="32" class="mb-3">mdi-radar</v-icon>
          <h3 class="text-h6 mb-1">Scanners</h3>
          <p class="text-body-2 text-medium-emphasis">Configure and manage security scanners</p>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
const router = useRouter()
</script>
