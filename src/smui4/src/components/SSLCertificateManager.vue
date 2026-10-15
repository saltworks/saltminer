<template>
  <div>
    <h2 class="text-h5 mb-6">SSL Certificate</h2>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <!-- Current Certificate Card -->
    <v-card class="pa-6 mb-6">
      <div class="d-flex align-center mb-4">
        <v-icon color="primary" class="mr-2">mdi-certificate</v-icon>
        <span class="text-h6">Current Certificate</span>
      </div>

      <div v-if="loading" class="d-flex justify-center pa-4">
        <v-progress-circular indeterminate color="primary" />
      </div>

      <div v-else-if="certificate && certificate.found">
        <div class="d-flex align-center mb-4">
          <v-chip
            v-if="isExpired"
            color="error"
            size="small"
            class="mr-2"
          >
            Expired
          </v-chip>
          <v-chip
            v-else-if="expiringSoon"
            color="warning"
            size="small"
            class="mr-2"
          >
            Expiring Soon
          </v-chip>
          <v-chip
            v-else
            color="success"
            size="small"
            class="mr-2"
          >
            Valid
          </v-chip>
        </div>

        <v-list density="compact" lines="one">
          <v-list-item>
            <template #prepend>
              <span class="text-body-2 text-medium-emphasis cert-label">Subject (CN)</span>
            </template>
            <v-list-item-title class="text-body-2">{{ certificate.subject }}</v-list-item-title>
          </v-list-item>
          <v-divider />
          <v-list-item>
            <template #prepend>
              <span class="text-body-2 text-medium-emphasis cert-label">Issuer</span>
            </template>
            <v-list-item-title class="text-body-2">{{ certificate.issuer }}</v-list-item-title>
          </v-list-item>
          <v-divider />
          <v-list-item>
            <template #prepend>
              <span class="text-body-2 text-medium-emphasis cert-label">Valid From</span>
            </template>
            <v-list-item-title class="text-body-2">{{ formatDate(certificate.validFrom) }}</v-list-item-title>
          </v-list-item>
          <v-divider />
          <v-list-item>
            <template #prepend>
              <span class="text-body-2 text-medium-emphasis cert-label">Valid To</span>
            </template>
            <v-list-item-title class="text-body-2">{{ formatDate(certificate.validTo) }}</v-list-item-title>
          </v-list-item>
          <v-divider />
          <v-list-item>
            <template #prepend>
              <span class="text-body-2 text-medium-emphasis cert-label">Serial Number</span>
            </template>
            <v-list-item-title class="text-body-2 font-monospace">{{ certificate.serialNumber }}</v-list-item-title>
          </v-list-item>
        </v-list>
      </div>

      <div v-else-if="!loading" class="text-body-1 text-medium-emphasis">
        No certificate found. Upload a certificate and key below.
      </div>
    </v-card>

    <!-- Upload Section Card -->
    <v-card class="pa-6">
      <div class="d-flex align-center mb-4">
        <v-icon color="primary" class="mr-2">mdi-upload</v-icon>
        <span class="text-h6">Upload Certificate</span>
      </div>

      <v-alert
        v-if="uploadError"
        type="error"
        closable
        class="mb-4"
        @click:close="uploadError = null"
      >
        {{ uploadError }}
      </v-alert>

      <v-alert
        v-if="uploadSuccess"
        type="success"
        closable
        class="mb-4"
        @click:close="uploadSuccess = false"
      >
        Certificate uploaded successfully. Run <code>docker compose restart nginx</code> to apply the new certificate.
      </v-alert>

      <v-file-input
        v-model="certFile"
        label="Certificate (.crt)"
        accept=".crt,.pem"
        prepend-icon="mdi-certificate-outline"
        class="mb-4"
        :disabled="loading"
        @update:model-value="uploadSuccess = false"
      />

      <v-file-input
        v-model="keyFile"
        label="Private Key (.key)"
        accept=".key,.pem"
        prepend-icon="mdi-key-outline"
        class="mb-4"
        :disabled="loading"
        @update:model-value="uploadSuccess = false"
      />

      <v-btn
        color="primary"
        :loading="loading"
        :disabled="!certFile || !keyFile"
        prepend-icon="mdi-upload"
        @click="handleUpload"
      >
        Upload Certificate
      </v-btn>
    </v-card>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useSSL } from '../composables/useSSL.js'

const { certificate, loading, error, fetchCertificate, uploadCertificate } = useSSL()

const certFile = ref(null)
const keyFile = ref(null)
const uploadError = ref(null)
const uploadSuccess = ref(false)

const isExpired = computed(() => {
  if (!certificate.value?.validTo) return false
  return new Date(certificate.value.validTo) < new Date()
})

const expiringSoon = computed(() => {
  if (!certificate.value?.validTo || isExpired.value) return false
  const thirtyDays = 30 * 24 * 60 * 60 * 1000
  return new Date(certificate.value.validTo) - new Date() < thirtyDays
})

function formatDate(dateStr) {
  if (!dateStr) return '—'
  return new Date(dateStr).toLocaleString()
}

async function handleUpload() {
  uploadError.value = null
  uploadSuccess.value = false
  try {
    await uploadCertificate(certFile.value, keyFile.value)
    certFile.value = null
    keyFile.value = null
    uploadSuccess.value = true
  } catch (e) {
    uploadError.value = e.message
  }
}

onMounted(fetchCertificate)

defineExpose({ fetchCertificate })
</script>

<style scoped>
.cert-label {
  min-width: 140px;
  display: inline-block;
}

.font-monospace {
  font-family: monospace;
}
</style>
