import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import basicSsl from '@vitejs/plugin-basic-ssl'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const remoteTarget = env.VITE_REMOTE_SALTMINER || 'https://qatracking.saltminer.io'

  return {
    plugins: [vue(), basicSsl()],
    base: '/smui4/',
    server: {
      https: true,
      proxy: {
        // Local Flask API
        '/smuiapi4': {
          target: 'http://localhost:5001',
          changeOrigin: true,
        },
        // Everything else → remote SaltMiner instance
        // This catches Kibana, legacy GUI, auth, etc.
        '^/(?!smui4|smuiapi4)': {
          target: remoteTarget,
          changeOrigin: true,
          secure: false,
          cookieDomainRewrite: 'localhost',
          headers: {
            'X-Forwarded-Proto': 'https',
          },
        },
      },
    },
  }
})
