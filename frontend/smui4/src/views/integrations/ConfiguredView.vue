<template>
  <div>
    <v-btn
      variant="text"
      color="primary"
      prepend-icon="mdi-arrow-left"
      class="mb-4"
      :to="{ name: 'integrations-overview' }"
    >
      Back to Integrations
    </v-btn>

    <h1 class="text-h4 mb-1">Configured Integrations</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Configure and manage your configured security scanning integrations
    </p>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <!-- Detail mode: instance settings form -->
    <template v-if="instance">
      <v-card class="pa-6">
        <div class="d-flex justify-space-between align-start mb-6">
          <div class="d-flex align-center">
            <v-avatar size="56" rounded="lg" color="grey-lighten-4" class="mr-4">
              <img
                :src="instanceIcon"
                width="28"
                height="28"
                @error="$event.target.src = '/smui4/icons/integrations/default.svg'"
              />
            </v-avatar>
            <div>
              <h2 class="text-h5">{{ instance }}</h2>
              <div class="d-flex align-center">
                <v-icon
                  :color="form.enabled ? 'success' : 'grey'"
                  size="16"
                  class="mr-1"
                >
                  {{ form.enabled ? 'mdi-check-circle' : 'mdi-circle-outline' }}
                </v-icon>
                <span class="text-body-2" :class="form.enabled ? 'text-success' : 'text-medium-emphasis'">
                  {{ form.enabled ? 'Active' : 'Inactive' }}
                </span>
              </div>
            </div>
          </div>
          <div class="d-flex align-center">
            <span class="text-body-2 mr-2">Enabled</span>
            <v-switch
              v-model="form.enabled"
              color="primary"
              hide-details
              inset
            />
          </div>
        </div>

        <!-- Adapter Name (read-only) -->
        <v-text-field
          :model-value="form.adapterName"
          label="Adapter Name"
         
          class="mb-4"
          readonly
          bg-color="grey-lighten-4"
        />

        <!-- Dynamic fields from template -->
        <template v-for="field in adapterFields" :key="field.property">
          <v-switch
            v-if="field.value_type === 'boolean'"
            v-model="form.fields[field.property]"
            :label="field.label || field.property"
            :hint="field.description"
            :persistent-hint="!!field.description"
            color="primary"
            class="mb-4"
          />
          <v-text-field
            v-else
            v-model="form.fields[field.property]"
            :label="field.label || field.property"
            :hint="field.description"
            :persistent-hint="!!field.description"
            :type="isPasswordField(field.property) ? 'password' : (field.value_type === 'integer' ? 'number' : 'text')"
           
            class="mb-4"
          />
        </template>

        <v-divider class="mb-6" />

        <!-- Scan Schedule (always present) -->
        <div class="d-flex align-center mb-4">
          <v-icon class="mr-2">mdi-clock-outline</v-icon>
          <span class="text-h6">Scan Schedule</span>
        </div>

        <v-row class="mb-6">
          <v-col cols="12" md="6">
            <v-text-field
              v-model="form.runEveryHours"
              label="Run Every (Hours)"
             
              type="number"
              hint="Hours between scans (1-168)"
              persistent-hint
            />
          </v-col>
          <v-col cols="12" md="6">
            <v-text-field
              v-model="form.startingAt"
              label="Starting At (America/New_York)"
             
              hint="Initial scan start time"
              persistent-hint
            />
          </v-col>
        </v-row>

        <div class="d-flex gap-4">
          <v-btn
            color="primary"
            :loading="loading"
            @click="saveSettings"
          >
            Save Changes
          </v-btn>
          <v-btn
            color="error"
            variant="outlined"
           
            @click="confirmDelete = true"
          >
            Delete Integration
          </v-btn>
        </div>
      </v-card>

      <!-- Delete Confirmation -->
      <v-dialog v-model="confirmDelete" max-width="400">
        <v-card>
          <v-card-title>Delete {{ instance }}?</v-card-title>
          <v-card-text>
            This will remove all configuration for this integration. This action cannot be undone.
          </v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn variant="text" @click="confirmDelete = false">Cancel</v-btn>
            <v-btn color="error" :loading="loading" @click="handleDelete">Delete</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>
    </template>

    <!-- List mode: all configured instances -->
    <template v-else>
      <v-row>
        <v-col
          v-for="inst in configured"
          :key="inst.instance"
          cols="12"
          md="6"
          lg="4"
        >
          <v-card
           
            class="pa-6 cursor-pointer"
            @click="router.push({ name: 'integrations-configured-detail', params: { instance: inst.instance } })"
          >
            <div class="d-flex align-center mb-2">
              <v-avatar size="48" rounded="lg" color="grey-lighten-4" class="mr-3">
                <img
                  :src="getAdapterIcon(getProperty(inst, 'adapterName'))"
                  width="24"
                  height="24"
                  @error="$event.target.src = '/smui4/icons/integrations/default.svg'"
                />
              </v-avatar>
              <div>
                <h3 class="text-h6">{{ inst.instance }}</h3>
                <div class="d-flex align-center">
                  <v-icon
                    :color="getProperty(inst, 'enabled') === 'true' ? 'success' : 'grey'"
                    size="14"
                    class="mr-1"
                  >
                    {{ getProperty(inst, 'enabled') === 'true' ? 'mdi-check-circle' : 'mdi-circle-outline' }}
                  </v-icon>
                  <span class="text-caption">
                    {{ getProperty(inst, 'enabled') === 'true' ? 'Active' : 'Inactive' }}
                  </span>
                </div>
              </div>
            </div>
            <p class="text-body-2 text-medium-emphasis">{{ getProperty(inst, 'adapterName') }}</p>
          </v-card>
        </v-col>
        <v-col v-if="!loading && configured.length === 0" cols="12">
          <v-card class="pa-6 text-center text-medium-emphasis">
            No integrations configured yet.
            <router-link :to="{ name: 'integrations-available' }">Add one</router-link>.
          </v-card>
        </v-col>
      </v-row>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useIntegrations } from '../../composables/useIntegrations.js'

const SCHEDULE_PROPERTIES = new Set(['enabled', 'runEveryHours', 'startingAt', 'adapterName'])
const DEFAULT_ICON = '/smui4/icons/integrations/default.svg'

const props = defineProps({
  instance: { type: String, default: '' },
})

const router = useRouter()
const {
  configured,
  available,
  instanceDetail,
  loading,
  error,
  fetchConfigured,
  fetchAvailable,
  fetchInstanceSettings,
  saveInstanceSettings,
  deleteInstance,
} = useIntegrations()

const confirmDelete = ref(false)

const form = reactive({
  adapterName: '',
  enabled: false,
  runEveryHours: '24',
  startingAt: '',
  fields: {},
})

const adapterFields = computed(() => {
  if (!instanceDetail.value) return []
  return instanceDetail.value.properties.filter(
    (p) => !SCHEDULE_PROPERTIES.has(p.property)
  )
})

const instanceIcon = computed(() => {
  return getAdapterIcon(form.adapterName)
})

function getProperty(inst, prop) {
  const setting = inst.properties.find((p) => p.property === prop)
  return setting?.value ?? null
}

function getAdapterIcon(adapterName) {
  const adapter = available.value.find((a) => a.name === adapterName)
  return adapter?.icon || DEFAULT_ICON
}

function isPasswordField(propertyName) {
  const lower = propertyName.toLowerCase()
  return lower.includes('secret') || lower.includes('password') || lower.includes('token')
}

function populateForm(detail) {
  if (!detail) return
  const get = (prop, fallback = '') => {
    const setting = detail.properties.find((p) => p.property === prop)
    return setting?.value ?? fallback
  }
  form.adapterName = get('adapterName')
  form.enabled = get('enabled') === 'true'
  form.runEveryHours = get('runEveryHours', '24')
  form.startingAt = get('startingAt')

  // Populate dynamic fields
  const fields = {}
  for (const prop of detail.properties) {
    if (!SCHEDULE_PROPERTIES.has(prop.property)) {
      if (prop.value_type === 'boolean') {
        fields[prop.property] = prop.value === 'true'
      } else {
        fields[prop.property] = prop.value || ''
      }
    }
  }
  form.fields = fields
}

async function saveSettings() {
  const updates = []

  // Schedule + system fields
  updates.push({ property: 'adapterName', value: form.adapterName, value_type: 'string', label: 'Adapter Name' })
  updates.push({ property: 'enabled', value: String(form.enabled), value_type: 'boolean', label: 'Enabled' })
  updates.push({ property: 'runEveryHours', value: form.runEveryHours, value_type: 'integer', label: 'Run Every (Hours)' })
  updates.push({ property: 'startingAt', value: form.startingAt, value_type: 'string', label: 'Starting At' })

  // Dynamic fields
  for (const field of adapterFields.value) {
    const val = form.fields[field.property]
    updates.push({
      property: field.property,
      value: field.value_type === 'boolean' ? String(val) : (val || ''),
      value_type: field.value_type || 'string',
      label: field.label || field.property,
      description: field.description || '',
    })
  }

  await saveInstanceSettings(props.instance, updates)
}

async function handleDelete() {
  await deleteInstance(props.instance)
  confirmDelete.value = false
  router.push({ name: 'integrations-configured' })
}

watch(instanceDetail, (detail) => {
  if (detail) populateForm(detail)
})

watch(() => props.instance, async (newInstance) => {
  if (newInstance) {
    await Promise.all([fetchInstanceSettings(newInstance), fetchAvailable()])
  } else {
    await Promise.all([fetchConfigured(), fetchAvailable()])
  }
})

onMounted(async () => {
  if (props.instance) {
    await Promise.all([fetchInstanceSettings(props.instance), fetchAvailable()])
  } else {
    await Promise.all([fetchConfigured(), fetchAvailable()])
  }
})
</script>
