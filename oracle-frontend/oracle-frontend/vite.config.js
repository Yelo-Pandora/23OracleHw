import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-vue';
//1.从'url'模块中导入fileURLToPath和URL
import { fileURLToPath, URL } from 'url';

export default defineConfig({
  plugins: [plugin()],
  server: {
    port: 8080,
    proxy: {
      '/api': {
        target: 'http://localhost:8081', // 后端服务地址
        changeOrigin: true,               // 允许跨域
      }
    }
  },
  resolve: {
    alias: {
      // 使用import.meta.url和fileURLToPath来定义别名
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  }
})
