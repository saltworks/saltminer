<template>
  <div>
    <span class="text-h6">Custom Field Definitions</span>

    <v-alert v-if="error" type="error" closable class="mt-4 mb-2" @click:close="error = null">
      {{ error }}
    </v-alert>

    <v-expansion-panels class="mt-4" multiple>
      <v-expansion-panel
        v-for="def in definitions"
        :key="def.type"
        :value="def.type"
      >
        <v-expansion-panel-title>
          <div class="d-flex align-center">
            <span class="font-weight-medium">{{ def.type }}</span>
            <v-chip size="x-small" class="ml-2" variant="tonal">
              {{ def.values.length }} {{ def.values.length === 1 ? 'field' : 'fields' }}
            </v-chip>
          </div>
        </v-expansion-panel-title>

        <v-expansion-panel-text>
          <div class="pa-2">
            <v-table density="compact" class="mb-4">
              <thead>
                <tr>
                  <th style="width: 70px;">Order</th>
                  <th style="min-width: 110px;">Section</th>
                  <th style="min-width: 120px;">Name</th>
                  <th style="min-width: 140px;">Display</th>
                  <th style="min-width: 190px;">Type</th>
                  <th style="min-width: 120px;">Default</th>
                  <th style="width: 70px;" class="text-center">Hidden</th>
                  <th style="width: 80px;" class="text-center">Read Only</th>
                  <th style="width: 80px;" class="text-center">Required</th>
                  <th style="min-width: 200px;">Options</th>
                  <th style="width: 40px;"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(field, idx) in getEditState(def.type).values" :key="idx">
                  <!-- Order -->
                  <td>
                    <v-text-field
                      v-model.number="field.order"
                      type="number"
                      variant="underlined"
                      density="compact"
                      hide-details
                    />
                  </td>
                  <!-- Section (defaults to 'saltminer' on save when blank) -->
                  <td>
                    <v-text-field
                      v-model="field.section"
                      variant="underlined"
                      density="compact"
                      hide-details
                      placeholder="saltminer"
                    />
                  </td>
                  <!-- Name -->
                  <td>
                    <v-text-field
                      v-model="field.name"
                      variant="underlined"
                      density="compact"
                      hide-details
                      placeholder="field_key"
                    />
                  </td>
                  <!-- Display -->
                  <td>
                    <v-text-field
                      v-model="field.display"
                      variant="underlined"
                      density="compact"
                      hide-details
                    />
                  </td>
                  <!-- Type -->
                  <td>
                    <v-select
                      v-model="field.type"
                      :items="fieldTypes"
                      variant="underlined"
                      density="compact"
                      hide-details
                    />
                  </td>
                  <!-- Default -->
                  <td>
                    <v-text-field
                      v-model="field.default"
                      variant="underlined"
                      density="compact"
                      hide-details
                    />
                  </td>
                  <!-- Hidden -->
                  <td class="text-center">
                    <v-checkbox
                      v-model="field.hidden"
                      hide-details
                      density="compact"
                    />
                  </td>
                  <!-- Read Only -->
                  <td class="text-center">
                    <v-checkbox
                      v-model="field.readOnly"
                      hide-details
                      density="compact"
                    />
                  </td>
                  <!-- Required -->
                  <td class="text-center">
                    <v-checkbox
                      v-model="field.required"
                      hide-details
                      density="compact"
                    />
                  </td>
                  <!-- Options — only for select types -->
                  <td>
                    <v-text-field
                      v-if="isSelectType(field.type)"
                      v-model="field._optionsRaw"
                      variant="underlined"
                      density="compact"
                      hide-details
                      placeholder="Option A, Option B"
                    />
                    <span v-else class="text-medium-emphasis text-body-2 px-1">—</span>
                  </td>
                  <!-- Delete -->
                  <td>
                    <v-btn
                      icon="mdi-delete"
                      size="x-small"
                      variant="text"
                      color="error"
                      @click="removeField(def.type, idx)"
                    />
                  </td>
                </tr>

                <tr v-if="getEditState(def.type).values.length === 0">
                  <td colspan="11" class="text-center text-medium-emphasis text-body-2 py-4">
                    No custom fields defined. Click "Add Field" to add one.
                  </td>
                </tr>
              </tbody>
            </v-table>

            <div class="d-flex align-center gap-3">
              <v-btn
                size="small"
                variant="tonal"
                prepend-icon="mdi-plus"
                @click="addField(def.type)"
              >
                Add Field
              </v-btn>

              <v-btn
                color="primary"
                size="small"
                :loading="loading"
                @click="handleSave(def.type)"
              >
                Save
              </v-btn>
            </div>
          </div>
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>

    <v-card v-if="!loading && definitions.length === 0" class="pa-6 mt-4 text-center text-medium-emphasis">
      No definitions found.
    </v-card>

    <div v-if="loading && definitions.length === 0" class="d-flex justify-center mt-6">
      <v-progress-circular indeterminate color="primary" />
    </div>
  </div>
</template>

<script setup>
import { reactive, watch, onMounted } from 'vue'
import { useCustomFieldDefs } from '../composables/useCustomFieldDefs.js'

const { definitions, loading, error, fetchDefinitions, saveDefinition } = useCustomFieldDefs()

const fieldTypes = [
  'Single line text (text)',
  'Multi line text (text)',
  'Markdown (text)',
  'Integer (long)',
  'Number (double)',
  'Date (date)',
  'Single select drop down',
  'Multi select drop down',
]

const SELECT_TYPES = new Set(['Single select drop down', 'Multi select drop down'])

function isSelectType(type) {
  return SELECT_TYPES.has(type)
}

// --- Edit state per definition type ---

const editStates = reactive({})

function getEditState(type) {
  if (!editStates[type]) {
    const def = definitions.value.find((d) => d.type === type)
    if (!def) return { values: [] }
    editStates[type] = {
      values: def.values.map((v) => ({
        ...v,
        section: v.section ?? '',
        _isNew: false,
        _optionsRaw: Array.isArray(v.options) ? v.options.join(', ') : '',
      })),
    }
  }
  return editStates[type]
}

function addField(type) {
  getEditState(type).values.push({
    section: '',
    name: '',
    display: '',
    type: 'Single line text (text)',
    readOnly: false,
    default: null,
    hidden: false,
    required: false,
    order: getEditState(type).values.length + 1,
    options: [],
    _isNew: true,
    _optionsRaw: '',
  })
}

function removeField(type, index) {
  getEditState(type).values.splice(index, 1)
}

async function handleSave(type) {
  const state = getEditState(type)
  const def = definitions.value.find((d) => d.type === type)
  if (!def) return

  const values = state.values.map(({ _isNew, _optionsRaw, ...rest }) => ({
    ...rest,
    section: (rest.section && rest.section.trim()) || 'saltminer',
    options: isSelectType(rest.type)
      ? (_optionsRaw || '')
          .split(',')
          .map((s) => s.trim())
          .filter(Boolean)
      : [],
  }))

  try {
    await saveDefinition({ ...def, values })
    // reset state so it reloads from fresh data
    delete editStates[type]
  } catch (e) {
    // error already set by composable
  }
}

// Reload edit states when definitions data changes
watch(definitions, () => {
  for (const key of Object.keys(editStates)) {
    delete editStates[key]
  }
})

onMounted(fetchDefinitions)

defineExpose({ fetchDefinitions })
</script>
