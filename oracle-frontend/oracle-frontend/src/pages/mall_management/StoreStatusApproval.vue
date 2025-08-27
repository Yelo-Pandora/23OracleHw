<template>
  <DashboardLayout>
    <div class="store-status-approval">
      <h2>店面状态审批管理</h2>
      <p>审批商户提交的店面状态变更申请</p>

      <div class="approval-controls">
        <div class="control-group">
          <label>选择店面（可选）：</label>
          <select v-model="filters.storeId">
            <option value="">全部店面</option>
            <option v-for="store in stores" :key="store.STORE_ID" :value="store.STORE_ID">
              {{ store.STORE_ID }} - {{ store.STORE_NAME }}
            </option>
          </select>
        </div>

        <div class="control-group">
          <label>申请状态：</label>
          <select v-model="filters.status">
            <option value="Pending">待审批</option>
            <option value="Approved">已通过</option>
            <option value="Rejected">已驳回</option>
            <option value="">全部状态</option>
          </select>
        </div>

        <div class="control-actions">
          <button class="btn-primary" @click="fetchApplications" :disabled="loading">
            {{ loading ? '加载中...' : '刷新申请列表' }}
          </button>
        </div>
      </div>

      <div v-if="applications.length === 0" class="empty-state">
        <p>暂无申请记录</p>
      </div>

      <div v-else class="applications-list">
        <div v-for="app in applications" :key="app.applicationNo" class="application-item">
          <div class="application-header">
            <div class="application-meta">
              <h3>申请编号：{{ app.applicationNo }}</h3>
              <div class="meta-details">
                <span class="store-info">店铺：{{ app.storeId }} - {{ app.storeName || '未知店铺' }}</span>
                <span class="change-type">变更类型：{{ getChangeTypeLabel(app.changeType) }}</span>
                <span class="applicant">申请人：{{ app.applicant }}</span>
                <span class="created-time">申请时间：{{ formatDate(app.createdAt) }}</span>
                <span :class="['status-badge', getStatusClass(app.status)]">{{ getStatusLabel(app.status) }}</span>
              </div>
            </div>
          </div>

          <div class="application-content">
            <div class="reason-section">
              <h4>申请原因：</h4>
              <p>{{ app.reason }}</p>
            </div>

            <div v-if="app.status === 'Pending'" class="approval-section">
              <h4>审批操作：</h4>
              <div class="approval-form">
                <div class="form-row">
                  <label>审批结果：</label>
                  <select v-model="approvalData[app.applicationNo].action" @change="onApprovalActionChange(app.applicationNo)">
                    <option value="通过">通过</option>
                    <option value="驳回">驳回</option>
                  </select>
                </div>

                <div v-if="approvalData[app.applicationNo].action === '通过'" class="form-row">
                  <label>目标状态：</label>
                  <select v-model="approvalData[app.applicationNo].targetStatus">
                    <option v-for="status in availableTargetStatuses" :key="status" :value="status">
                      {{ status }}
                    </option>
                  </select>
                </div>

                <div class="form-row">
                  <label>审批意见：</label>
                  <textarea v-model="approvalData[app.applicationNo].comment"
                           :placeholder="approvalData[app.applicationNo].action === '驳回' ? '请输入驳回原因...' : '请输入审批意见...'"
                           rows="3"></textarea>
                </div>

                <div class="form-row">
                  <label>审批人账号：</label>
                  <input type="text" v-model="approvalData[app.applicationNo].approverAccount"
                        :placeholder="approverPlaceholder" />
                </div>

                <div class="form-actions">
                  <button class="btn-success" @click="submitApproval(app)" :disabled="submitting[app.applicationNo]">
                    {{ submitting[app.applicationNo] ? '提交中...' : '提交审批' }}
                  </button>
                </div>
              </div>
            </div>

            <div v-else class="approval-result">
              <h4>审批结果：</h4>
              <p><strong>审批结果：</strong>{{ app.status === 'Approved' ? '通过' : '驳回' }}</p>
              <p><strong>审批人：</strong>{{ app.approver || '系统' }}</p>
              <p><strong>审批时间：</strong>{{ app.approvalTime ? formatDate(app.approvalTime) : '未知' }}</p>
              <p v-if="app.comment"><strong>审批意见：</strong>{{ app.comment }}</p>
            </div>
          </div>
        </div>
      </div>

      <div v-if="message" :class="['message', messageType]">
        {{ message }}
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
const applications = ref([])
const loading = ref(false)
const submitting = ref({})
const message = ref('')
const messageType = ref('info')

const filters = ref({
  storeId: '',
  status: 'Pending'
})

const approvalData = ref({})

// 可用目标状态
const availableTargetStatuses = [
  '正常营业', '维修中', '暂停营业', '已退租', '装修中'
]

// 变更类型标签映射
const changeTypeLabels = {
  '退租': '退租申请',
  '维修': '维修申请',
  '暂停营业': '暂停营业申请',
  '恢复营业': '恢复营业申请'
}

// 状态标签映射
const statusLabels = {
  'Pending': '待审批',
  'Approved': '已通过',
  'Rejected': '已驳回'
}

const approverPlaceholder = computed(() => {
  return userStore.userInfo?.account || userStore.userInfo?.username || '请输入审批人账号'
})

function getChangeTypeLabel(type) {
  return changeTypeLabels[type] || type
}

function getStatusLabel(status) {
  return statusLabels[status] || status
}

function getStatusClass(status) {
  switch (status) {
    case 'Pending': return 'pending'
    case 'Approved': return 'approved'
    case 'Rejected': return 'rejected'
    default: return 'unknown'
  }
}

function formatDate(dateString) {
  if (!dateString) return '未知'
  try {
    return new Date(dateString).toLocaleString('zh-CN')
  } catch {
    return dateString
  }
}

// 加载店铺列表
async function loadStores() {
  try {
    const params = {}

    // 员工可以传递管理员账号来获取所有店铺
    if (userStore.role === '员工') {
      params.operatorAccount = userStore.userInfo?.account
    }
    // 商户不传递operatorAccount参数

    const response = await axios.get('/api/Store/AllStores', { params })

    if (response.data && Array.isArray(response.data)) {
      let filteredStores = []

      if (userStore.role === '商户') {
        // 对于商户，通过STORE_ACCOUNT表获取关联的店铺
        try {
          const storeAccountResponse = await axios.get('/api/Accounts/GetStoreAccountByAccount', {
            params: { account: userStore.userInfo?.account }
          })

          if (storeAccountResponse.data && storeAccountResponse.data.storeId) {
            // 找到商户关联的店铺ID，然后从所有店铺中过滤出对应的店铺
            const merchantStoreId = storeAccountResponse.data.storeId
            filteredStores = response.data.filter(store => store.STORE_ID === merchantStoreId)
          } else {
            filteredStores = []
          }
        } catch (accountError) {
          console.error('获取商户店铺关联失败:', accountError)
          filteredStores = []
        }
      } else {
        // 员工可以看到所有店铺
        filteredStores = response.data
      }

      stores.value = filteredStores
    }
  } catch (error) {
    console.error('加载店铺列表失败:', error)
  }
}

// 获取申请列表
async function fetchApplications() {
  loading.value = true
  message.value = '正在加载申请列表...'
  messageType.value = 'info'

  try {
    const params = {}
    if (filters.value.storeId) params.storeId = filters.value.storeId
    if (filters.value.status) params.status = filters.value.status

    const response = await axios.get('/api/Store/Applications', { params })
    const data = response.data

    if (data && Array.isArray(data)) {
      applications.value = data

      // 初始化审批数据
      data.forEach(app => {
        if (app.status === 'Pending') {
          approvalData.value[app.applicationNo] = {
            action: '通过',
            targetStatus: availableTargetStatuses[0],
            comment: '同意该申请',
            approverAccount: userStore.userInfo?.account || userStore.userInfo?.username || ''
          }
          submitting.value[app.applicationNo] = false
        }
      })

      message.value = `成功加载 ${data.length} 条申请记录`
      messageType.value = 'success'
    } else {
      applications.value = []
      message.value = '未找到申请记录'
      messageType.value = 'info'
    }
  } catch (error) {
    console.error('获取申请列表失败:', error)
    applications.value = []
    message.value = error.response?.data?.error || '获取申请列表失败'
    messageType.value = 'error'
  } finally {
    loading.value = false
  }
}

// 审批动作变化时更新默认意见
function onApprovalActionChange(applicationNo) {
  const action = approvalData.value[applicationNo].action
  if (action === '驳回') {
    approvalData.value[applicationNo].comment = '申请材料不完整，请补充后重新提交'
  } else {
    approvalData.value[applicationNo].comment = '同意该申请'
  }
}

// 提交审批
async function submitApproval(app) {
  const approval = approvalData.value[app.applicationNo]

  // 表单验证
  if (!approval.action) {
    message.value = '请选择审批结果'
    messageType.value = 'error'
    return
  }

  if (!approval.comment.trim()) {
    message.value = '请输入审批意见'
    messageType.value = 'error'
    return
  }

  if (!approval.approverAccount) {
    approval.approverAccount = userStore.userInfo?.account || userStore.userInfo?.username || ''
  }

  try {
    submitting.value[app.applicationNo] = true
    message.value = '正在提交审批...'
    messageType.value = 'info'

    const requestData = {
      storeId: app.storeId,
      approvalAction: approval.action,
      approvalComment: approval.comment.trim(),
      approverAccount: approval.approverAccount,
      applicationNo: app.applicationNo
    }

    // 只有审批通过时才包含目标状态
    if (approval.action === '通过') {
      requestData.targetStatus = approval.targetStatus
    }

    const response = await axios.post('/api/Store/ApproveStatusChange', requestData)
    const data = response.data

    message.value = `✅ 审批提交成功！\n${data.message || ''}`
    messageType.value = 'success'

    // 刷新申请列表
    await fetchApplications()
  } catch (error) {
    console.error('提交审批失败:', error)
    message.value = error.response?.data?.error || '提交审批失败，请稍后重试'
    messageType.value = 'error'
  } finally {
    submitting.value[app.applicationNo] = false
  }
}

onMounted(async () => {
  await loadStores()
  await fetchApplications()
})
</script>

<style scoped>
.store-status-approval {
  max-width: 1200px;
  margin: 0 auto;
}

.approval-controls {
  background: white;
  padding: 24px;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  margin-top: 20px;
  display: flex;
  gap: 20px;
  align-items: flex-end;
  flex-wrap: wrap;
}

.control-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.control-group label {
  font-weight: 600;
  color: #333;
}

.control-group select {
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  min-width: 200px;
}

.control-actions {
  display: flex;
  align-items: flex-end;
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

.empty-state {
  text-align: center;
  padding: 60px 20px;
  color: #666;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  margin-top: 20px;
}

.applications-list {
  margin-top: 20px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.application-item {
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  overflow: hidden;
}

.application-header {
  background: #f8f9fa;
  padding: 16px 24px;
  border-bottom: 1px solid #e9ecef;
}

.application-meta h3 {
  margin: 0 0 12px 0;
  color: #333;
  font-size: 18px;
}

.meta-details {
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
  align-items: center;
  font-size: 14px;
  color: #666;
}

.status-badge {
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.status-badge.pending {
  background-color: #fff3cd;
  color: #856404;
}

.status-badge.approved {
  background-color: #d4edda;
  color: #155724;
}

.status-badge.rejected {
  background-color: #f8d7da;
  color: #721c24;
}

.application-content {
  padding: 24px;
}

.reason-section h4,
.approval-section h4,
.approval-result h4 {
  margin: 0 0 12px 0;
  color: #333;
  font-size: 16px;
}

.reason-section p {
  margin: 0;
  color: #555;
  line-height: 1.6;
}

.approval-form {
  background: #f8f9fa;
  padding: 20px;
  border-radius: 6px;
}

.form-row {
  margin-bottom: 16px;
  display: flex;
  align-items: flex-start;
}

.form-row label {
  width: 100px;
  font-weight: 600;
  color: #333;
  margin-top: 8px;
  flex-shrink: 0;
}

.form-row select,
.form-row input,
.form-row textarea {
  flex: 1;
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  transition: border-color 0.2s;
}

.form-row select:focus,
.form-row input:focus,
.form-row textarea:focus {
  outline: none;
  border-color: #1abc9c;
  box-shadow: 0 0 0 2px rgba(26, 188, 156, 0.2);
}

.form-actions {
  margin-top: 20px;
  display: flex;
  justify-content: flex-start;
}

.btn-success {
  background-color: #28a745;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 600;
  transition: background-color 0.2s;
}

.btn-success:hover:not(:disabled) {
  background-color: #218838;
}

.btn-success:disabled {
  background-color: #6c757d;
  cursor: not-allowed;
}

.approval-result p {
  margin: 8px 0;
  color: #555;
}

.approval-result strong {
  color: #333;
}

.message {
  margin-top: 20px;
  padding: 16px;
  border-radius: 4px;
  font-size: 14px;
  white-space: pre-line;
}

.message.info {
  background-color: #d1ecf1;
  border: 1px solid #bee5eb;
  color: #0c5460;
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
</style>
