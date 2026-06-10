<template>
  <div>
    <h1 class="text-h4 mb-1">Scanners</h1>
    <p class="text-body-1 text-medium-emphasis mb-6">
      Manage and configure security scanners
    </p>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <!-- Detail mode: scanner config -->
    <template v-if="scanner">
      <!-- Quick Navigation -->
      <v-card class="pa-4 mb-6">
        <p class="text-body-2 font-weight-medium mb-2">Quick Navigation</p>
        <div class="d-flex flex-wrap gap-2">
          <v-chip
            v-for="s in allScanners"
            :key="s.name"
            :color="s.name === scanner ? 'primary' : undefined"
            :variant="s.name === scanner ? 'flat' : 'outlined'"
            @click="router.push({ name: 'scanning-scanner-detail', params: { scanner: s.name } })"
          >
            <v-avatar :color="s.color" size="20" class="mr-1">
              <v-icon size="12" color="white">{{ s.icon }}</v-icon>
            </v-avatar>
            {{ s.name }}
          </v-chip>
        </div>
      </v-card>

      <!-- Scanner Details -->
      <v-card class="pa-6 mb-6">
        <div class="d-flex align-center mb-6">
          <v-avatar :color="getScannerMeta(scanner)?.color || '#1A7F64'" size="56" rounded="lg" class="mr-4">
            <v-icon color="white" size="28">mdi-radar</v-icon>
          </v-avatar>
          <div>
            <h2 class="text-h5">{{ scanner }}</h2>
            <p class="text-body-2 text-medium-emphasis">{{ getScannerMeta(scanner)?.description || '' }}</p>
          </div>
        </div>

        <!-- Scan Schedule -->
        <v-card variant="tonal" class="pa-4 mb-6">
          <div class="d-flex align-center mb-4">
            <v-icon class="mr-2">mdi-clock-outline</v-icon>
            <span class="text-h6">Scan Schedule</span>
          </div>

          <v-row class="mb-2">
            <v-col cols="12" md="4">
              <v-select
                v-model="form.frequency"
                :items="['Daily', 'Weekly', 'Bi-weekly', 'Monthly']"
                label="Frequency"
               
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="form.startDate"
                label="Start Date"
               
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="form.startTime"
                label="Start Time (America/New_York)"
               
              />
            </v-col>
          </v-row>

          <v-alert
            v-if="form.frequency && form.startDate"
            type="info"
            variant="tonal"
            density="compact"
          >
            Schedule: {{ form.frequency }} starting on
            <strong>{{ form.startDate }}</strong> at {{ form.startTime || '00:00' }}
            (America/New_York)
          </v-alert>
        </v-card>

        <!-- Command Line Parameters -->
        <div class="d-flex justify-space-between align-center mb-2">
          <div class="d-flex align-center">
            <v-icon class="mr-2">mdi-console</v-icon>
            <span class="text-h6">Command Line Parameters</span>
          </div>
          <v-btn variant="text" size="small" @click="form.commandLine = ''">Reset to Default</v-btn>
        </div>

        <v-textarea
          v-model="form.commandLine"
         
          placeholder="Enter command line parameters..."
          rows="3"
          hint="Configure scanner-specific command line arguments and options"
          persistent-hint
          class="mb-6"
        />

        <div class="d-flex gap-4">
          <v-btn color="primary" :loading="loading" @click="saveSettings">
            Save Changes
          </v-btn>
          <v-btn color="error" variant="outlined" @click="confirmDelete = true">
            Delete Scanner
          </v-btn>
        </div>
      </v-card>

      <!-- Delete Confirmation -->
      <v-dialog v-model="confirmDelete" max-width="400">
        <v-card>
          <v-card-title>Delete {{ scanner }}?</v-card-title>
          <v-card-text>This will remove all configuration for this scanner.</v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn variant="text" @click="confirmDelete = false">Cancel</v-btn>
            <v-btn color="error" :loading="loading" @click="handleDelete">Delete</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>
    </template>

    <!-- List mode: quick nav chips + link to first scanner -->
    <template v-else>
      <v-card class="pa-4 mb-6">
        <p class="text-body-2 font-weight-medium mb-2">Quick Navigation</p>
        <div class="d-flex flex-wrap gap-2">
          <v-chip
            v-for="s in allScanners"
            :key="s.name"
           
            @click="router.push({ name: 'scanning-scanner-detail', params: { scanner: s.name } })"
          >
            <v-avatar :color="s.color" size="20" class="mr-1">
              <v-icon size="12" color="white">{{ s.icon }}</v-icon>
            </v-avatar>
            {{ s.name }}
          </v-chip>
        </div>
      </v-card>

      <v-row>
        <v-col
          v-for="s in configuredScanners"
          :key="s.scanner"
          cols="12"
          md="6"
          lg="4"
        >
          <v-card
           
            class="pa-6 cursor-pointer"
            @click="router.push({ name: 'scanning-scanner-detail', params: { scanner: s.scanner } })"
          >
            <div class="d-flex align-center mb-2">
              <v-avatar :color="getScannerMeta(s.scanner)?.color || '#1A7F64'" size="48" rounded="lg" class="mr-3">
                <v-icon color="white" size="24">mdi-radar</v-icon>
              </v-avatar>
              <div>
                <h3 class="text-h6">{{ s.scanner }}</h3>
                <p class="text-body-2 text-medium-emphasis">{{ getScannerMeta(s.scanner)?.description || '' }}</p>
              </div>
            </div>
          </v-card>
        </v-col>
        <v-col v-if="!loading && configuredScanners.length === 0" cols="12">
          <v-card class="pa-6 text-center text-medium-emphasis">
            No scanners configured. Select a scanner from the quick navigation above to configure it.
          </v-card>
        </v-col>
      </v-row>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useScanning } from '../../composables/useScanning.js'

const AVAILABLE_SCANNERS = [
  { name: 'Nmap', description: 'Network discovery and security auditing tool', icon: 'mdi-radar', color: '#1A7F64' },
  { name: 'Nessus', description: 'Vulnerability scanner for network assessment', icon: 'mdi-radar', color: '#E53935' },
  { name: 'OpenVAS', description: 'Open-source vulnerability assessment scanner', icon: 'mdi-radar', color: '#43A047' },
  { name: 'Burp Suite', description: 'Web application security testing platform', icon: 'mdi-radar', color: '#F9A825' },
  { name: 'Nikto', description: 'Web server vulnerability scanner', icon: 'mdi-radar', color: '#5CBBFF' },
  { name: 'Metasploit', description: 'Penetration testing and exploit framework', icon: 'mdi-radar', color: '#7B61FF' },
]

const props = defineProps({
  scanner: { type: String, default: '' },
})

const router = useRouter()
const {
  scanners: configuredScanners,
  scannerDetail,
  loading,
  error,
  fetchScanners,
  fetchScannerSettings,
  saveScannerSettings,
  deleteScanner,
} = useScanning()

const allScanners = ref(AVAILABLE_SCANNERS)
const confirmDelete = ref(false)

const form = ref({
  frequency: 'Daily',
  startDate: '',
  startTime: '',
  commandLine: '',
})

function getScannerMeta(name) {
  return AVAILABLE_SCANNERS.find((s) => s.name === name) || null
}

function populateForm(detail) {
  if (!detail) return
  const get = (prop, fallback = '') => {
    const setting = detail.properties.find((p) => p.property === prop)
    return setting?.value ?? fallback
  }
  form.value.frequency = get('frequency', 'Daily')
  form.value.startDate = get('startDate')
  form.value.startTime = get('startTime')
  form.value.commandLine = get('commandLine')
}

async function saveSettings() {
  const updates = [
    { property: 'frequency', value: form.value.frequency, value_type: 'string', label: 'Frequency' },
    { property: 'startDate', value: form.value.startDate, value_type: 'string', label: 'Start Date' },
    { property: 'startTime', value: form.value.startTime, value_type: 'string', label: 'Start Time' },
    { property: 'commandLine', value: form.value.commandLine, value_type: 'string', label: 'Command Line Parameters' },
  ]
  await saveScannerSettings(props.scanner, updates)
}

async function handleDelete() {
  await deleteScanner(props.scanner)
  confirmDelete.value = false
  router.push({ name: 'scanning-scanners' })
}

watch(scannerDetail, (detail) => {
  if (detail) populateForm(detail)
})

onMounted(async () => {
  if (props.scanner) {
    await fetchScannerSettings(props.scanner)
  } else {
    await fetchScanners()
  }
})
</script>
