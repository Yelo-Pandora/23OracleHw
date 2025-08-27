<template>
  <DashboardLayout>
    <div class="store-status-request">
      <h2>店面状态变更申请</h2>
      <p>提交店铺状态变更申请（如退租、维修、暂停营业等）</p>

      <div class="form-section">
        <div class="form-group">
          <label>选择店面：</label>
          <select v-model="form.storeId" @change="onStoreChange">
            <option value="">请选择店面</option>
            <option v-for="store in stores" :key="store.STORE_ID" :value="store.STORE_ID">
              {{ store.STORE_ID }} - {{ store.STORE_NAME }}
            </option>
          </select>
        </div>

        <div class="form-group">
          <label>变更类型：</label>
          <select v-model="form.changeType" @change="updateTargetStatus">
            <option value="">请选择变更类型</option>
            <option v-for="type in allowedTypes" :key="type" :value="type">
              {{ getChangeTypeLabel(type) }}
            </option>
          </select>
        </div>

        <div class="form-group">
          <label>目标状态：</label>
          <input type="text" v-model="form.targetStatus" readonly placeholder="根据变更类型自动确定" />
        </div>

        <div class="form-group">
          <label>申请原因：</label>
          <textarea v-model="form.reason" placeholder="请详细说明状态变更的原因..." rows="4"></textarea>
        </div>

        <div class="form-group">
          <label>申请人账号：</label>
          <input type="text" v-model="form.applicantAccount" :placeholder="accountPlaceholder" />
        </div>

        <div class="form-actions">
          <button class="btn-primary" @click="submitRequest" :disabled="submitting">
            {{ submitting ? '提交中...' : '提交申请' }}
          </button>
          <button class="btn-secondary" @click="refreshStoreStatus" :disabled="!form.storeId">
            查询当前状态
          </button>
          <button class="btn-secondary" @click="resetForm">
            重置表单
          </button>
        </div>
      </div>

      <div v-if="message" :class="['message', messageType]">
        {{ message }}
      </div>

      <div v-if="storeStatus" class="status-info">
        <h3>当前店面状态信息</h3>
        <div class="status-details">
          <p><strong>店铺名称：</strong>{{ storeStatus.storeName }}</p>
          <p><strong>当前状态：</strong>{{ storeStatus.currentStatus }}</p>
          <p><strong>租户名称：</strong>{{ storeStatus.tenantName }}</p>
          <p><strong>是否有租用记录：</strong>{{ storeStatus.hasRentRecord ? '是' : '否' }}</p>
          <p><strong>可申请变更：</strong>{{ storeStatus.canApplyStatusChange ? '是' : '否' }}</p>
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
.store-status-request {
  max-width: 800px;
  margin: 0 auto;
}

.form-section {
  background: white;
  padding: 24px;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  margin-top: 20px;
}

.form-group {
  margin-bottom: 20px;
  display: flex;
  align-items: flex-start;
}

.form-group label {
  width: 120px;
  font-weight: 600;
  color: #333;
  margin-top: 8px;
  flex-shrink: 0;
}

.form-group select,
.form-group input,
.form-group textarea {
  flex: 1;
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  transition: border-color 0.2s;
}

.form-group select:focus,
.form-group input:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #1abc9c;
  box-shadow: 0 0 0 2px rgba(26, 188, 156, 0.2);
}

.form-actions {
  margin-top: 30px;
  display: flex;
  gap: 12px;
  justify-content: flex-start;
}

.btn-primary {
  background-color: #1abc9c;
  color: white;
  border: none;
  padding: 12px 24px;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 600;
  transition: background-color 0.2s;
}

.btn-primary:hover:not(:disabled) {
  background-color: #16a085;
}

.btn-primary:disabled {
  background-color: #bdc3c7;
  cursor: not-allowed;
}

.btn-secondary {
  background-color: #95a5a6;
  color: white;
  border: none;
  padding: 12px 24px;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 600;
  transition: background-color 0.2s;
}

.btn-secondary:hover:not(:disabled) {
  background-color: #7f8c8d;
}

.btn-secondary:disabled {
  background-color: #ecf0f1;
  color: #7f8c8d;
  cursor: not-allowed;
}

.message {
  margin-top: 20px;
  padding: 16px;
  border-radius: 4px;
  font-size: 14px;
  white-space: pre-line;
}

.message.info {
  background-color: #d4edda;
  border: 1px solid #c3e6cb;
  color: #155724;
}

.message.success {
  background-color: #d4edda;
  border: 1px solid #c3e6cb;
  color: #155724;
}

.message.error {
  background-color: #f8d7da;
  border: 1px solid #f5c6cb;
  color: #721c24;
}

.status-info {
  background: white;
  padding: 24px;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  margin-top: 20px;
}

.status-info h3 {
  margin-top: 0;
  color: #333;
  border-bottom: 2px solid #1abc9c;
  padding-bottom: 8px;
}

.status-details p {
  margin: 8px 0;
  color: #555;
}

.status-details strong {
  color: #333;
}
</style>
