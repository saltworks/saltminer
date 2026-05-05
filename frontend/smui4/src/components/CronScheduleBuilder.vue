<template>
  <div>
    <v-row dense>
      <v-col cols="12" md="4">
        <v-select
          v-model="mode"
          :items="modeOptions"
          label="Schedule Mode"
          @update:model-value="onModeChange"
        />
      </v-col>

      <!-- Every N minutes -->
      <template v-if="mode === 'everyMinutes'">
        <v-col cols="12" md="4">
          <v-text-field
            v-model.number="everyN"
            type="number"
            min="1"
            max="59"
            label="Every N minutes"
            @update:model-value="emitChange"
          />
        </v-col>
      </template>

      <!-- Every N hours -->
      <template v-if="mode === 'everyHours'">
        <v-col cols="12" md="4">
          <v-text-field
            v-model.number="everyN"
            type="number"
            min="1"
            max="23"
            label="Every N hours"
            @update:model-value="emitChange"
          />
        </v-col>
      </template>

      <!-- Daily -->
      <template v-if="mode === 'daily'">
        <v-col cols="12" md="4">
          <v-text-field
            v-model="timeOfDay"
            type="time"
            label="At time (HH:MM)"
            @update:model-value="emitChange"
          />
        </v-col>
      </template>

      <!-- Weekly -->
      <template v-if="mode === 'weekly'">
        <v-col cols="12" md="4">
          <v-text-field
            v-model="timeOfDay"
            type="time"
            label="At time (HH:MM)"
            @update:model-value="emitChange"
          />
        </v-col>
        <v-col cols="12">
          <div class="text-caption text-medium-emphasis mb-1">On days:</div>
          <div class="d-flex flex-wrap gap-2">
            <v-chip
              v-for="day in weekDays"
              :key="day.value"
              :color="selectedDays.includes(day.value) ? 'primary' : undefined"
              :variant="selectedDays.includes(day.value) ? 'flat' : 'outlined'"
              size="small"
              @click="toggleDay(day.value)"
            >
              {{ day.label }}
            </v-chip>
          </div>
        </v-col>
      </template>

      <!-- Monthly -->
      <template v-if="mode === 'monthly'">
        <v-col cols="12" md="4">
          <v-text-field
            v-model="timeOfDay"
            type="time"
            label="At time (HH:MM)"
            @update:model-value="emitChange"
          />
        </v-col>
        <v-col cols="12" md="4">
          <v-text-field
            v-model.number="dayOfMonth"
            type="number"
            min="1"
            max="31"
            label="Day of month"
            @update:model-value="emitChange"
          />
        </v-col>
      </template>

      <!-- Custom -->
      <template v-if="mode === 'custom'">
        <v-col cols="12" md="8">
          <v-text-field
            v-model="customCron"
            label="Cron expression (Quartz format)"
            hint="7 fields: second minute hour day-of-month month day-of-week year"
            persistent-hint
            @update:model-value="emitChange"
          />
        </v-col>
      </template>
    </v-row>

    <v-alert type="info" variant="tonal" density="compact" class="mt-2">
      <div class="d-flex align-center">
        <span class="text-body-2 mr-2">Generated schedule:</span>
        <code class="text-body-2">{{ cronString }}</code>
      </div>
      <div class="text-caption text-medium-emphasis">{{ humanReadable }}</div>
    </v-alert>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'

const props = defineProps({
  modelValue: { type: String, default: '' },
})

const emit = defineEmits(['update:modelValue'])

const modeOptions = [
  { title: 'Every N minutes', value: 'everyMinutes' },
  { title: 'Every N hours', value: 'everyHours' },
  { title: 'Daily at time', value: 'daily' },
  { title: 'Weekly on days', value: 'weekly' },
  { title: 'Monthly on day', value: 'monthly' },
  { title: 'Custom (raw cron)', value: 'custom' },
]

const weekDays = [
  { value: 'SUN', label: 'Sun' },
  { value: 'MON', label: 'Mon' },
  { value: 'TUE', label: 'Tue' },
  { value: 'WED', label: 'Wed' },
  { value: 'THU', label: 'Thu' },
  { value: 'FRI', label: 'Fri' },
  { value: 'SAT', label: 'Sat' },
]

const mode = ref('daily')
const everyN = ref(5)
const timeOfDay = ref('00:00')
const selectedDays = ref(['MON'])
const dayOfMonth = ref(1)
const customCron = ref('')

function buildCron() {
  const [hh, mm] = timeOfDay.value.split(':').map((v) => parseInt(v, 10) || 0)
  switch (mode.value) {
    case 'everyMinutes':
      return `0 0/${everyN.value || 1} * * * ? *`
    case 'everyHours':
      return `0 0 0/${everyN.value || 1} * * ? *`
    case 'daily':
      return `0 ${mm} ${hh} 1/1 * ? *`
    case 'weekly': {
      const days = selectedDays.value.length > 0 ? selectedDays.value.join(',') : 'MON'
      return `0 ${mm} ${hh} ? * ${days} *`
    }
    case 'monthly':
      return `0 ${mm} ${hh} ${dayOfMonth.value || 1} * ? *`
    case 'custom':
      return customCron.value
  }
  return ''
}

const cronString = computed(() => buildCron())

const humanReadable = computed(() => {
  const [hh, mm] = timeOfDay.value.split(':')
  switch (mode.value) {
    case 'everyMinutes':
      return `Runs every ${everyN.value || 1} minute(s)`
    case 'everyHours':
      return `Runs every ${everyN.value || 1} hour(s)`
    case 'daily':
      return `Runs daily at ${hh}:${mm}`
    case 'weekly':
      return `Runs at ${hh}:${mm} on ${selectedDays.value.join(', ') || 'no days selected'}`
    case 'monthly':
      return `Runs on day ${dayOfMonth.value} of each month at ${hh}:${mm}`
    case 'custom':
      return customCron.value ? 'Custom expression' : 'No schedule'
  }
  return ''
})

function toggleDay(day) {
  const i = selectedDays.value.indexOf(day)
  if (i === -1) {
    selectedDays.value.push(day)
  } else {
    selectedDays.value.splice(i, 1)
  }
  emitChange()
}

function emitChange() {
  emit('update:modelValue', buildCron())
}

function onModeChange() {
  emitChange()
}

// Parse incoming cron string and populate fields
function parseCron(cron) {
  if (!cron || !cron.trim()) {
    mode.value = 'daily'
    return
  }
  const parts = cron.trim().split(/\s+/)
  if (parts.length < 6) {
    mode.value = 'custom'
    customCron.value = cron
    return
  }
  const [sec, min, hour, dom, month, dow] = parts

  // Every N minutes: 0 0/N * * * ? *
  const minEvery = min.match(/^0\/(\d+)$/)
  if (sec === '0' && minEvery && hour === '*' && dom === '*' && month === '*' && dow === '?') {
    mode.value = 'everyMinutes'
    everyN.value = parseInt(minEvery[1], 10)
    return
  }

  // Every N hours: 0 0 0/N * * ? *
  const hourEvery = hour.match(/^0\/(\d+)$/)
  if (sec === '0' && min === '0' && hourEvery && dom === '*' && month === '*' && dow === '?') {
    mode.value = 'everyHours'
    everyN.value = parseInt(hourEvery[1], 10)
    return
  }

  // Daily at HH:MM: 0 MM HH 1/1 * ? *
  if (sec === '0' && /^\d+$/.test(min) && /^\d+$/.test(hour) && dom === '1/1' && month === '*' && dow === '?') {
    mode.value = 'daily'
    timeOfDay.value = `${hour.padStart(2, '0')}:${min.padStart(2, '0')}`
    return
  }

  // Weekly: 0 MM HH ? * DAYS *
  if (sec === '0' && /^\d+$/.test(min) && /^\d+$/.test(hour) && dom === '?' && month === '*' && dow !== '?') {
    mode.value = 'weekly'
    timeOfDay.value = `${hour.padStart(2, '0')}:${min.padStart(2, '0')}`
    selectedDays.value = dow.split(',').filter((d) => weekDays.some((w) => w.value === d))
    if (selectedDays.value.length === 0) selectedDays.value = ['MON']
    return
  }

  // Monthly: 0 MM HH N * ? *
  if (sec === '0' && /^\d+$/.test(min) && /^\d+$/.test(hour) && /^\d+$/.test(dom) && month === '*' && dow === '?') {
    mode.value = 'monthly'
    timeOfDay.value = `${hour.padStart(2, '0')}:${min.padStart(2, '0')}`
    dayOfMonth.value = parseInt(dom, 10)
    return
  }

  mode.value = 'custom'
  customCron.value = cron
}

watch(
  () => props.modelValue,
  (newVal) => {
    if (newVal !== cronString.value) {
      parseCron(newVal)
    }
  },
  { immediate: true },
)
</script>
