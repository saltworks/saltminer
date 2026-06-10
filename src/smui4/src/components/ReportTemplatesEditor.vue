<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-4">
      <span class="text-h6">Report Templates</span>
    </div>

    <v-alert v-if="error" type="error" closable class="mb-4" @click:close="error = null">
      {{ error }}
    </v-alert>

    <!-- Default Template Download -->
    <v-card class="pa-6 mb-6">
      <div class="d-flex align-center">
        <v-icon color="primary" size="32" class="mr-4">mdi-file-document-outline</v-icon>
        <div class="flex-grow-1">
          <h3 class="text-subtitle-1 font-weight-medium">Default Template</h3>
          <p class="text-body-2 text-medium-emphasis">
            Download the SaltMiner default report template as a starting point for customization.
          </p>
        </div>
        <v-btn
          color="primary"
          prepend-icon="mdi-download"
          href="/smui4/SaltMinerTemplate.docx"
          download
        >
          Download Template
        </v-btn>
      </div>
    </v-card>

    <!-- Upload Section -->
    <v-card class="pa-6 mb-6">
      <div class="d-flex align-center mb-4">
        <v-icon color="primary" class="mr-2">mdi-upload</v-icon>
        <span class="text-subtitle-1 font-weight-medium">Upload Custom Template</span>
      </div>

      <div
        class="upload-area pa-8 text-center rounded-lg mb-2"
        :class="{ 'upload-area--dragging': isDragging }"
        @dragover.prevent="isDragging = true"
        @dragleave.prevent="isDragging = false"
        @drop.prevent="handleDrop"
        @click="$refs.fileInput.click()"
      >
        <v-icon size="48" color="grey">mdi-cloud-upload-outline</v-icon>
        <p class="text-body-1 mt-2">Drag and drop a .docx file here, or click to browse</p>
        <p class="text-caption text-medium-emphasis">Maximum file size: 25MB</p>
        <input
          ref="fileInput"
          type="file"
          accept=".docx"
          style="display: none"
          @change="handleFileSelect"
        />
      </div>

      <v-alert v-if="uploadError" type="error" density="compact" class="mt-2" closable @click:close="uploadError = ''">
        {{ uploadError }}
      </v-alert>
    </v-card>

    <!-- Templates List -->
    <v-card class="pa-6">
      <div class="d-flex align-center mb-4">
        <v-icon color="primary" class="mr-2">mdi-file-multiple-outline</v-icon>
        <span class="text-subtitle-1 font-weight-medium">Custom Templates</span>
      </div>

      <v-data-table
        v-if="templates.length > 0"
        :headers="headers"
        :items="templates"
        density="compact"
        hover
      >
        <template #item.name="{ item }">
          <div class="d-flex align-center">
            <v-icon size="18" class="mr-2" color="primary">mdi-file-word</v-icon>
            {{ item.name }}
          </div>
        </template>
        <template #item.size="{ item }">
          {{ formatSize(item.size) }}
        </template>
        <template #item.lastModified="{ item }">
          {{ formatDate(item.lastModified) }}
        </template>
        <template #item.actions="{ item }">
          <v-btn
            icon="mdi-download"
            size="x-small"
            variant="text"
            :href="`/smuiapi4/report-templates/${encodeURIComponent(item.name)}`"
            download
          />
          <v-btn
            icon="mdi-delete"
            size="x-small"
            variant="text"
            color="error"
            @click="confirmDelete(item.name)"
          />
        </template>
      </v-data-table>

      <div v-else class="text-center text-medium-emphasis pa-4">
        No custom templates uploaded yet.
      </div>
    </v-card>

    <!-- Delete Confirmation -->
    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card>
        <v-card-title>Delete "{{ deleteTarget }}"?</v-card-title>
        <v-card-text>This template will be permanently removed.</v-card-text>
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
import { ref, onMounted } from 'vue'
import { useReportTemplates } from '../composables/useReportTemplates.js'

const { templates, loading, error, fetchTemplates, uploadTemplate, deleteTemplate } = useReportTemplates()

const isDragging = ref(false)
const uploadError = ref('')
const fileInput = ref(null)

const headers = [
  { title: 'Filename', key: 'name' },
  { title: 'Size', key: 'size', width: '120px' },
  { title: 'Last Modified', key: 'lastModified', width: '200px' },
  { title: '', key: 'actions', width: '80px', sortable: false },
]

function formatSize(bytes) {
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

function formatDate(isoString) {
  if (!isoString) return ''
  return new Date(isoString).toLocaleString()
}

async function handleFileSelect(event) {
  const file = event.target.files[0]
  if (file) await doUpload(file)
  event.target.value = ''
}

async function handleDrop(event) {
  isDragging.value = false
  const file = event.dataTransfer.files[0]
  if (file) await doUpload(file)
}

async function doUpload(file) {
  uploadError.value = ''
  if (!file.name.toLowerCase().endsWith('.docx')) {
    uploadError.value = 'Only .docx files are allowed'
    return
  }
  if (file.size > 25 * 1024 * 1024) {
    uploadError.value = 'File exceeds 25MB limit'
    return
  }
  try {
    await uploadTemplate(file)
  } catch (e) {
    uploadError.value = e.message
  }
}

// Delete
const showDeleteDialog = ref(false)
const deleteTarget = ref('')

function confirmDelete(filename) {
  deleteTarget.value = filename
  showDeleteDialog.value = true
}

async function handleDelete() {
  try {
    await deleteTemplate(deleteTarget.value)
    showDeleteDialog.value = false
  } catch (e) {
    showDeleteDialog.value = false
  }
}

onMounted(fetchTemplates)

defineExpose({ fetchTemplates })
</script>

<style scoped>
.upload-area {
  border: 2px dashed rgba(0, 0, 0, 0.2);
  cursor: pointer;
  transition: all 0.2s ease;
}

.upload-area:hover,
.upload-area--dragging {
  border-color: rgb(var(--v-theme-primary));
  background-color: rgba(var(--v-theme-primary), 0.04);
}
</style>
