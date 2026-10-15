<template>
  <div>
    <h1 class="text-h4 mb-1">Executive Dashboard</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Overview of security posture and critical findings
    </p>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <!-- KPI Cards -->
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

    <v-row>
      <!-- Top Security Issues -->
      <v-col cols="12" lg="8">
        <v-card>
          <v-card-title class="d-flex justify-space-between align-center">
            <span>Top Security Issues</span>
            <v-btn variant="text" color="primary" size="small">View all</v-btn>
          </v-card-title>

          <v-list>
            <v-list-item
              v-for="(issue, i) in dashboard?.topIssues || []"
              :key="i"
              class="py-3"
            >
              <template #prepend>
                <v-chip
                  :color="severityColor(issue.severity)"
                  size="small"
                  label
                  class="mr-3"
                >
                  {{ issue.severity }}
                </v-chip>
                <span class="text-body-2 text-medium-emphasis mr-3">{{ issue.count }} Issues</span>
              </template>

              <v-list-item-title>{{ issue.name }}</v-list-item-title>
              <v-list-item-subtitle>{{ issue.location }}</v-list-item-subtitle>

              <template #append>
                <v-chip
                  :color="issue.status === 'In Progress' ? 'info' : undefined"
                  size="small"
                 
                >
                  {{ issue.status }}
                </v-chip>
              </template>
            </v-list-item>
          </v-list>
        </v-card>
      </v-col>

      <!-- Recent Activity -->
      <v-col cols="12" lg="4">
        <v-card>
          <v-card-title>Recent Activity</v-card-title>

          <v-list>
            <v-list-item
              v-for="(activity, i) in dashboard?.recentActivity || []"
              :key="i"
              class="py-3"
            >
              <template #prepend>
                <v-icon :color="activity.color" size="20" class="mr-3">
                  {{ activityIcon(activity.icon) }}
                </v-icon>
              </template>

              <v-list-item-title class="text-body-2">{{ activity.title }}</v-list-item-title>
              <v-list-item-subtitle>
                {{ activity.subtitle }}
                <br />
                <span class="text-caption">{{ activity.time }}</span>
              </v-list-item-subtitle>
            </v-list-item>
          </v-list>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useDashboard } from '../../composables/useDashboard.js'
import KpiCard from '../../components/KpiCard.vue'

const { dashboard, loading, error, fetchDashboard } = useDashboard()

function severityColor(severity) {
  const colors = { Critical: 'error', High: 'warning', Medium: 'info', Low: 'success' }
  return colors[severity] || 'grey'
}

function activityIcon(icon) {
  const icons = { alert: 'mdi-alert-circle', check: 'mdi-check-circle', scan: 'mdi-radar' }
  return icons[icon] || 'mdi-information'
}

onMounted(() => fetchDashboard('executive'))
</script>
