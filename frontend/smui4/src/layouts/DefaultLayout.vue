<template>
  <v-layout>
    <!-- Sidebar -->
    <v-navigation-drawer
      v-model="drawer"
      :rail="rail"
      :temporary="mobile"
    >
      <!-- Logo + App Name -->
      <v-list-item
        class="pa-4"
        :prepend-icon="rail ? 'mdi-shield-check' : undefined"
      >
        <template v-if="!rail" #prepend>
          <v-icon color="primary" size="32" class="mr-3">mdi-shield-check</v-icon>
        </template>
        <v-list-item-title class="text-h6 font-weight-bold">
          SaltMiner
        </v-list-item-title>
      </v-list-item>

      <v-divider />

      <!-- Navigation -->
      <v-list density="compact" nav>
        <!-- Dashboards -->
        <v-list-group value="dashboards">
          <template #activator="{ props }">
            <v-tooltip :text="'Dashboards'" location="end" :disabled="!rail">
              <template #activator="{ props: tp }">
                <v-list-item v-bind="{ ...props, ...tp }" prepend-icon="mdi-view-dashboard" title="Dashboards" @click="router.push({ name: 'dashboards-overview' })" />
              </template>
            </v-tooltip>
          </template>
          <v-list-item
            title="Executive"
            :to="{ name: 'dashboard-executive' }"
          />
          <v-list-item
            title="Development"
            :to="{ name: 'dashboard-development' }"
          />
          <v-list-item
            title="k-Development"
            :to="{ name: 'dashboard-k-development' }"
          />
          <v-list-item
            title="Security"
            :to="{ name: 'dashboard-security' }"
          />
          <v-list-item
            title="Operations"
            :to="{ name: 'dashboard-operations' }"
          />
          <v-list-item
            title="Kibana"
            href="/"
            target="_blank"
            prepend-icon="mdi-open-in-new"
          />
        </v-list-group>

        <!-- Integrations -->
        <v-list-group value="integrations">
          <template #activator="{ props }">
            <v-tooltip :text="'Integrations'" location="end" :disabled="!rail">
              <template #activator="{ props: tp }">
                <v-list-item v-bind="{ ...props, ...tp }" prepend-icon="mdi-puzzle" title="Integrations" @click="router.push({ name: 'integrations-overview' })" />
              </template>
            </v-tooltip>
          </template>
          <v-list-item
            title="Configured"
            :to="{ name: 'integrations-configured' }"
          />
          <v-list-item
            title="Available"
            :to="{ name: 'integrations-available' }"
          />
        </v-list-group>

        <!-- Scanning -->
        <v-list-group value="scanning">
          <template #activator="{ props }">
            <v-tooltip :text="'Scanning'" location="end" :disabled="!rail">
              <template #activator="{ props: tp }">
                <v-list-item v-bind="{ ...props, ...tp }" prepend-icon="mdi-radar" title="Scanning" @click="router.push({ name: 'scanning-overview' })" />
              </template>
            </v-tooltip>
          </template>
          <v-list-item
            title="Jobs"
            :to="{ name: 'scanning-jobs' }"
          />
          <v-list-item
            title="Schedule"
            :to="{ name: 'scanning-schedule' }"
          />
          <v-list-item
            title="Scanners"
            :to="{ name: 'scanning-scanners' }"
          />
        </v-list-group>

        <!-- PenTest — external link to existing system -->
        <v-tooltip :text="'PenTest'" location="end" :disabled="!rail">
          <template #activator="{ props: tp }">
            <v-list-item
              v-bind="tp"
              prepend-icon="mdi-security"
              title="PenTest"
              href="/s/pentest/"
            />
          </template>
        </v-tooltip>

        <!-- Inventory -->
        <v-tooltip :text="'Inventory'" location="end" :disabled="!rail">
          <template #activator="{ props: tp }">
            <v-list-item
              v-bind="tp"
              prepend-icon="mdi-package-variant-closed"
              title="Inventory"
              :to="{ name: 'inventory-assets' }"
            />
          </template>
        </v-tooltip>

        <!-- Settings -->
        <v-list-group value="settings">
          <template #activator="{ props }">
            <v-tooltip :text="'Settings'" location="end" :disabled="!rail">
              <template #activator="{ props: tp }">
                <v-list-item v-bind="{ ...props, ...tp }" prepend-icon="mdi-cog" title="Settings" @click="router.push({ name: 'settings' })" />
              </template>
            </v-tooltip>
          </template>
          <v-list-item
            title="Logs"
            :to="{ name: 'settings-logs' }"
          />
        </v-list-group>
      </v-list>

      <!-- Collapse toggle + Footer -->
      <template #append>
        <v-divider />
        <v-list-item
          v-if="!mobile"
          :icon="rail ? 'mdi-chevron-right' : 'mdi-chevron-left'"
          @click.stop="rail = !rail"
          class="my-1"
        />
        <div v-if="!rail" class="pa-4">
          <p class="text-caption text-medium-emphasis">v2.0.0</p>
          <p class="text-caption text-medium-emphasis">&copy; 2026 SaltMiner</p>
        </div>
      </template>
    </v-navigation-drawer>

    <!-- Top Header -->
    <v-app-bar flat border="b">
      <v-app-bar-nav-icon
        @click="onMenuClick"
      />

      <v-text-field
        class="mx-4"
        density="compact"
        variant="solo-filled"
        flat
        hide-details
        placeholder="Search"
        prepend-inner-icon="mdi-magnify"
        style="max-width: 576px;"
      />

      <v-spacer />

      <v-btn icon="mdi-help-circle-outline" variant="text" />
      <v-btn icon variant="text">
        <v-badge color="error" dot>
          <v-icon>mdi-bell-outline</v-icon>
        </v-badge>
      </v-btn>
      <v-btn
        :icon="isDark ? 'mdi-weather-sunny' : 'mdi-weather-night'"
        variant="text"
        @click="toggleDarkMode"
      />
      <v-menu offset-y>
        <template #activator="{ props }">
          <v-avatar v-bind="props" color="primary" size="32" class="ml-2 mr-4 cursor-pointer">
            <v-icon color="white" size="small">mdi-account</v-icon>
          </v-avatar>
        </template>
        <v-card min-width="220">
          <v-card-text class="pb-2">
            <div class="d-flex align-center mb-2">
              <v-avatar color="primary" size="40" class="mr-3">
                <v-icon color="white">mdi-account</v-icon>
              </v-avatar>
              <div>
                <div class="text-subtitle-2 font-weight-medium">{{ userName }}</div>
                <div v-if="userEmail" class="text-caption text-medium-emphasis">{{ userEmail }}</div>
              </div>
            </div>
          </v-card-text>
          <v-divider />
          <v-list density="compact" nav>
            <v-list-item
              prepend-icon="mdi-account-cog"
              title="Edit Profile"
              href="/s/saltminer/security/account"
              target="_blank"
            />
            <v-list-item
              prepend-icon="mdi-logout"
              title="Logout"
              href="/logout"
            />
          </v-list>
        </v-card>
      </v-menu>
    </v-app-bar>

    <!-- Main Content -->
    <v-main>
      <v-container fluid class="pa-6">
        <router-view />
      </v-container>
    </v-main>

    <!-- Not Authenticated Overlay -->
    <v-overlay
      :model-value="authChecked && !authenticated"
      persistent
      class="d-flex align-center justify-center"
      scrim="rgba(0,0,0,0.7)"
    >
      <v-card max-width="450" class="pa-8 text-center">
        <v-icon color="warning" size="64" class="mb-4">mdi-lock-alert</v-icon>
        <h2 class="text-h5 mb-2">Session Not Found</h2>
        <p class="text-body-1 text-medium-emphasis mb-6">
          You are not logged in or your session has expired. Please log in to continue using SaltMiner.
        </p>
        <v-btn
          color="primary"
          size="large"
          href="/"
          prepend-icon="mdi-login"
        >
          Log In
        </v-btn>
      </v-card>
    </v-overlay>
  </v-layout>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useDisplay, useTheme } from 'vuetify'
import apiClient from '../services/api.js'

const router = useRouter()
const { mobile } = useDisplay()
const theme = useTheme()
const drawer = ref(true)
const rail = ref(false)

// User info
const userName = ref('User')
const userEmail = ref('')
const authenticated = ref(false)
const authChecked = ref(false)

onMounted(async () => {
  try {
    const response = await apiClient.get('/auth/me')
    if (response.data?.authenticated) {
      authenticated.value = true
      userName.value = response.data.fullName || response.data.username || 'User'
      userEmail.value = response.data.email || ''
    }
  } catch {
    // Auth not available or failed
  }
  authChecked.value = true
})

const isDark = computed(() => theme.global.name.value === 'dark')

// Load dark mode preference from cookie on startup
const savedTheme = document.cookie.split('; ').find((c) => c.startsWith('sm_dark_mode='))
if (savedTheme) {
  theme.global.name.value = savedTheme.split('=')[1] === 'true' ? 'dark' : 'light'
}

function toggleDarkMode() {
  const newTheme = isDark.value ? 'light' : 'dark'
  theme.global.name.value = newTheme
  document.cookie = `sm_dark_mode=${newTheme === 'dark'}; path=/; max-age=${365 * 24 * 60 * 60}`
}

function onMenuClick() {
  if (mobile.value) {
    drawer.value = !drawer.value
  } else {
    rail.value = !rail.value
  }
}
</script>
