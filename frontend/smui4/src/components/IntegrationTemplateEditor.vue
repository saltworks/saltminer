<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-4">
      <span class="text-h6">Integration Templates</span>
      <v-btn color="primary" prepend-icon="mdi-plus" @click="openNewDialog">
        New Adapter
      </v-btn>
    </div>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <v-expansion-panels v-if="available.length > 0" v-model="expandedPanel">
      <v-expansion-panel
        v-for="adapter in available"
        :key="adapter.name"
        :value="adapter.name"
      >
        <v-expansion-panel-title>
          <div class="d-flex align-center">
            <v-avatar size="32" rounded="lg" color="grey-lighten-4" class="mr-3">
              <img
                :src="adapter.icon"
                width="16"
                height="16"
                @error="$event.target.src = '/smui4/icons/integrations/default.svg'"
              />
            </v-avatar>
            <span class="font-weight-medium">{{ adapter.name }}</span>
            <v-chip size="x-small" class="ml-2" variant="tonal">
              {{ adapter.fields.length }} fields
            </v-chip>
          </div>
        </v-expansion-panel-title>

        <v-expansion-panel-text>
          <div class="pa-2">
            <!-- Metadata -->
            <v-row class="mb-4">
              <v-col cols="12" md="8">
                <v-text-field
                  v-model="getEditState(adapter.name).description"
                  label="Description"
                 
                  density="compact"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model="getEditState(adapter.name).iconFilename"
                  label="Icon filename"
                 
                  density="compact"
                  placeholder="e.g., checkmarx.svg"
                />
              </v-col>
            </v-row>

            <!-- Fields table -->
            <v-table density="compact" class="mb-4">
              <thead>
                <tr>
                  <th>Property Name</th>
                  <th>Label</th>
                  <th style="width: 150px;">Value Type</th>
                  <th>Description</th>
                  <th style="width: 50px;"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(field, i) in getEditState(adapter.name).fields" :key="i">
                  <td>
                    <v-text-field
                      v-model="field.property"
                      variant="plain"
                      density="compact"
                      hide-details
                    />
                  </td>
                  <td>
                    <v-text-field
                      v-model="field.label"
                      variant="plain"
                      density="compact"
                      hide-details
                    />
                  </td>
                  <td>
                    <v-select
                      v-model="field.value_type"
                      :items="valueTypes"
                      variant="plain"
                      density="compact"
                      hide-details
                    />
                  </td>
                  <td>
                    <v-text-field
                      v-model="field.description"
                      variant="plain"
                      density="compact"
                      hide-details
                    />
                  </td>
                  <td>
                    <v-btn
                      icon="mdi-delete"
                      size="x-small"
                      variant="text"
                      color="error"
                      @click="removeField(adapter.name, i)"
                    />
                  </td>
                </tr>
              </tbody>
            </v-table>

            <v-btn
             
              size="small"
              prepend-icon="mdi-plus"
              class="mb-4"
              @click="addField(adapter.name)"
            >
              Add Field
            </v-btn>

            <v-divider class="mb-4" />

            <div class="d-flex gap-4">
              <v-btn
                color="primary"
                :loading="loading"
                @click="saveAdapter(adapter.name)"
              >
                Save Changes
              </v-btn>
              <v-btn
                color="error"
            variant="outlined"
               
                @click="confirmDeleteAdapter(adapter.name)"
              >
                Delete Adapter
              </v-btn>
            </div>
          </div>
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>

    <v-card v-else class="pa-6 text-center text-medium-emphasis">
      No integration templates found. Click "New Adapter" to create one.
    </v-card>

    <!-- New Adapter Dialog -->
    <v-dialog v-model="showNewDialog" max-width="500">
      <v-card>
        <v-card-title>New Adapter</v-card-title>
        <v-card-text>
          <v-text-field
            v-model="newAdapter.name"
            label="Adapter Name"
           
            class="mb-4"
            :error-messages="newAdapterError"
          />
          <v-text-field
            v-model="newAdapter.description"
            label="Description"
           
            class="mb-4"
          />
          <v-text-field
            v-model="newAdapter.icon"
            label="Icon filename"
           
            placeholder="e.g., myadapter.svg (optional)"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showNewDialog = false">Cancel</v-btn>
          <v-btn
            color="primary"
            :loading="loading"
            :disabled="!newAdapter.name.trim()"
            @click="handleCreateAdapter"
          >
            Create
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation Dialog -->
    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card>
        <v-card-title>Delete {{ deleteTarget }}?</v-card-title>
        <v-card-text>
          This will remove the adapter template and all its field definitions. This cannot be undone.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn color="error" :loading="loading" @click="handleDeleteAdapter">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Propagation Dialog -->
    <v-dialog v-model="showPropagateDialog" max-width="500">
      <v-card>
        <v-card-title>Update Configured Instances?</v-card-title>
        <v-card-text>
          This adapter has configured instances. Do you want to update them with the new field definitions?
          <p class="text-body-2 text-medium-emphasis mt-2">
            New fields will be added with empty values. Removed fields will be deleted. Existing field values will not be changed.
          </p>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showPropagateDialog = false">Skip</v-btn>
          <v-btn color="primary" :loading="loading" @click="handlePropagate">Update Instances</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, watch, onMounted } from 'vue'
import { useIntegrations } from '../composables/useIntegrations.js'

const {
  available,
  loading,
  error,
  fetchAvailable,
  updateAdapterTemplate,
  createAdapter,
  deleteAdapterTemplate,
  propagateTemplate,
} = useIntegrations()

const valueTypes = ['string', 'boolean', 'integer', 'date']
const expandedPanel = ref(null)

onMounted(fetchAvailable)

// Edit state per adapter — populated when available data loads
const editStates = reactive({})

function getEditState(adapterName) {
  if (!editStates[adapterName]) {
    const adapter = available.value.find((a) => a.name === adapterName)
    if (!adapter) return { description: '', iconFilename: '', fields: [] }
    editStates[adapterName] = {
      description: adapter.description || '',
      iconFilename: extractFilename(adapter.icon),
      fields: adapter.fields.map((f) => ({ ...f })),
    }
  }
  return editStates[adapterName]
}

function extractFilename(iconPath) {
  if (!iconPath) return ''
  const parts = iconPath.split('/')
  const filename = parts[parts.length - 1]
  return filename === 'default.svg' ? '' : filename
}

function addField(adapterName) {
  getEditState(adapterName).fields.push({
    property: '',
    label: '',
    value_type: 'string',
    description: '',
  })
}

function removeField(adapterName, index) {
  getEditState(adapterName).fields.splice(index, 1)
}

// Save
const showPropagateDialog = ref(false)
const propagateTarget = ref('')
const propagateFields = ref([])

async function saveAdapter(adapterName) {
  const state = getEditState(adapterName)
  try {
    await updateAdapterTemplate(adapterName, {
      description: state.description,
      icon: state.iconFilename,
      fields: state.fields,
    })
    await fetchAvailable()
    // Reset edit state so it reloads from fresh data
    delete editStates[adapterName]

    // Check if instances exist for propagation prompt
    propagateTarget.value = adapterName
    propagateFields.value = state.fields
    showPropagateDialog.value = true
  } catch (e) {
    // error already set by composable
  }
}

async function handlePropagate() {
  try {
    await propagateTemplate(propagateTarget.value, propagateFields.value)
    showPropagateDialog.value = false
  } catch (e) {
    // error already set by composable
  }
}

// New adapter
const showNewDialog = ref(false)
const newAdapterError = ref('')
const newAdapter = reactive({ name: '', description: '', icon: '' })

function openNewDialog() {
  newAdapter.name = ''
  newAdapter.description = ''
  newAdapter.icon = ''
  newAdapterError.value = ''
  showNewDialog.value = true
}

async function handleCreateAdapter() {
  newAdapterError.value = ''
  try {
    await createAdapter({
      adapterName: newAdapter.name.trim(),
      description: newAdapter.description,
      icon: newAdapter.icon,
    })
    showNewDialog.value = false
    expandedPanel.value = newAdapter.name.trim()
  } catch (e) {
    if (e.message && e.message.includes('already exists')) {
      newAdapterError.value = 'An adapter with this name already exists.'
    } else {
      newAdapterError.value = e.message
    }
  }
}

// Delete adapter
const showDeleteDialog = ref(false)
const deleteTarget = ref('')

function confirmDeleteAdapter(adapterName) {
  deleteTarget.value = adapterName
  showDeleteDialog.value = true
}

async function handleDeleteAdapter() {
  try {
    await deleteAdapterTemplate(deleteTarget.value)
    delete editStates[deleteTarget.value]
    showDeleteDialog.value = false
  } catch (e) {
    showDeleteDialog.value = false
    // error set by composable — will show "in use" message
  }
}

// Reload edit states when available data changes
watch(available, () => {
  // Clear cached edit states so they reload from fresh data
  for (const key of Object.keys(editStates)) {
    delete editStates[key]
  }
})

defineExpose({ fetchAvailable })
</script>
