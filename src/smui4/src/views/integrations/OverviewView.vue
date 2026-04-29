<template>
  <div>
    <h1 class="text-h4 mb-1">Integrations</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Manage integrations with security scanning products
    </p>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <!-- Configured Integrations -->
    <div class="d-flex justify-space-between align-center mb-4">
      <h2 class="text-h5">Configured Integrations</h2>
      <v-btn
        variant="text"
        color="primary"
        append-icon="mdi-arrow-right"
        :to="{ name: 'integrations-configured' }"
      >
        View All
      </v-btn>
    </div>

    <v-row class="mb-8">
      <v-col
        v-for="inst in configured"
        :key="inst.instance"
        cols="12"
        md="6"
        lg="4"
      >
        <integration-card
          :name="inst.instance"
          :description="getProperty(inst, 'adapterName') || ''"
          :icon="getAdapterIcon(getProperty(inst, 'adapterName'))"
          :subtitle="getScheduleLabel(inst)"
        >
          <template #top-right>
            <v-icon
              :color="getProperty(inst, 'enabled') === 'true' ? 'success' : 'grey'"
              size="20"
            >
              {{ getProperty(inst, 'enabled') === 'true' ? 'mdi-check-circle' : 'mdi-circle-outline' }}
            </v-icon>
          </template>
        </integration-card>
      </v-col>
      <v-col v-if="!loading && configured.length === 0" cols="12">
        <v-card class="pa-6 text-center text-medium-emphasis">
          No integrations configured yet.
          <router-link :to="{ name: 'integrations-available' }">Add one</router-link>.
        </v-card>
      </v-col>
    </v-row>

    <!-- Available Integration Types -->
    <div class="d-flex justify-space-between align-center mb-4">
      <h2 class="text-h5">Available Integration Types</h2>
      <v-btn
        variant="text"
        color="primary"
        append-icon="mdi-arrow-right"
        :to="{ name: 'integrations-available' }"
      >
        View All
      </v-btn>
    </div>

    <v-row>
      <v-col
        v-for="adapter in available"
        :key="adapter.name"
        cols="12"
        md="6"
        lg="4"
      >
        <integration-card
          :name="adapter.name"
          :description="adapter.description"
          :icon="adapter.icon"
        >
          <template #top-right>
            <v-icon size="20" color="grey">mdi-plus</v-icon>
          </template>
        </integration-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useIntegrations } from '../../composables/useIntegrations.js'
import IntegrationCard from '../../components/IntegrationCard.vue'

const {
  configured,
  available,
  loading,
  error,
  fetchConfigured,
  fetchAvailable,
} = useIntegrations()

function getProperty(inst, prop) {
  const setting = inst.properties.find((p) => p.property === prop)
  return setting?.value ?? null
}

function getScheduleLabel(inst) {
  const hours = getProperty(inst, 'runEveryHours')
  if (hours) return `Every ${hours}h`
  return ''
}

function getAdapterIcon(adapterName) {
  const adapter = available.value.find((a) => a.name === adapterName)
  return adapter?.icon || '/smui4/icons/integrations/default.svg'
}

onMounted(async () => {
  await Promise.all([fetchConfigured(), fetchAvailable()])
})
</script>
