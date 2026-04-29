<template>
  <div>
    <h1 class="text-h4 mb-1">Development Dashboard</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Development security metrics and code analysis findings
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
      <v-icon size="48" class="mb-2">mdi-chart-line</v-icon>
      <p>Development trend charts and detailed breakdowns coming soon.</p>
    </v-card>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useDashboard } from '../../composables/useDashboard.js'
import KpiCard from '../../components/KpiCard.vue'

const { dashboard, loading, error, fetchDashboard } = useDashboard()
onMounted(() => fetchDashboard('development'))
</script>
