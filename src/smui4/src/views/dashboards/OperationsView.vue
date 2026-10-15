<template>
  <div>
    <h1 class="text-h4 mb-1">Operations Dashboard</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Scanner health, scan activity, and operational metrics
    </p>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <v-row class="mb-6">
      <v-col
        v-for="kpi in dashboard?.kpis || []"
        :key="kpi.label"
        cols="12"
        sm="6"
        lg="3"
      >
        <kpi-card v-bind="kpi" />
      </v-col>
    </v-row>

    <v-card class="pa-6 text-center text-medium-emphasis">
      <v-icon size="48" class="mb-2">mdi-cog</v-icon>
      <p>Operational trend charts and scanner status breakdowns coming soon.</p>
    </v-card>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useDashboard } from '../../composables/useDashboard.js'
import KpiCard from '../../components/KpiCard.vue'

const { dashboard, loading, error, fetchDashboard } = useDashboard()
onMounted(() => fetchDashboard('operations'))
</script>
