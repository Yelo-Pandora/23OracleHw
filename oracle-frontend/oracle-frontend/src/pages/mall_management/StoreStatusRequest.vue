<template>
  <DashboardLayout>
    <div class="page-container">
      <div class="page-header">
        <h1>店面状态变更申请</h1>
        <p>提交店铺状态变更申请（如退租、维修、暂停营业等）。</p>
      </div>

      <div class="content-grid">
        <div class="form-card">
          <div class="form-group">
            <label>选择店面</label>
            <select v-model="form.storeId" @change="onStoreChange">
              <option value="">请选择店面</option>
              <option v-for="store in stores" :key="store.STORE_ID" :value="store.STORE_ID">
                {{ store.STORE_ID }} - {{ store.STORE_NAME }}
              </option>
            </select>
          </div>
          <div class="form-group">
            <label>变更类型</label>
            <select v-model="form.changeType" @change="updateTargetStatus">
              <option value="">请选择变更类型</option>
              <option v-for="type in allowedTypes" :key="type" :value="type">{{ getChangeTypeLabel(type) }}</option>
            </select>
          </div>
          <div class="form-group">
            <label>目标状态</label>
            <input type="text" v-model="form.targetStatus" readonly placeholder="根据变更类型自动确定" />
          </div>
          <div class="form-group">
            <label>申请原因</label>
            <textarea v-model="form.reason" placeholder="请详细说明状态变更的原因..."></textarea>
          </div>
          <div class="form-group">
            <label>申请人账号</label>
            <input type="text" v-model="form.applicantAccount" :placeholder="accountPlaceholder" />
          </div>
          <div class="actions">
            <button class="btn-primary" @click="submitRequest" :disabled="submitting">{{ submitting ? '提交中...' : '提交申请' }}</button>
            <button class="btn-secondary" @click="refreshStoreStatus" :disabled="!form.storeId">查询状态</button>
            <button class="btn-secondary" @click="resetForm">重置</button>
          </div>
        </div>

        <div class="info-sidebar">
          <div v-if="storeStatus" class="info-card">
            <h4>当前店面状态</h4>
            <p><strong>店铺名称：</strong>{{ storeStatus.storeName }}</p>
            <p><strong>当前状态：</strong>{{ storeStatus.currentStatus }}</p>
            <p><strong>租户名称：</strong>{{ storeStatus.tenantName }}</p>
            <p><strong>有租用记录：</strong>{{ storeStatus.hasRentRecord ? '是' : '否' }}</p>
            <p><strong>可申请变更：</strong>{{ storeStatus.canApplyStatusChange ? '是' : '否' }}</p>
          </div>
          <div v-if="message" :class="['form-message', messageType]">
            {{ message }}
          </div>
        </div>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import axios from 'axios'
import { useUserStore } from '@/stores/user'
import DashboardLayout from '@/components/BoardLayout.vue'

const userStore = useUserStore()

const stores = ref([])
const allowedTypes = ref([])
const storeStatus = ref(null)
const message = ref('')
const messageType = ref('info')
const submitting = ref(false)

const form = ref({
  storeId: '',
  changeType: '',
  reason: '',
  targetStatus: '',
  applicantAccount: ''
})

const accountPlaceholder = computed(() => {
  return userStore.userInfo?.account || userStore.userInfo?.username || '请输入申请人账号'
})

// 变更类型标签映射
const changeTypeLabels = {
  '退租': '退租申请',
  '维修': '维修申请',
  '暂停营业': '暂停营业申请',
  '恢复营业': '恢复营业申请'
}

function getChangeTypeLabel(type) {
  return changeTypeLabels[type] || type
}

// 加载用户关联的店铺列表（参照 StoreDetail.vue）
async function loadStores() {
  message.value = '正在加载店铺列表...'
  messageType.value = 'info'

  const account = userStore.userInfo?.account || userStore.token || ''
  const role = userStore.role || ''

  if (!account) {
    stores.value = []
    message.value = '未检测到账户信息，无法加载店铺'
    messageType.value = 'info'
    return
  }

  try {
    if (role === '商户') {
      // 尝试通过 GetMyRentBills 获取商户相关的 storeId
      try {
        const r = await axios.get('/api/Store/GetMyRentBills', { params: { merchantAccount: account } })
        const storeIds = new Set()
        if (r.data) {
          if (r.data.storeId) storeIds.add(Number(r.data.storeId))
          if (Array.isArray(r.data.bills)) r.data.bills.forEach(b => { if (b.storeId) storeIds.add(Number(b.storeId)) })
        }

        if (storeIds.size > 0) {
          // 加载所有店铺并过滤（与 StoreDetail 保持一致）
          const all = await axios.get('/api/Store/AllStores')
          if (Array.isArray(all.data)) {
            stores.value = all.data.filter(s => storeIds.has(Number(s.STORE_ID) || Number(s.storeId)))
          }
        } else {
          // 兜底：通过账号->店铺映射查询
          try {
            const map = await axios.get('/api/Account/GetStoreAccountByAccount', { params: { account } })
            if (map.data && (map.data.STORE_ID || map.data.storeId)) {
              stores.value = [{ STORE_ID: map.data.STORE_ID || map.data.storeId, STORE_NAME: map.data.STORE_NAME || map.data.storeName || '' }]
            } else {
              stores.value = []
            }
          } catch (e) {
            stores.value = []
          }
        }
      } catch (e) {
        // 二次兜底：直接查询账号->店铺映射
        try {
          const map = await axios.get('/api/Account/GetStoreAccountByAccount', { params: { account } })
          if (map.data && (map.data.STORE_ID || map.data.storeId)) {
            stores.value = [{ STORE_ID: map.data.STORE_ID || map.data.storeId, STORE_NAME: map.data.STORE_NAME || map.data.storeName || '' }]
          } else {
            stores.value = []
          }
        } catch (e2) {
          console.error('商户店铺加载失败', e2)
          stores.value = []
        }
      }

      if (stores.value.length === 1) {
        form.value.storeId = Number(stores.value[0].STORE_ID || stores.value[0].storeId)
        await refreshStoreStatus()
      }

      message.value = `成功加载 ${stores.value.length} 个店铺`
      messageType.value = 'success'
      return
    }

    if (role === '员工') {
      try {
        const r = await axios.get('/api/Store/AllStores', { params: { operatorAccount: account } })
        if (Array.isArray(r.data)) stores.value = r.data
        message.value = `成功加载 ${stores.value.length} 个店铺`
        messageType.value = 'success'
        return
      } catch (e) {
        console.error('员工加载所有店铺失败', e)
        stores.value = []
        message.value = '加载店铺列表失败'
        messageType.value = 'error'
        return
      }
    }

    // 其它角色或未识别
    stores.value = []
    message.value = '未处理的用户角色，无法加载店铺'
    messageType.value = 'info'
  } catch (error) {
    console.error('加载店铺列表失败:', error)
    stores.value = []
    message.value = error.response?.data?.error || '加载店铺列表失败'
    messageType.value = 'error'
  }
}

// 查询店面状态
async function refreshStoreStatus() {
  if (!form.value.storeId) {
    message.value = '请先选择店面'
    messageType.value = 'error'
    return
  }

  try {
    message.value = '正在查询店面状态...'
    messageType.value = 'info'

    const response = await axios.get(`/api/Store/StoreStatus/${form.value.storeId}`)
    const data = response.data

    storeStatus.value = data
    allowedTypes.value = data.allowedChangeTypes || []

    message.value = `查询完成：当前状态 ${data.currentStatus}`
    messageType.value = 'success'
  } catch (error) {
    console.error('查询店面状态失败:', error)
    storeStatus.value = null
    allowedTypes.value = []
    message.value = error.response?.data?.error || '查询店面状态失败'
    messageType.value = 'error'
  }
}

// 店面选择变化时自动查询状态
function onStoreChange() {
  if (form.value.storeId) {
    refreshStoreStatus()
  } else {
    storeStatus.value = null
    allowedTypes.value = []
    form.value.changeType = ''
    form.value.targetStatus = ''
  }
}

// 更新目标状态
function updateTargetStatus() {
  const statusMap = {
    '退租': '已退租',
    '维修': '维修中',
    '暂停营业': '暂停营业',
    '恢复营业': '正常营业'
  }

  form.value.targetStatus = statusMap[form.value.changeType] || ''
}

// 提交申请
async function submitRequest() {
  // 表单验证
  if (!form.value.storeId) {
    message.value = '请选择店面'
    messageType.value = 'error'
    return
  }

  if (!form.value.changeType) {
    message.value = '请选择变更类型'
    messageType.value = 'error'
    return
  }

  if (!form.value.reason.trim()) {
    message.value = '请填写申请原因'
    messageType.value = 'error'
    return
  }

  // 设置默认申请人账号
  if (!form.value.applicantAccount) {
    form.value.applicantAccount = userStore.userInfo?.account || userStore.userInfo?.username || ''
  }

  try {
    submitting.value = true
    message.value = '正在提交申请...'
    messageType.value = 'info'

    const requestData = {
      storeId: parseInt(form.value.storeId),
      changeType: form.value.changeType,
      reason: form.value.reason.trim(),
      targetStatus: form.value.targetStatus,
      applicantAccount: form.value.applicantAccount
    }

    const response = await axios.post('/api/Store/StatusChangeRequest', requestData)
    const data = response.data

    message.value = `✅ 申请提交成功！\n申请编号: ${data.applicationNo}\n店铺名称: ${data.storeName}\n变更类型: ${data.changeType}\n目标状态: ${data.targetStatus}\n当前状态: ${data.status}`
    messageType.value = 'success'

    // 重置表单
    resetForm()
  } catch (error) {
    console.error('提交申请失败:', error)
    message.value = error.response?.data?.error || '提交申请失败，请稍后重试'
    messageType.value = 'error'
  } finally {
    submitting.value = false
  }
}

// 重置表单
function resetForm() {
  form.value = {
    storeId: '',
    changeType: '',
    reason: '',
    targetStatus: '',
    applicantAccount: ''
  }
  storeStatus.value = null
  allowedTypes.value = []
}

onMounted(() => {
  loadStores()
})
</script>

<style scoped>
:root {
  --primary-color: #1abc9c;
  --secondary-color: #7f8c8d;
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

.page-header h1 { font-size: 24px; font-weight: 600; margin-bottom: 4px; }
.page-header p { font-size: 14px; color: var(--secondary-color); }

.content-grid {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 24px;
  align-items: start;
}

.form-card, .info-card {
  background-color: var(--card-bg);
  border-radius: var(--border-radius);
  box-shadow: var(--card-shadow);
  padding: 24px;
}

.info-sidebar {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 20px;
}
.form-group label { font-weight: 500; font-size: 14px; }
.form-group input, .form-group select, .form-group textarea {
  padding: 10px 12px;
  border: 1px solid var(--input-border-color);
  border-radius: 8px;
}
.form-group textarea { min-height: 100px; }

.actions {
  display: flex;
  gap: 12px;
  margin-top: 24px;
}
.btn-primary, .btn-secondary {
  padding: 10px 20px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
  transition: background-color 0.2s, transform 0.2s;
}
.btn-primary { background-color: var(--primary-color); color: white; }
.btn-primary:hover:not(:disabled) { 
  background-color: #16a085;
  transform: translateY(-2px);
}
.btn-primary:disabled { 
  background-color: #a3e9a4; 
  cursor: not-allowed; 
}
.btn-secondary { background-color: #ecf0f1; color: #34495e; }
.btn-secondary:hover:not(:disabled) { 
  background-color: #bdc3c7;
  transform: translateY(-2px);
}

.info-card h4 { font-size: 18px; margin-bottom: 16px; }
.info-card p { margin: 0 0 12px 0; }
.info-card p strong { color: #333; }

.form-message {
  padding: 16px;
  border-radius: 8px;
  font-size: 14px;
}
.form-message.success { color: #27ae60; background-color: #e8f8f5; }
.form-message.error { color: #e74c3c; background-color: #fbeae5; }
.form-message.info { color: #3498db; background-color: #eaf4fb; }

@media (max-width: 992px) {
  .content-grid {
    grid-template-columns: 1fr;
  }
}
</style>
