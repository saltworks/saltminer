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

    <h1 class="text-h4 mb-1">Available Integrations</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Select an integration type to add a new instance. You can add multiple instances of the same type.
    </p>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

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
          <template #actions>
            <v-btn
              color="primary"
              block
              prepend-icon="mdi-plus"
              @click="addInstance(adapter.name)"
            >
              Add Instance
            </v-btn>
          </template>
        </integration-card>
      </v-col>
    </v-row>

    <!-- Add Instance Dialog -->
    <v-dialog v-model="showDialog" max-width="500">
      <v-card>
        <v-card-title>Add {{ selectedAdapter }} Instance</v-card-title>
        <v-card-text>
          <v-text-field
            v-model="newInstanceName"
            label="Instance Name"
           
            placeholder="e.g., Checkmarx Production"
            :error-messages="dialogError"
            autofocus
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="closeDialog">Cancel</v-btn>
          <v-btn
            color="primary"
            :loading="loading"
            :disabled="!newInstanceName.trim()"
            @click="handleCreate"
          >
            Create
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useIntegrations } from '../../composables/useIntegrations.js'
import IntegrationCard from '../../components/IntegrationCard.vue'

const router = useRouter()
const { available, loading, error, fetchAvailable, createInstance } = useIntegrations()

const showDialog = ref(false)
const selectedAdapter = ref('')
const newInstanceName = ref('')
const dialogError = ref('')

function addInstance(adapterName) {
  selectedAdapter.value = adapterName
  newInstanceName.value = ''
  dialogError.value = ''
  showDialog.value = true
}

function closeDialog() {
  showDialog.value = false
  dialogError.value = ''
}

async function handleCreate() {
  dialogError.value = ''
  try {
    await createInstance(selectedAdapter.value, newInstanceName.value.trim())
    showDialog.value = false
    router.push({
      name: 'integrations-configured-detail',
      params: { instance: newInstanceName.value.trim() },
    })
  } catch (e) {
    if (e.message && e.message.includes('already exists')) {
      dialogError.value = 'An instance with this name already exists. Please choose a different name.'
    } else {
      dialogError.value = e.message
    }
  }
}

onMounted(fetchAvailable)
</script>
