<template>
  <div>
    <h1 class="text-h4 mb-1">Settings</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Manage your application configuration and preferences
    </p>

    <v-tabs v-model="activeTab" color="primary" class="mb-6">
      <v-tab value="general">General</v-tab>
      <v-tab value="security">Security</v-tab>
      <v-tab value="notifications">Notifications</v-tab>
      <v-tab value="users">Users</v-tab>
      <v-tab value="integrations">Integrations</v-tab>
      <v-tab value="reports">Reports</v-tab>
      <v-tab value="saltminer-jobs">SaltMiner Jobs</v-tab>
      <v-tab value="custom-jobs">Custom Jobs</v-tab>
      <v-tab value="definitions">Definitions</v-tab>
    </v-tabs>

    <v-window v-model="activeTab">
      <v-window-item value="general">
        <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
          {{ error }}
        </v-alert>

        <!-- Organization Settings -->
        <v-card class="pa-6 mb-6">
          <div class="d-flex align-center mb-6">
            <v-icon color="primary" class="mr-2">mdi-domain</v-icon>
            <span class="text-h6">Organization</span>
          </div>

          <v-text-field
            v-model="form.orgName"
            label="Organization Name"
           
            class="mb-4"
            :loading="loading"
          />

          <v-text-field
            v-model="form.primaryDomain"
            label="Primary Domain"
           
            class="mb-4"
            hint="The URL users will use to access SaltMiner"
            persistent-hint
            :loading="loading"
          />

          <v-btn
            color="primary"
            :loading="loading"
            @click="saveOrgSettings"
          >
            Save Changes
          </v-btn>
        </v-card>

        <!-- Other Settings -->
        <v-card class="pa-6">
          <div class="d-flex justify-space-between align-center mb-4">
            <div class="d-flex align-center">
              <v-icon color="primary" class="mr-2">mdi-cog</v-icon>
              <span class="text-h6">Additional Settings</span>
            </div>
            <v-btn color="primary" prepend-icon="mdi-plus" size="small" @click="openAddDialog">
              Add Setting
            </v-btn>
          </div>

          <v-data-table
            v-if="otherSettings.length > 0"
            :headers="otherHeaders"
            :items="otherSettings"
            density="compact"
            hover
          >
            <template #item.value="{ item }">
              <span class="text-body-2">{{ item.value }}</span>
            </template>
            <template #item.value_type="{ item }">
              <v-chip size="x-small" variant="tonal">{{ item.value_type }}</v-chip>
            </template>
            <template #item.actions="{ item }">
              <v-btn icon="mdi-pencil" size="x-small" variant="text" @click="openEditDialog(item)" />
              <v-btn icon="mdi-delete" size="x-small" variant="text" color="error" @click="confirmDelete(item.property)" />
            </template>
          </v-data-table>

          <div v-else class="text-center text-medium-emphasis pa-4">
            No additional settings configured.
          </div>
        </v-card>
      </v-window-item>

      <!-- Placeholder tabs -->
      <v-window-item value="security">
        <SSLCertificateManager ref="sslManager" />
      </v-window-item>
      <v-window-item value="notifications">
        <v-card class="pa-6">
          <p class="text-body-1 text-medium-emphasis">Notification settings coming soon.</p>
        </v-card>
      </v-window-item>
      <v-window-item value="users">
        <v-card class="pa-6">
          <p class="text-body-1 text-medium-emphasis">User management coming soon.</p>
        </v-card>
      </v-window-item>
      <v-window-item value="integrations">
        <integration-template-editor ref="integrationEditor" />
      </v-window-item>
      <v-window-item value="reports">
        <report-templates-editor ref="reportTemplatesEditor" />
      </v-window-item>
      <v-window-item value="saltminer-jobs">
        <saltminer-jobs-editor ref="saltminerJobsEditor" />
      </v-window-item>
      <v-window-item value="custom-jobs">
        <custom-jobs-editor ref="customJobsEditor" />
      </v-window-item>
      <v-window-item value="definitions">
        <DefinitionsTab ref="definitionsTab" />
      </v-window-item>
    </v-window>

    <!-- Add/Edit Setting Dialog -->
    <v-dialog v-model="showSettingDialog" max-width="500">
      <v-card>
        <v-card-title>{{ editingProperty ? 'Edit Setting' : 'Add Setting' }}</v-card-title>
        <v-card-text>
          <v-text-field
            v-model="settingForm.property"
            label="Property Name"
           
            class="mb-4"
            :readonly="!!editingProperty"
            :bg-color="editingProperty ? 'grey-lighten-4' : undefined"
          />
          <v-text-field
            v-model="settingForm.label"
            label="Label"
           
            class="mb-4"
          />
          <v-select
            v-model="settingForm.value_type"
            :items="['string', 'boolean', 'integer', 'date']"
            label="Value Type"
           
            class="mb-4"
            @update:model-value="onValueTypeChange"
          />
          <!-- String -->
          <v-text-field
            v-if="settingForm.value_type === 'string'"
            v-model="settingForm.value"
            label="Value"
           
            class="mb-4"
          />
          <!-- Integer -->
          <v-text-field
            v-else-if="settingForm.value_type === 'integer'"
            v-model="settingForm.value"
            label="Value"
           
            type="number"
            class="mb-4"
          />
          <!-- Boolean -->
          <v-switch
            v-else-if="settingForm.value_type === 'boolean'"
            v-model="settingForm.boolValue"
            :label="settingForm.boolValue ? 'true' : 'false'"
            color="primary"
            hide-details
            class="mb-4"
          />
          <!-- Date (UTC) -->
          <v-text-field
            v-else-if="settingForm.value_type === 'date'"
            v-model="settingForm.value"
            label="Value (UTC)"
           
            type="datetime-local"
            hint="Stored as UTC string"
            persistent-hint
            class="mb-4"
          />
          <v-text-field
            v-model="settingForm.description"
            label="Description"
           
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showSettingDialog = false">Cancel</v-btn>
          <v-btn
            color="primary"
            :loading="loading"
            :disabled="!settingForm.property.trim()"
            @click="handleSaveSetting"
          >
            {{ editingProperty ? 'Save' : 'Add' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation -->
    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card>
        <v-card-title>Delete "{{ deleteTarget }}"?</v-card-title>
        <v-card-text>This setting will be permanently removed.</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn color="error" :loading="loading" @click="handleDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, watch } from 'vue'
import { useSettings } from '../../composables/useSettings.js'
import IntegrationTemplateEditor from '../../components/IntegrationTemplateEditor.vue'
import CustomJobsEditor from '../../components/CustomJobsEditor.vue'
import SaltminerJobsEditor from '../../components/SaltminerJobsEditor.vue'
import ReportTemplatesEditor from '../../components/ReportTemplatesEditor.vue'
import SSLCertificateManager from '../../components/SSLCertificateManager.vue'
import DefinitionsTab from '../../components/DefinitionsTab.vue'

const {
  loading,
  error,
  otherSettings,
  fetchGeneralSettings,
  saveGeneralSettings,
  getSettingValue,
  fetchOtherSettings,
  createOtherSetting,
  updateOtherSetting,
  deleteOtherSetting,
} = useSettings()

const activeTab = ref('general')
const integrationEditor = ref(null)
const customJobsEditor = ref(null)
const saltminerJobsEditor = ref(null)
const reportTemplatesEditor = ref(null)
const sslManager = ref(null)
const definitionsTab = ref(null)

watch(activeTab, (tab) => {
  if (tab === 'integrations' && integrationEditor.value) {
    integrationEditor.value.fetchAvailable()
  }
  if (tab === 'custom-jobs' && customJobsEditor.value) {
    customJobsEditor.value.fetchJobs()
  }
  if (tab === 'saltminer-jobs' && saltminerJobsEditor.value) {
    saltminerJobsEditor.value.fetchJobs()
  }
  if (tab === 'reports' && reportTemplatesEditor.value) {
    reportTemplatesEditor.value.fetchTemplates()
  }
  if (tab === 'security' && sslManager.value) {
    sslManager.value.fetchCertificate()
  }
  if (tab === 'definitions' && definitionsTab.value) {
    definitionsTab.value.refresh()
  }
})

// Organization settings
const form = ref({
  orgName: '',
  primaryDomain: '',
})

onMounted(async () => {
  await fetchGeneralSettings()
  form.value.orgName = getSettingValue('orgName') || ''
  form.value.primaryDomain = getSettingValue('primaryDomain') || ''
  await fetchOtherSettings()
})

async function saveOrgSettings() {
  const updates = [
    { property: 'orgName', value: form.value.orgName, value_type: 'string', label: 'Organization Name' },
    { property: 'primaryDomain', value: form.value.primaryDomain, value_type: 'string', label: 'Primary Domain' },
  ]
  await saveGeneralSettings(updates)
}

// Other settings table
const otherHeaders = [
  { title: 'Property', key: 'property' },
  { title: 'Label', key: 'label' },
  { title: 'Value', key: 'value' },
  { title: 'Type', key: 'value_type', width: '100px' },
  { title: 'Description', key: 'description' },
  { title: '', key: 'actions', width: '80px', sortable: false },
]

// Add/Edit dialog
const showSettingDialog = ref(false)
const editingProperty = ref(null)
const settingForm = reactive({
  property: '',
  value: '',
  boolValue: false,
  value_type: 'string',
  label: '',
  description: '',
})

function onValueTypeChange(newType) {
  if (newType === 'boolean') {
    settingForm.boolValue = settingForm.value === 'true'
  } else if (newType === 'integer') {
    settingForm.value = settingForm.value && !isNaN(settingForm.value) ? settingForm.value : ''
  } else if (newType === 'date') {
    settingForm.value = ''
  }
}

function openAddDialog() {
  editingProperty.value = null
  settingForm.property = ''
  settingForm.value = ''
  settingForm.boolValue = false
  settingForm.value_type = 'string'
  settingForm.label = ''
  settingForm.description = ''
  showSettingDialog.value = true
}

function openEditDialog(item) {
  editingProperty.value = item.property
  settingForm.property = item.property
  settingForm.value = item.value || ''
  settingForm.boolValue = item.value === 'true'
  settingForm.value_type = item.value_type || 'string'
  settingForm.label = item.label || ''
  settingForm.description = item.description || ''
  showSettingDialog.value = true
}

function getFormValue() {
  if (settingForm.value_type === 'boolean') {
    return String(settingForm.boolValue)
  }
  if (settingForm.value_type === 'date' && settingForm.value) {
    // Convert datetime-local to UTC ISO string
    return new Date(settingForm.value).toISOString()
  }
  return settingForm.value
}

async function handleSaveSetting() {
  try {
    const data = {
      property: settingForm.property.trim(),
      value: getFormValue(),
      value_type: settingForm.value_type,
      label: settingForm.label,
      description: settingForm.description,
    }
    if (editingProperty.value) {
      await updateOtherSetting(editingProperty.value, data)
    } else {
      await createOtherSetting(data)
    }
    showSettingDialog.value = false
  } catch (e) {
    // error set by composable
  }
}

// Delete
const showDeleteDialog = ref(false)
const deleteTarget = ref('')

function confirmDelete(propertyName) {
  deleteTarget.value = propertyName
  showDeleteDialog.value = true
}

async function handleDelete() {
  try {
    await deleteOtherSetting(deleteTarget.value)
    showDeleteDialog.value = false
  } catch (e) {
    showDeleteDialog.value = false
  }
}
</script>
