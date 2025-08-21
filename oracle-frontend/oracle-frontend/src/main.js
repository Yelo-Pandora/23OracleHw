import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from '@/router'

// 创建 Vue 应用实例
const app = createApp(App)
// 创建Pinia实例
const pinia = createPinia()

app.use(pinia)
app.use(router)

// 挂载应用
app.mount('#app')
