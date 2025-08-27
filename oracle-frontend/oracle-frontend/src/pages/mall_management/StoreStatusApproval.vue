<template>
  <div class="store-status-approval">
    <h2>店面状态变更审批</h2>
    <p>物业管理员查看待审批的申请并执行通过或驳回操作。</p>

    <div class="controls box">
      <label>过滤（状态）</label>
      <select v-model="filterStatus">
        <option value="">全部</option>
        <option value="Pending">待审批</option>
        <option value="Approved">已通过</option>
        <option value="Rejected">已驳回</option>
      </select>
      <button @click="loadApplications">刷新</button>
    </div>

    <div v-if="loading" class="box">加载中...</div>
    <div v-if="error" class="error box">{{ error }}</div>

    <div v-for="app in applications" :key="app.applicationNo" class="app box">
      <p><strong>申请编号：</strong>{{ app.applicationNo }} &nbsp; <strong>店铺：</strong>{{ app.storeName }} ({{ app.storeId }})</p>
      <p><strong>类型：</strong>{{ app.changeType }} &nbsp; <strong>目标状态：</strong>{{ app.targetStatus }}</p>
      <p><strong>申请人：</strong>{{ app.applicant }} &nbsp; <strong>提交时间：</strong>{{ new Date(app.createdAt).toLocaleString() }}</p>
      <p><strong>原因：</strong> {{ app.reason }}</p>
      <p><strong>当前状态：</strong>{{ app.status }}</p>

      <div v-if="app.status === 'Pending'" class="actions">
        <label>审批意见（可选）</label>
        <input v-model="app._comment" placeholder="填写审批意见（可选）" />
        <button @click="approve(app)">通过</button>
        <button @click="reject(app)">驳回</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'
import { useUserStore } from '@/stores/user'

const filterStatus = ref('Pending')
const applications = ref([])
const loading = ref(false)
const error = ref('')

const userStore = useUserStore()

async function loadApplications() {
  loading.value = true
  error.value = ''
  try {
    const res = await axios.get('/api/Store/Applications', { params: { status: filterStatus.value } })
    applications.value = res.data.map(a => ({ ...a, _comment: '' }))
  } catch (e) {
    error.value = e?.response?.data?.error || '无法获取申请列表'
  } finally {
    loading.value = false
  }
}

async function approve(app) {
  error.value = ''
  try {
    const dto = {
      StoreId: app.storeId,
      ApprovalAction: '通过',
      ApprovalComment: app._comment || '',
      ApproverAccount: userStore.userInfo?.account || userStore.token || '',
      TargetStatus: app.targetStatus,
      ApplicationNo: app.applicationNo
    }
    const res = await axios.post('/api/Store/ApproveStatusChange', dto)
    // 直接刷新列表
    await loadApplications()
  } catch (e) {
    error.value = e?.response?.data?.error || '审批失败'
  }
}

async function reject(app) {
  error.value = ''
  try {
    const dto = {
      StoreId: app.storeId,
      ApprovalAction: '驳回',
      ApprovalComment: app._comment || '无具体理由',
      ApproverAccount: userStore.userInfo?.account || userStore.token || '',
      TargetStatus: app.targetStatus,
      ApplicationNo: app.applicationNo
    }
    const res = await axios.post('/api/Store/ApproveStatusChange', dto)
    await loadApplications()
  } catch (e) {
    error.value = e?.response?.data?.error || '操作失败'
  }
}

// 初始加载
loadApplications()
</script>

<style scoped>
.box { background:#fff; padding:12px; margin-bottom:12px; border-radius:6px }
.app { border-left: 3px solid #ddd }
label { display:block; font-weight:600; margin-bottom:6px }
input, select, textarea { width:100%; padding:8px; box-sizing:border-box }
.actions { margin-top:12px; display:flex; gap:8px }
.error { color:#c00 }
</style>
