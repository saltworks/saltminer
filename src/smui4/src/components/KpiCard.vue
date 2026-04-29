<template>
  <v-card class="pa-4" height="100%">
    <div class="d-flex justify-space-between align-start">
      <div>
        <p class="text-body-2 text-medium-emphasis mb-1">{{ label }}</p>
        <p class="text-h4 font-weight-bold">{{ value }}</p>
        <p class="text-caption text-medium-emphasis mt-1">{{ change }}</p>
      </div>
      <v-icon :color="color" size="32">{{ iconName }}</v-icon>
    </div>
  </v-card>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  label: { type: String, required: true },
  value: { type: [String, Number], required: true },
  change: { type: String, default: '' },
  icon: { type: String, default: 'findings' },
  color: { type: String, default: 'primary' },
})

const iconMap = {
  alert: 'mdi-alert-circle',
  scan: 'mdi-radar',
  resolved: 'mdi-check-circle',
  findings: 'mdi-chart-box',
}

const iconName = computed(() => iconMap[props.icon] || 'mdi-chart-box')
</script>
