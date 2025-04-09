export default {
  // Target: https://go.nuxtjs.dev/config-target
  target: 'static',
  ssr: false,

  generate: {
    fallback: true,
    dir: 'dist/smpgui',
  },
  // Global page headers: https://go.nuxtjs.dev/config-head
  head: {
    title: 'saltworks',
    htmlAttrs: {
      lang: 'en',
    },
    meta: [
      { charset: 'utf-8' },
      { name: 'viewport', content: 'width=device-width, initial-scale=1' },
      { hid: 'description', name: 'description', content: '' },
      { name: 'format-detection', content: 'telephone=no' },
    ],
    link: [{ rel: 'icon', type: 'image/x-icon', href: '/smpgui/favicon.ico' }],
  },

  // Global CSS: https://go.nuxtjs.dev/config-css
  css: ['~/assets/scss/app.scss', '~/assets/scss/libs/tui.scss'],

  // Plugins to run before rendering page: https://go.nuxtjs.dev/config-plugins
  plugins: [
    { src: '~/plugins/directives.js', mode: 'client' },
    '~plugins/tui-editor.client.js',
    '~plugins/axios.js',
    '~plugins/handle-error-response.js'
  ],

  // Auto import components: https://go.nuxtjs.dev/config-components
  components: true,

  // Modules for dev and build (recommended): https://go.nuxtjs.dev/config-modules
  buildModules: [
    // https://go.nuxtjs.dev/eslint
    '@nuxtjs/eslint-module',
    '@nuxtjs/svg',
    '@nuxtjs/dotenv',
  ],

  // Modules: https://go.nuxtjs.dev/config-modules
  modules: [
    // https://go.nuxtjs.dev/axios
    '@nuxtjs/axios',
    '@nuxtjs/auth-next',
    '@nuxtjs/style-resources',
    '@tui-nuxt/editor',
  ],

  tui: {
    editor: {
      stylesheet: {
        editor: '@toast-ui/editor/dist/toastui-editor.css',
        // contents: '~/assets/scss/libs/tui.scss',
        // codemirror: 'codemirror/lib/codemirror.css',
        // codeHighlight: 'highlight.js/styles/github.css',
        // colorPicker: 'tui-color-picker/dist/tui-color-picker.min.css',
      },
    },
  },

  styleResources: {
    scss: ['./assets/scss/_imports.scss'],
  },

  // Axios module configuration: https://go.nuxtjs.dev/config-axios
  axios: {
    // Workaround to avoid enforcing hard-coded localhost:3000: https://github.com/nuxt-community/axios-module/issues/308
    baseURL: '/',
  },
  //
  router: {
    base: '/smpgui/',
    middleware: ['track-referrer']
  },
  // Build Configuration: https://go.nuxtjs.dev/config-build
  build: {},
}
