import { createApp } from 'vue'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import './assets/global.css'
import App from './App.vue'
import router from './router'

const vuetify = createVuetify({
  components,
  directives,
  defaults: {
    VCard: {
      rounded: 'lg',
      elevation: 2,
      variant: 'elevated',
    },
    VBtn: {
      rounded: 'pill',
    },
    VTextField: {
      rounded: 'lg',
      variant: 'outlined',
    },
    VSelect: {
      rounded: 'lg',
      variant: 'outlined',
    },
    VTextarea: {
      rounded: 'lg',
      variant: 'outlined',
    },
    VChip: {
      rounded: 'pill',
    },
    VAlert: {
      rounded: 'lg',
    },
    VDialog: {
      VCard: {
        rounded: 'xl',
      },
    },
    VExpansionPanels: {
      rounded: 'lg',
    },
    VExpansionPanel: {
      rounded: 'lg',
    },
  },
  theme: {
    defaultTheme: 'light',
    themes: {
      light: {
        colors: {
          primary: '#1A7F64',
          secondary: '#5CBBFF',
          error: '#E53935',
          warning: '#F9A825',
          success: '#43A047',
          background: '#F4F5F7',
          surface: '#FFFFFF',
        },
      },
      dark: {
        colors: {
          primary: '#1A7F64',
          secondary: '#5CBBFF',
          error: '#E53935',
          warning: '#F9A825',
          success: '#43A047',
          background: '#121212',
          surface: '#1E1E1E',
        },
      },
    },
  },
})

const app = createApp(App)
app.use(vuetify)
app.use(router)
app.mount('#app')
