<template>
  <div>
    <h1 class="text-h4 mb-1">Dashboards</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Security posture and operational dashboards
    </p>

    <v-row>
      <v-col cols="12" sm="6" lg="3">
        <v-card
         
          class="pa-6 cursor-pointer"
          @click="router.push({ name: 'dashboard-executive' })"
        >
          <v-icon color="error" size="32" class="mb-3">mdi-alert-circle</v-icon>
          <h3 class="text-h6 mb-1">Executive</h3>
          <p class="text-body-2 text-medium-emphasis">Security posture and critical findings</p>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" lg="3">
        <v-card
         
          class="pa-6 cursor-pointer"
          @click="router.push({ name: 'dashboard-development' })"
        >
          <v-icon color="warning" size="32" class="mb-3">mdi-code-braces</v-icon>
          <h3 class="text-h6 mb-1">Development</h3>
          <p class="text-body-2 text-medium-emphasis">SAST, DAST, and OSS security metrics</p>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" lg="3">
        <v-card
         
          class="pa-6 cursor-pointer"
          @click="router.push({ name: 'dashboard-security' })"
        >
          <v-icon color="primary" size="32" class="mb-3">mdi-shield-check</v-icon>
          <h3 class="text-h6 mb-1">Security</h3>
          <p class="text-body-2 text-medium-emphasis">Vulnerability metrics and compliance</p>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" lg="3">
        <v-card
         
          class="pa-6 cursor-pointer"
          @click="router.push({ name: 'dashboard-operations' })"
        >
          <v-icon color="info" size="32" class="mb-3">mdi-cog</v-icon>
          <h3 class="text-h6 mb-1">Operations</h3>
          <p class="text-body-2 text-medium-emphasis">Scanner health and scan activity</p>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
const router = useRouter()
</script>
