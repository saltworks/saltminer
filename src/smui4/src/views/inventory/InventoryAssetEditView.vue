<template>
  <div>
    <v-btn
      variant="text"
      prepend-icon="mdi-arrow-left"
      class="mb-4"
      @click="router.push({ name: 'inventory-assets' })"
    >
      Back to Inventory Assets
    </v-btn>

    <h1 class="text-h4 mb-1">{{ isNew ? 'New Inventory Asset' : 'Edit Inventory Asset' }}</h1>

    <v-alert v-if="error" type="error" closable class="mt-4 mb-2" @click:close="error = null">
      {{ error }}
    </v-alert>

    <v-alert v-if="requiredAlert" type="warning" closable class="mt-4 mb-2" @click:close="requiredAlert = ''">
      {{ requiredAlert }}
    </v-alert>

    <div v-if="loading && !assetDetail" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate color="primary" />
    </div>

    <v-card v-else class="pa-6 mt-4">
      <!-- Standard fields -->
      <v-text-field
        v-if="!getFieldDef('key').isHidden"
        v-model="form.key"
        :label="getFieldDef('key').label || 'Key'"
        :readonly="getFieldDef('key').isReadOnly"
        :hint="getFieldDef('key').isRequired ? 'Required' : ''"
        persistent-hint
        class="mb-4"
      />
      <v-text-field
        v-if="!getFieldDef('name').isHidden"
        v-model="form.name"
        :label="getFieldDef('name').label || 'Name'"
        :readonly="getFieldDef('name').isReadOnly"
        :hint="getFieldDef('name').isRequired ? 'Required' : ''"
        persistent-hint
        class="mb-4"
      />
      <v-textarea
        v-if="!getFieldDef('description').isHidden"
        v-model="form.description"
        :label="getFieldDef('description').label || 'Description'"
        :readonly="getFieldDef('description').isReadOnly"
        rows="3"
        class="mb-4"
      />
      <v-text-field
        v-if="!getFieldDef('version').isHidden"
        v-model="form.version"
        :label="getFieldDef('version').label || 'Version'"
        :readonly="getFieldDef('version').isReadOnly"
        class="mb-4"
      />
      <div v-if="!getFieldDef('isProduction').isHidden" class="d-flex align-center mb-6">
        <v-switch
          v-model="form.isProduction"
          :label="getFieldDef('isProduction').label || 'Is Production'"
          :readonly="getFieldDef('isProduction').isReadOnly"
          color="primary"
          hide-details
          inset
        />
      </div>

      <!-- Attributes (saltminer section) -->
      <template v-if="saltminerAttrDefs.length > 0">
        <v-divider class="mb-4" />
        <h3 class="text-subtitle-1 font-weight-medium mb-4">Attributes</h3>

        <template v-for="attrDef in visibleSaltminerAttrDefs" :key="attrDef.name">
          <!-- Single line text -->
          <v-text-field
            v-if="attributeField(attrDef.type) === 'string'"
            v-model="form.attributes.saltminer[attrDef.name]"
            :label="getAttrCustomization(attrDef.name).label || attrDef.display"
            :readonly="getAttrCustomization(attrDef.name).isReadOnly"
            :hint="getAttrCustomization(attrDef.name).isRequired ? 'Required' : ''"
            persistent-hint
            class="mb-4"
          />

          <!-- Number -->
          <v-text-field
            v-else-if="attributeField(attrDef.type) === 'number'"
            v-model="form.attributes.saltminer[attrDef.name]"
            :label="getAttrCustomization(attrDef.name).label || attrDef.display"
            :readonly="getAttrCustomization(attrDef.name).isReadOnly"
            type="number"
            :step="attrDef.type === 'Integer (long)' ? '1' : 'any'"
            class="mb-4"
          />

          <!-- Multi-line text -->
          <v-textarea
            v-else-if="attributeField(attrDef.type) === 'text'"
            v-model="form.attributes.saltminer[attrDef.name]"
            :label="getAttrCustomization(attrDef.name).label || attrDef.display"
            :readonly="getAttrCustomization(attrDef.name).isReadOnly"
            rows="3"
            class="mb-4"
          />

          <!-- Date -->
          <v-text-field
            v-else-if="attributeField(attrDef.type) === 'date'"
            v-model="form.attributes.saltminer[attrDef.name]"
            :label="getAttrCustomization(attrDef.name).label || attrDef.display"
            :readonly="getAttrCustomization(attrDef.name).isReadOnly"
            type="date"
            class="mb-4"
          />

          <!-- Single select -->
          <v-select
            v-else-if="attributeField(attrDef.type) === 'select'"
            v-model="form.attributes.saltminer[attrDef.name]"
            :label="getAttrCustomization(attrDef.name).label || attrDef.display"
            :readonly="getAttrCustomization(attrDef.name).isReadOnly"
            :items="attrDef.options || []"
            clearable
            class="mb-4"
          />

          <!-- Multi-select -->
          <v-select
            v-else-if="attributeField(attrDef.type) === 'multiselect'"
            v-model="form.attributes.saltminer[attrDef.name]"
            :label="getAttrCustomization(attrDef.name).label || attrDef.display"
            :readonly="getAttrCustomization(attrDef.name).isReadOnly"
            :items="attrDef.options || []"
            multiple
            chips
            clearable
            class="mb-4"
          />
        </template>
      </template>

      <!-- Additional attributes (non-saltminer, read-only display) -->
      <template v-if="additionalSections.length > 0">
        <v-divider class="mb-4" />
        <h3 class="text-subtitle-1 font-weight-medium mb-4">Additional Attributes</h3>
        <div
          v-for="section in additionalSections"
          :key="section.name"
          class="mb-4"
        >
          <div class="text-overline">{{ section.name }}</div>
          <v-list density="compact">
            <v-list-item
              v-for="attr in section.attrs"
              :key="attr.name"
            >
              <template #title>
                <span class="text-body-2 text-medium-emphasis">{{ attr.label || attr.name }}</span>
              </template>
              <template #subtitle>
                <span class="text-body-2">{{ attr.value ?? '—' }}</span>
              </template>
            </v-list-item>
          </v-list>
        </div>
      </template>

      <v-divider class="mb-4" />
      <div class="d-flex gap-4">
        <v-btn color="primary" :loading="loading" @click="handleSave">Save</v-btn>
        <v-btn variant="text" @click="router.push({ name: 'inventory-assets' })">Cancel</v-btn>
      </div>
    </v-card>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useInventoryAssets } from '../../composables/useInventoryAssets.js'

const props = defineProps({
  id: { type: String, default: '' },
})

const router = useRouter()
const route = useRoute()
const {
  assetDetail,
  attributeDefinitions,
  loading,
  error,
  fetchAssetForEdit,
  fetchCreatePrimer,
  saveAsset,
} = useInventoryAssets()

const isNew = computed(() => !props.id && route.name === 'inventory-asset-create')

const requiredAlert = ref('')

const form = reactive({
  id: null,
  key: '',
  name: '',
  description: '',
  version: '',
  isProduction: false,
  attributes: { saltminer: {} },
})

// All other attribute sections (non-saltminer) kept as-is to send back
const otherSections = ref({})

// --- Type mapping ---
const ATTRIBUTE_TYPES = [
  { field: 'string', types: ['Single line text (text)'] },
  { field: 'text', types: ['Multi line text (text)', 'Markdown (text)'] },
  { field: 'number', types: ['Integer (long)', 'Number (double)'] },
  { field: 'date', types: ['Date (date)'] },
  { field: 'select', types: ['Single select drop down'] },
  { field: 'multiselect', types: ['Multi select drop down'] },
]

function attributeField(type) {
  return ATTRIBUTE_TYPES.find((t) => t.types.includes(type))?.field || 'string'
}

// --- Field defs ---
// For standard fields like key, name, description etc., the API returns
// a def object. We store the original structure from the server in
// fieldDefs and the user-editable value in form.
const fieldDefs = ref({})

function getFieldDef(name) {
  return fieldDefs.value[name] || {}
}

// --- Attribute customizations (saltminer section) ---
// Each attribute customization has the metadata (label, readOnly, isRequired, etc.)
const attributeCustomizations = ref([])

function getAttrCustomization(name) {
  return (
    attributeCustomizations.value.find((a) => a.name === name) || {
      label: name,
      isReadOnly: false,
      isRequired: false,
      isHidden: false,
    }
  )
}

const saltminerAttrDefs = computed(() =>
  attributeDefinitions.value.filter((d) => d.section === 'saltminer'),
)

const visibleSaltminerAttrDefs = computed(() =>
  saltminerAttrDefs.value.filter((d) => !getAttrCustomization(d.name).isHidden),
)

// Additional attribute sections (non-saltminer) — displayed read-only
const additionalSections = computed(() => {
  return Object.entries(otherSections.value).map(([name, attrs]) => ({
    name,
    attrs,
  }))
})

// --- Populate form from server data ---
function populateForm(assetData) {
  if (!assetData) return

  fieldDefs.value = {}
  const flatValues = {}
  for (const [key, val] of Object.entries(assetData)) {
    if (key === 'attributes') continue
    if (val && typeof val === 'object' && 'value' in val) {
      fieldDefs.value[key] = val
      flatValues[key] = val.value ?? val.defaultValue ?? ''
    } else {
      flatValues[key] = val
    }
  }

  form.id = flatValues.id ?? null
  form.key = flatValues.key ?? ''
  form.name = flatValues.name ?? ''
  form.description = flatValues.description ?? ''
  form.version = flatValues.version ?? ''
  form.isProduction = !!flatValues.isProduction

  // Attributes
  const saltminerValues = {}
  attributeCustomizations.value = []
  otherSections.value = {}

  const allAttrs = assetData.attributes || {}
  for (const [section, list] of Object.entries(allAttrs)) {
    if (!Array.isArray(list)) continue
    if (section === 'saltminer') {
      attributeCustomizations.value = list
      for (const attr of list) {
        const def = attributeDefinitions.value.find((d) => d.name === attr.name)
        const field = def ? attributeField(def.type) : 'string'
        let rawValue = attr.value
        if (rawValue === null || rawValue === undefined || rawValue === '') {
          rawValue = attr.defaultValue
        }
        if (field === 'multiselect') {
          if (typeof rawValue === 'string' && rawValue.startsWith('[')) {
            try {
              saltminerValues[attr.name] = JSON.parse(rawValue)
            } catch {
              saltminerValues[attr.name] = []
            }
          } else if (Array.isArray(rawValue)) {
            saltminerValues[attr.name] = rawValue
          } else {
            saltminerValues[attr.name] = []
          }
        } else {
          saltminerValues[attr.name] = rawValue ?? ''
        }
      }
    } else {
      otherSections.value[section] = list
    }
  }

  form.attributes.saltminer = saltminerValues
}

// --- Save ---
function checkRequired() {
  const missing = []
  for (const name of ['key', 'name']) {
    const def = getFieldDef(name)
    if (def.isRequired && !form[name]) {
      missing.push(def.label || name)
    }
  }
  for (const attrDef of saltminerAttrDefs.value) {
    const cust = getAttrCustomization(attrDef.name)
    if (!cust.isRequired) continue
    const val = form.attributes.saltminer[attrDef.name]
    if (val === null || val === undefined || val === '' || (Array.isArray(val) && val.length === 0)) {
      missing.push(cust.label || attrDef.name)
    }
  }
  return missing
}

async function handleSave() {
  requiredAlert.value = ''
  const missing = checkRequired()
  if (missing.length > 0) {
    requiredAlert.value = `Required: ${missing.join(', ')}`
    return
  }

  // Build attributes payload
  const saltminerOut = {}
  for (const attrDef of saltminerAttrDefs.value) {
    const val = form.attributes.saltminer[attrDef.name]
    if (attributeField(attrDef.type) === 'multiselect') {
      saltminerOut[attrDef.name] = JSON.stringify(val || [])
    } else {
      saltminerOut[attrDef.name] = val ?? ''
    }
  }

  const payload = {
    key: form.key,
    name: form.name,
    description: form.description,
    version: form.version || '',
    isProduction: form.isProduction ?? false,
    attributes: {
      ...otherSections.value,
      saltminer: saltminerOut,
    },
  }
  if (form.id) payload.id = form.id

  try {
    await saveAsset(payload)
    router.push({ name: 'inventory-assets' })
  } catch {
    // error set by composable
  }
}

watch(assetDetail, (v) => populateForm(v), { immediate: false })

onMounted(async () => {
  if (isNew.value) {
    await fetchCreatePrimer()
  } else {
    await fetchAssetForEdit(props.id)
  }
})
</script>
