<template>
  <DashboardLayout>
    <div class="page-container">
      <div class="page-header">
        <h1>商户信息管理</h1>
        <p>查看并管理您的商户详细信息。</p>
      </div>

      <div class="controls-card">
        <template v-if="role === '商户' || role === '员工'">
          <div class="form-group">
            <label>选择店铺</label>
            <select v-model.number="selectedStoreId">
              <option value="">--请选择店铺--</option>
              <option v-for="s in stores" :key="s.STORE_ID" :value="s.STORE_ID">{{ s.STORE_ID }} - {{ s.STORE_NAME || s.storeName }}</option>
            </select>
          </div>
          <div class="button-group">
            <button class="btn-primary" @click="onSelectStore" :disabled="!selectedStoreId">加载店铺</button>
            <button class="btn-secondary" @click="refreshStores">刷新列表</button>
          </div>
          <p v-if="role === '商户' && stores.length === 0" class="info-text">未找到归属店铺；若您确实有店铺，请联系管理员或使用下方手动ID查询。</p>
        </template>

        <template v-else>
          <div class="form-group">
            <label>店铺ID</label>
            <input type="number" v-model.number="storeId" min="1" placeholder="输入店铺ID" />
          </div>
          <button class="btn-primary" @click="loadMerchantInfo" :disabled="!storeId">查询商户信息</button>
        </template>
      </div>

      <div v-if="loading" class="status-card">加载中...</div>
      <div v-if="error" class="status-card error">{{ error }}</div>

      <div v-if="merchant && !loading" class="details-grid">
        <form @submit.prevent="submit" class="form-card">
          <h4>编辑信息</h4>
          <div class="form-grid">
            <div class="form-group">
              <label>店铺名称</label>
              <input type="text" v-model="form.storeName" :disabled="!editable.corePermissions.storeName" />
            </div>
            <div class="form-group">
              <label>租户名称</label>
              <input type="text" v-model="merchant.tenantName" disabled />
            </div>
            <div class="form-group">
              <label>租户类型</label>
              <select v-model="form.storeType" :disabled="!editable.corePermissions.storeType">
                <option value="">-- 请选择 --</option>
                <option>餐饮</option>
                <option>零售</option>
                <option>服务</option>
                <option>企业连锁</option>
              </select>
            </div>
            <div class="form-group">
              <label>联系方式</label>
              <input type="text" v-model="form.contactInfo" :disabled="!editable.nonCorePermissions.contactInfo" />
            </div>
            <div class="form-group">
              <label>租用起始时间</label>
              <input type="date" v-model="form.rentStart" :disabled="!editable.corePermissions.rentStart" />
            </div>
            <div class="form-group">
              <label>租用结束时间</label>
              <input type="date" v-model="form.rentEnd" :disabled="!editable.corePermissions.rentEnd" />
            </div>
            <div class="form-group">
              <label>店铺状态</label>
              <select v-model="form.storeStatus" :disabled="!editable.corePermissions.storeStatus">
                <option value="">-- 请选择 --</option>
                <option>正常营业</option>
                <option>歇业中</option>
                <option>翻新中</option>
                <option>维修中</option>
                <option>暂停营业</option>
              </select>
            </div>
          </div>
          <div class="actions">
            <button type="submit" class="btn-primary" :disabled="submitting">{{ submitting ? '保存中...' : '保存修改' }}</button>
          </div>
          <div v-if="submitError" class="form-message error">{{ submitError }}</div>
          <div v-if="submitSuccess" class="form-message success">{{ submitSuccess }}</div>
        </form>

        <div class="info-card">
          <h4>只读信息</h4>
          <p><strong>店铺ID:</strong> {{ merchant.storeId }}</p>
          <p><strong>当前状态:</strong> {{ merchant.storeStatus }}</p>
          <p><strong>租期:</strong> {{ merchant.rentStart ? merchant.rentStart.split('T')[0] : '-' }} ~ {{ merchant.rentEnd ? merchant.rentEnd.split('T')[0] : '-' }}</p>
          <p><strong>权限角色:</strong> {{ editable.permissions.role }}</p>
          <p><strong>可修改核心信息:</strong> {{ editable.permissions.canModifyCore ? '是' : '否' }}</p>
        </div>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import axios from 'axios'
import { useUserStore } from '@/stores/user'
import { useRoute } from 'vue-router'
import DashboardLayout from '@/components/BoardLayout.vue'

const route = useRoute()
const userStore = useUserStore()

const storeId = ref(route.query.storeId ? Number(route.query.storeId) : null)
const selectedStoreId = ref(null)
const stores = ref([])
const merchant = ref(null)
const editable = reactive({
  nonCorePermissions: { contactInfo: false },
  corePermissions: { storeType: false, rentStart: false, rentEnd: false, storeStatus: false, storeName: false },
  permissions: { canModifyCore: false, canModifyNonCore: false, role: '未知' }
})

const loading = ref(false)
const error = ref('')
const submitting = ref(false)
const submitError = ref('')
const submitSuccess = ref('')

// form model -- initialize from merchant when loaded
const form = reactive({
  storeName: '',
  storeType: '',
  contactInfo: '',
  rentStart: '',
  rentEnd: '',
  storeStatus: ''
})

function clearMessages() {
  error.value = ''
  submitError.value = ''
  submitSuccess.value = ''
}

async function loadMerchantInfo() {
  clearMessages()
  let idToLoad = storeId.value
  // if selectedStoreId set (员工选择)， prefer that
  if (selectedStoreId.value) idToLoad = selectedStoreId.value

  if (!idToLoad) {
    error.value = '请输入有效的店铺ID或先选择店铺'
    return
  }

  loading.value = true
  try {
  const operator = userStore.userInfo?.account || userStore.token || ''

    // 获取商户信息（带权限）
  const res = await axios.get(`/api/Store/GetMerchantInfo/${idToLoad}`, { params: { operatorAccount: operator } })
    merchant.value = res.data

    // 填充表单
    form.storeName = merchant.value.storeName || ''
    form.storeType = merchant.value.storeType || ''
    form.contactInfo = merchant.value.contactInfo || ''
    form.rentStart = merchant.value.rentStart ? formatDateForInput(merchant.value.rentStart) : ''
    form.rentEnd = merchant.value.rentEnd ? formatDateForInput(merchant.value.rentEnd) : ''
    form.storeStatus = merchant.value.storeStatus || ''

    // 获取可编辑字段以决定哪些控件可用
  const ef = await axios.get(`/api/Store/GetEditableFields/${idToLoad}`, { params: { operatorAccount: operator } })
    const data = ef.data
    // map permissions
    editable.permissions.canModifyCore = data.permissions?.canModifyCore === true
    editable.permissions.canModifyNonCore = data.permissions?.canModifyNonCore === true
    editable.permissions.role = data.permissions?.role || (userStore.role || '未知')

    // Set which specific fields are editable
  editable.nonCorePermissions.contactInfo = (data.nonCoreFields || []).includes('contactInfo')

    const coreFields = data.coreFields || []
    editable.corePermissions.storeType = coreFields.includes('storeType')
    editable.corePermissions.rentStart = coreFields.includes('rentStart')
    editable.corePermissions.rentEnd = coreFields.includes('rentEnd')
    editable.corePermissions.storeStatus = coreFields.includes('storeStatus')
    editable.corePermissions.storeName = coreFields.includes('storeName')

  } catch (e) {
    error.value = e?.response?.data?.error || '查询商户信息失败'
  } finally {
    loading.value = false
  }
}

function formatDateForInput(dt) {
  // backend may return ISO string; extract date part
  try {
    return dt.split('T')[0]
  } catch {
    return ''
  }
}

function hasCoreFieldChange() {
  if (!merchant.value) return false
  // compare fields considered core
  if (editable.corePermissions.storeName && form.storeName !== (merchant.value.storeName || '')) return true
  if (editable.corePermissions.storeType && form.storeType !== (merchant.value.storeType || '')) return true
  if (editable.corePermissions.storeStatus && form.storeStatus !== (merchant.value.storeStatus || '')) return true
  if (editable.corePermissions.rentStart && form.rentStart !== (merchant.value.rentStart ? formatDateForInput(merchant.value.rentStart) : '')) return true
  if (editable.corePermissions.rentEnd && form.rentEnd !== (merchant.value.rentEnd ? formatDateForInput(merchant.value.rentEnd) : '')) return true
  return false
}

async function submit() {
  clearMessages()
  if (!merchant.value) {
    submitError.value = '请先加载商户信息'
    return
  }

  // If current user cannot modify core but attempted to change core fields, block with message per use case
  const operator = userStore.userInfo?.account || userStore.token || ''
  if (!editable.permissions.canModifyCore && hasCoreFieldChange()) {
    submitError.value = '无权限，需联系管理人员'
    return
  }

  submitting.value = true
  try {
    const dto = {
      StoreId: Number(merchant.value.storeId),
      ContactInfo: editable.nonCorePermissions.contactInfo ? form.contactInfo : undefined,
      StoreType: editable.corePermissions.storeType ? (form.storeType || undefined) : undefined,
      StoreName: editable.corePermissions.storeName ? (form.storeName || undefined) : undefined,
      StoreStatus: editable.corePermissions.storeStatus ? (form.storeStatus || undefined) : undefined,
      RentStart: editable.corePermissions.rentStart && form.rentStart ? new Date(form.rentStart) : undefined,
      RentEnd: editable.corePermissions.rentEnd && form.rentEnd ? new Date(form.rentEnd) : undefined,
      OperatorAccount: operator || ''
    }

    // Remove undefined keys intentionally - backend will accept missing fields
    Object.keys(dto).forEach(k => { if (dto[k] === undefined) delete dto[k] })

    const res = await axios.put('/api/Store/UpdateMerchantInfo', dto)
    submitSuccess.value = res.data?.message || '保存成功'
    // reload merchant info to reflect backend state and to pick up any rent recalculation
    await loadMerchantInfo()
  } catch (e) {
    submitError.value = e?.response?.data?.error || '保存失败，请检查输入或联系管理员'
  } finally {
    submitting.value = false
  }
}

// if route provided storeId, auto-load
// determine role and auto-load stores
const role = computed(() => userStore.role || '游客')

async function loadForRole() {
  clearMessages()
  const account = userStore.userInfo?.account || userStore.token || ''
  if (!account) return

  try {
    if (role.value === '商户') {
      // For merchants, get their store info from their own API endpoint
      const response = await axios.get('/api/Store/GetMyRentBills', { params: { merchantAccount: account } });
      if (response.data && Array.isArray(response.data.bills) && response.data.bills.length > 0) {
        const firstBill = response.data.bills[0];
        stores.value = [{
          STORE_ID: firstBill.StoreId,
          STORE_NAME: firstBill.StoreName
        }];
        
        // If we found the store, auto-select it and load its details
        if (stores.value.length > 0) {
          selectedStoreId.value = stores.value[0].STORE_ID;
          await loadMerchantInfo();
        }
      }
    } else if (role.value === '员工') {
      // For employees, load all stores for selection
      const response = await axios.get('/api/Store/AllStores', { params: { operatorAccount: account } });
      if (Array.isArray(response.data)) {
        stores.value = response.data;
      }
    }
  } catch (e) {
    error.value = '加载店铺列表失败，请稍后重试。';
    console.error('Failed to load stores for role:', e);
  }
}

onMounted(() => {
  loadForRole();
});

// user actions
async function refreshStores() {
  clearMessages()
  const account = userStore.userInfo?.account || userStore.token || ''
  if (!account) return
  try {
    if (role.value === '员工') {
      const r = await axios.get('/api/Store/AllStores', { params: { operatorAccount: account } })
      if (Array.isArray(r.data)) stores.value = r.data
    } else if (role.value === '商户') {
      // re-run loadForRole to refresh merchant-owned stores
      await loadForRole()
    }
  } catch (e) {
    // ignore
  }
}

async function onSelectStore() {
  if (!selectedStoreId.value) return
  // set storeId and load
  storeId.value = selectedStoreId.value
  await loadMerchantInfo()
}
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

.controls-card, .status-card, .form-card, .info-card {
  background-color: var(--card-bg);
  border-radius: var(--border-radius);
  box-shadow: var(--card-shadow);
  padding: 24px;
}

.controls-card {
  display: flex;
  align-items: flex-end;
  gap: 20px;
  flex-wrap: wrap;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.form-group label { font-weight: 500; font-size: 14px; }
.form-group input, .form-group select {
  padding: 10px 12px;
  border: 1px solid var(--input-border-color);
  border-radius: 8px;
  min-width: 220px;
}

.button-group { display: flex; gap: 12px; }
.btn-primary, .btn-secondary {
  padding: 10px 20px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
  transition: background-color 0.2s, transform 0.2s;
}
.btn-primary { background-color: #1abc9c; color: white; }
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

.status-card { text-align: center; font-size: 16px; }
.status-card.error { color: #e74c3c; background-color: #fbeae5; }

.details-grid {
  display: grid;
  grid-template-columns: 3fr 1fr;
  gap: 24px;
  align-items: start;
}

.form-card h4, .info-card h4 { font-size: 18px; margin-bottom: 16px; }
.form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
}

.actions { margin-top: 24px; }
.form-message { margin-top: 16px; padding: 10px; border-radius: 8px; }
.form-message.error { color: #e74c3c; background-color: #fbeae5; }
.form-message.success { color: #27ae60; background-color: #e8f8f5; }

.info-card p { margin: 0 0 12px 0; }
.info-card p strong { color: #333; }

@media (max-width: 992px) {
  .details-grid {
    grid-template-columns: 1fr;
  }
}
</style>
