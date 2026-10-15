<template>
  <div>
    <h2 class="text-h6 mb-2">Look Up Definitions</h2>
    <p class="text-body-2 text-medium-emphasis mb-4">
      Edit the dropdown options for each look up type. Each look up is a list of display/value/order items.
    </p>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <div v-if="loading && lookups.length === 0" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate color="primary" />
    </div>

    <v-expansion-panels v-else v-model="expanded" multiple>
      <v-expansion-panel
        v-for="lookup in lookups"
        :key="lookup.id"
        :value="lookup.id"
      >
        <v-expansion-panel-title>
          <div class="d-flex align-center flex-grow-1">
            <span class="font-weight-medium mr-3">{{ lookup.type }}</span>
            <v-chip size="x-small" variant="tonal">
              {{ (editStates[lookup.id]?.values || lookup.values || []).length }} values
            </v-chip>
          </div>
        </v-expansion-panel-title>

        <v-expansion-panel-text>
          <div class="pa-2">
            <v-table density="compact">
              <thead>
                <tr>
                  <th style="width: 100px;">Order</th>
                  <th>Display</th>
                  <th>Value</th>
                  <th style="width: 60px;"></th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="(item, i) in getEditState(lookup).values"
                  :key="i"
                >
                  <td>
                    <v-text-field
                      v-model.number="item.order"
                      type="number"
                      variant="plain"
                      density="compact"
                      hide-details
                    />
                  </td>
                  <td>
                    <v-text-field
                      v-model="item.display"
                      variant="plain"
                      density="compact"
                      hide-details
                    />
                  </td>
                  <td>
                    <v-text-field
                      v-model="item.value"
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
                      @click="removeRow(lookup, i)"
                    />
                  </td>
                </tr>
              </tbody>
            </v-table>

            <v-btn
              variant="outlined"
              size="small"
              prepend-icon="mdi-plus"
              class="mt-4 mb-4"
              @click="addRow(lookup)"
            >
              Add Value
            </v-btn>

            <v-divider class="mb-4" />

            <div class="d-flex gap-4">
              <v-btn
                color="primary"
                :loading="loading"
                @click="handleSave(lookup)"
              >
                Save Changes
              </v-btn>
            </div>
          </div>
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>
  </div>
</template>

<script setup>
import { ref, reactive, watch, onMounted } from 'vue'
import { useLookups } from '../composables/useLookups.js'

const { lookups, loading, error, fetchLookups, saveLookup } = useLookups()

const expanded = ref([])
// Edit state keyed by lookup id, populated on first access per lookup
const editStates = reactive({})

function getEditState(lookup) {
  if (!editStates[lookup.id]) {
    editStates[lookup.id] = {
      values: (lookup.values || []).map((v) => ({
        display: v.display || '',
        value: v.value || '',
        order: typeof v.order === 'number' ? v.order : 0,
      })),
    }
  }
  return editStates[lookup.id]
}

function addRow(lookup) {
  const state = getEditState(lookup)
  const maxOrder = state.values.reduce((m, v) => Math.max(m, v.order || 0), 0)
  state.values.push({ display: '', value: '', order: maxOrder + 1 })
}

function removeRow(lookup, index) {
  const state = getEditState(lookup)
  state.values.splice(index, 1)
}

async function handleSave(lookup) {
  try {
    const state = getEditState(lookup)
    const payload = {
      ...lookup,
      values: state.values.map((v) => ({
        display: v.display,
        value: v.value,
        order: typeof v.order === 'number' ? v.order : parseInt(v.order, 10) || 0,
      })),
    }
    await saveLookup(payload)
    // Reset edit state so it reloads from fresh data
    delete editStates[lookup.id]
  } catch (e) {
    // error set by composable
  }
}

// Clear edit states when lookups list refreshes so table shows server state
watch(lookups, () => {
  for (const key of Object.keys(editStates)) {
    delete editStates[key]
  }
})

onMounted(fetchLookups)

defineExpose({ fetchLookups })
</script>
