<template>
  <DashboardLayout>
    <div class="page-container">
      <div class="page-header">
        <h1>新增店面区域</h1>
        <p>创建一个新的可供租赁的店面区域。</p>
      </div>

      <form @submit.prevent="createArea" class="form-card">
        <div class="form-grid">
          <div class="form-group">
            <label>区域 ID</label>
            <input type="number" v-model.number="areaId" min="1" required placeholder="请输入唯一的区域ID" />
          </div>
          <div class="form-group">
            <label>区域面积 (㎡)</label>
            <input type="number" v-model.number="areaSize" min="1" required placeholder="例如：100" />
          </div>
          <div class="form-group">
            <label>基础租金 (元/月)</label>
            <input type="number" v-model.number="baseRent" min="0.01" step="0.01" required placeholder="例如：5000" />
          </div>
          <div class="form-group">
            <label>操作员账号</label>
            <input type="text" v-model="operatorAccount" placeholder="管理员账号" />
          </div>
        </div>

        <div class="actions">
          <button type="submit" class="btn-primary" :disabled="submitting">
            {{ submitting ? '创建中...' : '创建店面区域' }}
          </button>
        </div>

        <div v-if="loading" class="form-message info">加载中...</div>
        <div v-if="error" class="form-message error">{{ error }}</div>
        <div v-if="success" class="form-message success">{{ success }}</div>
      </form>
    </div>
  </DashboardLayout>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'
import DashboardLayout from '@/components/BoardLayout.vue'

const areaId = ref(null)
const areaSize = ref(100)
const baseRent = ref(1000)
const operatorAccount = ref('admin')

const loading = ref(false)
const submitting = ref(false)
const error = ref('')
const success = ref('')

async function createArea() {
  error.value = ''
  success.value = ''

  if (!areaId.value || areaId.value <= 0) {
    error.value = '区域ID必须为正整数'
    return
  }
  if (!areaSize.value || areaSize.value <= 0) {
    error.value = '区域面积必须大于0'
    return
  }
  if (baseRent.value == null || baseRent.value <= 0) {
    error.value = '基础租金必须大于0'
    return
  }

  submitting.value = true
  loading.value = true
  try {
    const dto = {
      AreaId: areaId.value,
      AreaSize: areaSize.value,
      BaseRent: baseRent.value,
      OperatorAccount: operatorAccount.value
    }

    const res = await axios.post('/api/Store/CreateRetailArea', dto)
    success.value = res.data?.message || '创建成功'
    // 显示返回的区域信息（可选）
    if (res.data) {
      success.value += `：区域ID ${res.data.areaId}，状态 ${res.data.rentStatus}`
    }
  } catch (e) {
    console.error(e)
    error.value = e?.response?.data?.error || e?.response?.data?.details || '创建失败'
  } finally {
    submitting.value = false
    loading.value = false
  }
}
</script>

<style scoped>
:root {
  --primary-color: #1abc9c;
  --card-bg: #ffffff;
  --card-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  --border-radius: 12px;
  --input-border-color: #dee2e6;
}

.page-container {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.page-header h1 {
  font-size: 24px;
  font-weight: 600;
  margin-bottom: 4px;
}

.page-header p {
  font-size: 14px;
  color: #7f8c8d;
}

.form-card {
  background-color: var(--card-bg);
  border-radius: var(--border-radius);
  box-shadow: var(--card-shadow);
  padding: 24px;
  max-width: 800px;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.form-group label {
  font-weight: 500;
  font-size: 14px;
}

.form-group input {
  padding: 10px 12px;
  border: 1px solid var(--input-border-color);
  border-radius: 8px;
}

.actions {
  margin-top: 24px;
}

.btn-primary {
  padding: 10px 20px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
  background-color: var(--primary-color);
  color: white;
  transition: background-color 0.2s;
}
.btn-primary:hover { background-color: #16a085; }
.btn-primary:disabled { background-color: #a3e9a4; cursor: not-allowed; }

.form-message {
  margin-top: 16px;
  padding: 12px;
  border-radius: 8px;
  font-size: 14px;
}
.form-message.error { color: #e74c3c; background-color: #fbeae5; }
.form-message.success { color: #27ae60; background-color: #e8f8f5; }
.form-message.info { color: #3498db; background-color: #eaf4fb; }
</style>
