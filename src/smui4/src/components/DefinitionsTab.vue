<template>
  <div>
    <h2 class="text-h5 mb-6">Definitions</h2>

    <v-row>
      <v-col
        v-for="card in cards"
        :key="card.key"
        cols="12"
        md="6"
        lg="4"
      >
        <v-card
          class="pa-6 cursor-pointer"
          :class="{ 'border-primary': activeCard === card.key }"
          @click="toggleCard(card.key)"
        >
          <div class="d-flex align-center mb-2">
            <v-icon :color="card.color" size="32" class="mr-3">{{ card.icon }}</v-icon>
            <div>
              <h3 class="text-subtitle-1 font-weight-medium">{{ card.title }}</h3>
              <p class="text-body-2 text-medium-emphasis">{{ card.description }}</p>
            </div>
          </div>
          <v-chip v-if="!card.active" size="x-small" variant="tonal" color="grey">Coming Soon</v-chip>
        </v-card>
      </v-col>
    </v-row>

    <!-- Expanded editor area -->
    <div v-if="activeCard" class="mt-6">
      <v-btn
        variant="text"
        prepend-icon="mdi-arrow-left"
        class="mb-4"
        @click="activeCard = null"
      >
        Back to Definitions
      </v-btn>

      <PenTestRolesEditor v-if="activeCard === 'pentest-roles'" ref="rolesEditor" />
      <CustomFieldDefsEditor v-if="activeCard === 'custom-fields'" ref="fieldsEditor" />
      <LookupsEditor v-if="activeCard === 'lookups'" ref="lookupsEditor" />

      <v-card v-if="activeCard === 'custom-attributes'" class="pa-6 text-center text-medium-emphasis">
        <v-icon size="48" class="mb-2">mdi-tag-multiple</v-icon>
        <p>Custom Attribute Definitions — coming soon.</p>
      </v-card>
      <v-card v-if="activeCard === 'search-filters'" class="pa-6 text-center text-medium-emphasis">
        <v-icon size="48" class="mb-2">mdi-filter-cog</v-icon>
        <p>Search Filter Definitions — coming soon.</p>
      </v-card>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import PenTestRolesEditor from './PenTestRolesEditor.vue'
import CustomFieldDefsEditor from './CustomFieldDefsEditor.vue'
import LookupsEditor from './LookupsEditor.vue'

const activeCard = ref(null)
const rolesEditor = ref(null)
const fieldsEditor = ref(null)
const lookupsEditor = ref(null)

const cards = [
  {
    key: 'pentest-roles',
    title: 'PenTest Roles',
    description: 'Manage field permissions and actions for PenTest roles',
    icon: 'mdi-shield-account',
    color: 'primary',
    active: true,
  },
  {
    key: 'custom-fields',
    title: 'User Defined Fields (Attributes)',
    description: 'Define custom fields for engagements, issues, and assets',
    icon: 'mdi-form-textbox',
    color: 'info',
    active: true,
  },
  {
    key: 'custom-attributes',
    title: 'Custom Attribute Definitions',
    description: 'Define custom attributes for inventory and assets',
    icon: 'mdi-tag-multiple',
    color: 'warning',
    active: false,
  },
  {
    key: 'lookups',
    title: 'Lookups',
    description: 'Manage dropdown options and reference data for lookup types',
    icon: 'mdi-book-search',
    color: 'success',
    active: true,
  },
  {
    key: 'search-filters',
    title: 'Search Filter Definitions',
    description: 'Configure search filters for data views',
    icon: 'mdi-filter-cog',
    color: 'secondary',
    active: false,
  },
]

function toggleCard(key) {
  const card = cards.find((c) => c.key === key)
  if (!card.active) return
  activeCard.value = activeCard.value === key ? null : key
}

function refresh() {
  if (activeCard.value === 'pentest-roles' && rolesEditor.value) {
    rolesEditor.value.fetchRoles()
  }
  if (activeCard.value === 'custom-fields' && fieldsEditor.value) {
    fieldsEditor.value.fetchDefinitions()
  }
  if (activeCard.value === 'lookups' && lookupsEditor.value) {
    lookupsEditor.value.fetchLookups()
  }
}

defineExpose({ refresh })
</script>
