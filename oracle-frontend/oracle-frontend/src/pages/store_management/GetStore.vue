<template>
    <div class="page-container">
      <div class="page-header">
        <h1>申请租赁店面</h1>
        <p>请选择空闲的店面并填写店铺信息以提交租赁申请，提交后系统会将店面状态更新为“已租用”，店铺状态为“正常营业”。</p>
      </div>

      <div class="form-card">
        <div v-if="loading" class="status">加载中...</div>
        <div v-if="error" class="status error">{{ error }}</div>

        <form @submit.prevent="submit">
          <div class="form-grid">
            <div class="form-group">
              <label>店铺名称</label>
              <input type="text" v-model="form.storeName" required />
            </div>

            <div class="form-group">
              <label>租户类型</label>
              <select v-model="form.storeType" required>
                <option value="">-- 请选择 --</option>
                <option>餐饮</option>
                <option>零售</option>
                <option>服务</option>
                <option>企业连锁</option>
              </select>
            </div>

            <div class="form-group">
              <label>租户名</label>
              <input type="text" v-model="form.tenantName" required />
            </div>

            <div class="form-group">
              <label>联系方式</label>
              <input type="text" v-model="form.contactInfo" required />
            </div>

            <div class="form-group">
              <label>租用店面 (区域 ID)</label>
              <select v-model.number="form.areaId" required>
                <option value="">-- 请选择空闲店面 --</option>
                <option v-for="a in areas" :key="a.areaId" :value="a.areaId">
                  {{ a.areaId }} - 面积: {{ a.areaSize }} - 基础租金: {{ a.baseRent }}
                </option>
              </select>
              <p v-if="areas.length===0" class="hint">当前没有可用空置店面。</p>
            </div>

            <div class="form-group">
              <label>租用起始时间</label>
              <input type="date" v-model="form.rentStart" required />
            </div>

            <div class="form-group">
              <label>租用结束时间</label>
              <input type="date" v-model="form.rentEnd" required />
            </div>
          </div>

          <div class="actions">
            <button class="btn-primary" type="submit" :disabled="submitting">{{ submitting ? '提交中...' : '提交申请' }}</button>
            <button type="button" class="btn-secondary" @click="fetchAreas" :disabled="loading">刷新可用店面</button>
          </div>

          <div v-if="success" class="message success">{{ success }}</div>
        </form>
      </div>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import axios from 'axios'
import { useUserStore } from '@/stores/user'
import DashboardLayout from '@/components/BoardLayout.vue'

const userStore = useUserStore()

const areas = ref([])
const loading = ref(false)
const error = ref('')
const success = ref('')
const submitting = ref(false)

const form = reactive({
  storeName: '',
  storeType: '',
  tenantName: '',
  contactInfo: '',
  areaId: null,
  rentStart: '',
  rentEnd: ''
})

async function fetchAreas() {
  loading.value = true
  error.value = ''
  try {
    const res = await axios.get('/api/Store/AvailableAreas')
    // backend 返回的是对象数组
    areas.value = Array.isArray(res.data) ? res.data : res.data || []
  } catch (e) {
    error.value = e?.response?.data?.error || '获取可用店面失败'
  } finally {
    loading.value = false
  }
}

function validate() {
  error.value = ''
  if (!form.storeName || !form.storeType || !form.tenantName || !form.contactInfo || !form.areaId || !form.rentStart || !form.rentEnd) {
    error.value = '请完整填写所有必填项'
    return false
  }
  const start = new Date(form.rentStart)
  const end = new Date(form.rentEnd)
  const today = new Date()
  today.setHours(0,0,0,0)
  if (start < today) {
    error.value = '租用起始时间不能早于今天'
    return false
  }
  if (end <= start) {
    error.value = '结束时间必须晚于起始时间'
    return false
  }
  return true
}

async function submit() {
  if (!validate()) return
  submitting.value = true
  success.value = ''
  try {
    const dto = {
      StoreName: form.storeName,
      StoreType: form.storeType,
      TenantName: form.tenantName,
      ContactInfo: form.contactInfo,
      AreaId: Number(form.areaId),
      RentStart: form.rentStart ? new Date(form.rentStart) : undefined,
      RentEnd: form.rentEnd ? new Date(form.rentEnd) : undefined,
      OperatorAccount: userStore.userInfo?.account || userStore.token || ''
    }

    console.log('Submitting DTO to CreateMerchantByExistingAccount:', dto)

    const res = await axios.post('/api/Store/CreateMerchantByExistingAccount', dto)
    success.value = res.data?.message || '提交成功'

    // 若后端返回 storeId，则带上 query 跳转到店铺详情以便立即加载
    const newStoreId = res.data?.storeId
    if (newStoreId) {
      // navigate to store detail with query param so StoreDetail.vue 可以直接加载该店铺
      window.location.href = `/store-management/store-detail?storeId=${newStoreId}`
      return
    }

    // fallback 跳转到列表页
    setTimeout(() => {
      window.location.href = '/store-management/store-detail'
    }, 1000)
  } catch (e) {
    console.error('Error response:', e.response)
    // 优先显示后端的 error 字段和 details
    if (e?.response?.data?.error) {
      error.value = e.response.data.error
    } else if (e?.response?.data?.details) {
      error.value = Array.isArray(e.response.data.details) ? e.response.data.details.join('; ') : String(e.response.data.details)
    } else {
      error.value = e?.message || '提交失败'
    }
  } finally {
    submitting.value = false
  }
}

onMounted(() => {
  fetchAreas()
})
</script>

<style scoped>
:root {
  --primary-color: #1abc9c;
  --secondary-color: #7f8c8d;
  --card-bg: #ffffff;
  --card-shadow: 0 6px 18px rgba(0,0,0,0.06);
  --border-radius: 10px;
  --input-border-color: #e6e6e6;
}
.page-container { display:flex; flex-direction:column; gap:20px; }
.form-card { background:var(--card-bg); padding:20px; border-radius:var(--border-radius); box-shadow:var(--card-shadow); }
.page-header h1 { margin:0 0 6px 0 }
.form-grid { display:grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap:16px; }
.form-group label { display:block; font-weight:600; margin-bottom:6px }
.form-group input, .form-group select { width:100%; padding:8px 10px; border:1px solid var(--input-border-color); border-radius:6px }
.actions { margin-top:16px; display:flex; gap:12px }
.btn-primary {
  background-color: var(--primary-color);
  color: white;
  padding:10px 16px;
  border-radius:8px;
  border:none;
  cursor: pointer;
  font-weight:500;
  transition: background-color 0.15s ease, transform 0.15s ease;
}
.btn-primary:hover:not(:disabled) {
  background-color: #16a085;
  transform: translateY(-2px);
}
.btn-primary:disabled {
  background-color: #a3e9a4;
  cursor: not-allowed;
}
.btn-secondary { background:#f3f3f3; padding:10px 16px; border-radius:8px; border:none }
.status { margin-bottom:12px }
.status.error { color:#c0392b }
.message.success { margin-top:12px; color:#16a085 }
.hint { margin-top:6px; color:var(--secondary-color); font-size:13px }
</style>
