<template>
  <v-card class="pa-6" height="100%">
    <div class="d-flex justify-space-between align-start mb-4">
      <v-avatar size="56" rounded="lg" color="grey-lighten-4">
        <img
          :src="iconSrc"
          width="28"
          height="28"
          @error="$event.target.src = '/smui4/icons/integrations/default.svg'"
        />
      </v-avatar>
      <slot name="top-right" />
    </div>

    <div v-if="subtitle" class="text-body-2 text-medium-emphasis mb-2">
      {{ subtitle }}
    </div>

    <h3 class="text-h6 mb-1">{{ name }}</h3>
    <p class="text-body-2 text-medium-emphasis mb-4">{{ description }}</p>

    <slot name="actions" />
  </v-card>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  name: { type: String, required: true },
  description: { type: String, default: '' },
  icon: { type: String, default: '/smui4/icons/integrations/default.svg' },
  subtitle: { type: String, default: '' },
})

const iconSrc = computed(() => props.icon || '/smui4/icons/integrations/default.svg')
</script>
