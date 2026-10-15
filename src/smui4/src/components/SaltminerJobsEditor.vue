<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-4">
      <span class="text-h6">SaltMiner Jobs</span>
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openNewDialog">
        New Job
      </v-btn>
    </div>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <v-card v-if="filteredJobs.length > 0">
      <v-data-table
        :headers="headers"
        :items="filteredJobs"
        :loading="loading"
        density="compact"
        hover
      >
        <template #item.status="{ item }">
          <v-chip :color="statusColor(item.status)" size="x-small" variant="tonal">
            {{ item.status || '—' }}
          </v-chip>
        </template>
        <template #item.disabled="{ item }">
          <v-chip
            :color="item.disabled ? 'grey' : 'success'"
            size="x-small"
            variant="tonal"
          >
            {{ item.disabled ? 'Disabled' : 'Enabled' }}
          </v-chip>
        </template>
        <template #item.schedule="{ item }">
          <code class="text-caption">{{ item.schedule }}</code>
        </template>
        <template #item.nextRunTime="{ item }">
          <span class="text-caption">{{ formatDate(item.nextRunTime) }}</span>
        </template>
        <template #item.lastRunTime="{ item }">
          <span class="text-caption">{{ formatDate(item.lastRunTime) }}</span>
        </template>
        <template #item.actions="{ item }">
          <v-btn icon="mdi-pencil" size="x-small" variant="text" @click="openEditDialog(item)" />
          <v-btn
            icon="mdi-delete"
            size="x-small"
            variant="text"
            color="error"
            @click="confirmDelete(item)"
          />
        </template>
      </v-data-table>
    </v-card>

    <v-card v-else-if="!loading" class="pa-6 text-center text-medium-emphasis">
      No SaltMiner jobs found.
    </v-card>

    <!-- Edit/Add Dialog -->
    <v-dialog v-model="showDialog" max-width="800" scrollable>
      <v-card>
        <v-card-title>{{ editingJob ? 'Edit Job' : 'Add Job' }}</v-card-title>
        <v-card-text>
          <v-text-field
            v-model="form.name"
            label="Name"
            class="mb-4"
          />
          <v-text-field
            v-model="form.description"
            label="Description"
            class="mb-4"
          />
          <v-select
            v-model="form.option"
            :items="optionItems"
            label="Command"
            class="mb-4"
          />
          <v-text-field
            v-model="form.parameters"
            label="Parameters"
            class="mb-4"
            hint="Optional command-line parameters"
            persistent-hint
          />

          <v-divider class="my-4" />
          <div class="text-subtitle-1 font-weight-medium mb-2">Schedule</div>
          <CronScheduleBuilder v-model="form.schedule" />

          <v-divider class="my-4" />
          <div class="d-flex justify-space-between align-center">
            <div>
              <div class="text-body-1 font-weight-medium">Disabled</div>
              <div class="text-caption text-medium-emphasis">
                When disabled, this job will not run on its schedule
              </div>
            </div>
            <v-switch
              v-model="form.disabled"
              color="primary"
              hide-details
              inset
            />
          </div>

          <v-divider v-if="editingJob" class="my-4" />
          <div v-if="editingJob" class="text-caption text-medium-emphasis">
            <div>Last Run: {{ formatDate(editingJob.lastRunTime) }}</div>
            <div>Next Run: {{ formatDate(editingJob.nextRunTime) }}</div>
            <div>Status: {{ editingJob.status || '—' }}</div>
            <div v-if="editingJob.message">Message: {{ editingJob.message }}</div>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDialog = false">Cancel</v-btn>
          <v-btn
            color="primary"
            :loading="loading"
            :disabled="!form.name || !form.schedule"
            @click="handleSave"
          >
            Save
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation -->
    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card>
        <v-card-title>Delete "{{ deleteTarget?.name }}"?</v-card-title>
        <v-card-text>
          This job will be permanently removed. This cannot be undone.
        </v-card-text>
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
import { ref, reactive, computed, onMounted } from 'vue'
import { useSaltminerJobs } from '../composables/useSaltminerJobs.js'
import CronScheduleBuilder from './CronScheduleBuilder.vue'

const { jobs, primer, loading, error, fetchJobs, fetchPrimer, saveJob, deleteJobs } = useSaltminerJobs()

const headers = [
  { title: 'Name', key: 'name' },
  { title: 'Description', key: 'description' },
  { title: 'Command', key: 'option' },
  { title: 'Parameters', key: 'parameters' },
  { title: 'Schedule', key: 'schedule' },
  { title: 'Status', key: 'status' },
  { title: 'Enabled', key: 'disabled' },
  { title: 'Last Run', key: 'lastRunTime' },
  { title: 'Next Run', key: 'nextRunTime' },
  { title: '', key: 'actions', sortable: false, width: '100px' },
]

const primerOptionValues = computed(() => {
  const items = primer.value?.serviceJobCommandDropdowns || []
  return new Set(items.map((i) => i.value))
})

const optionItems = computed(() => {
  const items = primer.value?.serviceJobCommandDropdowns || []
  return items
    .slice()
    .sort((a, b) => (a.order || 0) - (b.order || 0))
    .map((item) => ({ title: item.display, value: item.value }))
})

// Only show jobs whose option matches a SaltMiner primer command.
// Jobs with options not in the primer are Custom Jobs (they reference
// a script file and are shown on the Custom Jobs tab).
const filteredJobs = computed(() => {
  if (primerOptionValues.value.size === 0) return []
  return jobs.value.filter((job) => primerOptionValues.value.has(job.option))
})

const showDialog = ref(false)
const editingJob = ref(null)

const form = reactive({
  id: null,
  name: '',
  description: '',
  type: 'Command',
  option: '',
  schedule: '',
  parameters: '',
  disabled: false,
  lastRunTime: null,
  message: null,
})

function openEditDialog(job) {
  editingJob.value = job
  form.id = job.id
  form.name = job.name || ''
  form.description = job.description || ''
  form.type = job.type || 'Command'
  form.option = job.option || ''
  form.schedule = job.schedule || ''
  form.parameters = job.parameters || ''
  form.disabled = !!job.disabled
  form.lastRunTime = job.lastRunTime || null
  form.message = job.message || null
  showDialog.value = true
}

function openNewDialog() {
  editingJob.value = null
  form.id = null
  form.name = ''
  form.description = ''
  form.type = 'Command'
  form.option = ''
  form.schedule = '0 0 0 1/1 * ? *'
  form.parameters = ''
  form.disabled = false
  form.lastRunTime = null
  form.message = ''
  showDialog.value = true
}

async function handleSave() {
  try {
    const payload = {
      name: form.name,
      description: form.description,
      type: form.type,
      option: form.option,
      parameters: form.parameters || '',
      schedule: form.schedule,
      disabled: form.disabled,
      runNow: false,
      cancel: false,
      message: form.message || '',
      status: editingJob.value ? undefined : '',
    }
    if (form.id) payload.id = form.id
    if (editingJob.value) payload.lastRunTime = form.lastRunTime
    await saveJob(payload)
    showDialog.value = false
  } catch (e) {
    // error set by composable
  }
}

// Delete
const showDeleteDialog = ref(false)
const deleteTarget = ref(null)

function confirmDelete(job) {
  deleteTarget.value = job
  showDeleteDialog.value = true
}

async function handleDelete() {
  try {
    await deleteJobs([deleteTarget.value.id])
    showDeleteDialog.value = false
  } catch (e) {
    showDeleteDialog.value = false
  }
}

function statusColor(status) {
  if (!status) return 'grey'
  const s = status.toLowerCase()
  if (s === 'completed' || s === 'success') return 'success'
  if (s === 'failed' || s === 'error') return 'error'
  if (s === 'running' || s === 'in progress') return 'info'
  return 'grey'
}

function formatDate(dateStr) {
  if (!dateStr) return '—'
  try {
    return new Date(dateStr).toLocaleString()
  } catch {
    return dateStr
  }
}

onMounted(() => {
  fetchJobs()
  fetchPrimer()
})

defineExpose({ fetchJobs })
</script>
