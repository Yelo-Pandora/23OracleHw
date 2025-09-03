<template>
  <div class="collaboration-management">
    <!-- 顶部操作栏 -->
    <div class="header-actions">
      <h2>合作方管理</h2>
      <div class="action-buttons">
        <button class="btn btn-primary" @click="showCreateDialog = true">
          <span class="btn-icon">+</span>
          新建合作方
        </button>
        <button class="btn btn-secondary" @click="showReportDialog = true">
          <span class="btn-icon">📊</span>
          统计报表
        </button>
        <button class="btn btn-secondary" @click="refreshCollaborations">
          <span class="btn-icon">↻</span>
          刷新
        </button>
      </div>
    </div>

    <!-- 搜索和筛选 -->
    <div class="filter-section">
      <div class="filter-group">
        <div class="search-box">
          <input 
            type="text" 
            v-model="searchForm.name" 
            placeholder="搜索合作方名称..."
            class="search-input"
            @input="handleSearch"
          >
        </div>
        
        <div class="search-box">
          <input 
            type="text" 
            v-model="searchForm.contactor" 
            placeholder="搜索联系人..."
            class="search-input"
            @input="handleSearch"
          >
        </div>

        <div class="search-box">
          <input 
            type="number" 
            v-model.number="searchForm.id" 
            placeholder="搜索ID..."
            class="search-input"
            @input="handleSearch"
          >
        </div>

        <button class="btn btn-secondary" @click="handleSearch">
          🔍 搜索
        </button>
        
        <button class="btn btn-outline" @click="clearSearch">
          清空
        </button>
      </div>
    </div>

    <!-- 合作方列表 -->
    <div class="collaborations-list">
      <div v-if="loading" class="loading">
        正在加载合作方数据...
      </div>
      
      <div v-else-if="collaborations.length === 0" class="empty-state">
        <div class="empty-icon">🤝</div>
        <p>暂无合作方数据</p>
      </div>

      <div v-else class="collaborations-grid">
        <div 
          v-for="collaboration in collaborations" 
          :key="collaboration.COLLABORATION_ID"
          class="collaboration-card"
        >
          <div class="card-header">
            <h3 class="collaboration-name">{{ collaboration.COLLABORATION_NAME }}</h3>
            <div class="collaboration-id">ID: {{ collaboration.COLLABORATION_ID }}</div>
          </div>
          
          <div class="collaboration-details">
            <div class="detail-row">
              <span class="label">联系人:</span>
              <span class="value">{{ collaboration.CONTACTOR || '未设置' }}</span>
            </div>
            <div class="detail-row">
              <span class="label">电话:</span>
              <span class="value">{{ collaboration.PHONE_NUMBER || '未设置' }}</span>
            </div>
            <div class="detail-row">
              <span class="label">邮箱:</span>
              <span class="value email">{{ collaboration.EMAIL || '未设置' }}</span>
            </div>
          </div>

          <div class="card-actions">
            <button class="action-btn edit" @click="editCollaboration(collaboration)" title="编辑">
              ✏️ 编辑
            </button>
            <button class="action-btn view" @click="viewCollaborationDetail(collaboration)" title="查看详情">
              👁️ 详情
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 新建/编辑合作方对话框 -->
    <div v-if="showCreateDialog || showEditDialog" class="dialog-overlay" @click="closeDialogs">
      <div class="dialog" @click.stop>
        <div class="dialog-header">
          <h3>{{ isEditing ? '编辑合作方' : '新建合作方' }}</h3>
          <button class="close-btn" @click="closeDialogs">×</button>
        </div>
        
        <form @submit.prevent="submitCollaboration" class="dialog-form">
          <div class="form-row" v-if="!isEditing">
            <div class="form-group">
              <label>合作方ID *</label>
              <input 
                type="number" 
                v-model.number="currentCollaboration.CollaborationId" 
                required
                min="1"
                class="form-input"
                placeholder="请输入合作方ID"
              >
            </div>
          </div>

          <div class="form-group">
            <label>合作方名称 *</label>
            <input 
              type="text" 
              v-model="currentCollaboration.CollaborationName" 
              required
              maxlength="50"
              class="form-input"
              placeholder="请输入合作方名称"
            >
          </div>

          <div class="form-group">
            <label>联系人</label>
            <input 
              type="text" 
              v-model="currentCollaboration.Contactor" 
              maxlength="50"
              class="form-input"
              placeholder="请输入联系人姓名"
            >
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>电话号码</label>
              <input 
                type="tel" 
                v-model="currentCollaboration.PhoneNumber" 
                maxlength="20"
                class="form-input"
                placeholder="请输入电话号码"
              >
            </div>
            <div class="form-group">
              <label>邮箱</label>
              <input 
                type="email" 
                v-model="currentCollaboration.Email" 
                maxlength="50"
                class="form-input"
                placeholder="请输入邮箱地址"
              >
            </div>
          </div>

          <div class="form-actions">
            <button type="button" class="btn btn-secondary" @click="closeDialogs">
              取消
            </button>
            <button type="submit" class="btn btn-primary" :disabled="submitting">
              {{ submitting ? '保存中...' : (isEditing ? '更新' : '创建') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- 合作方详情对话框 -->
    <div v-if="showDetailDialog" class="dialog-overlay" @click="closeDetailDialog">
      <div class="dialog dialog-large" @click.stop>
        <div class="dialog-header">
          <h3>合作方详情 - {{ collaborationDetail?.COLLABORATION_NAME }}</h3>
          <button class="close-btn" @click="closeDetailDialog">×</button>
        </div>
        
        <div class="dialog-content" v-if="collaborationDetail">
          <div class="detail-grid">
            <div class="detail-section">
              <h4>基本信息</h4>
              <div class="detail-item">
                <span class="detail-label">合作方ID:</span>
                <span class="detail-value">{{ collaborationDetail.COLLABORATION_ID }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">合作方名称:</span>
                <span class="detail-value">{{ collaborationDetail.COLLABORATION_NAME }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">联系人:</span>
                <span class="detail-value">{{ collaborationDetail.CONTACTOR || '未设置' }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">电话号码:</span>
                <span class="detail-value">{{ collaborationDetail.PHONE_NUMBER || '未设置' }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">邮箱:</span>
                <span class="detail-value">{{ collaborationDetail.EMAIL || '未设置' }}</span>
              </div>
            </div>
          </div>
        </div>

        <div class="dialog-actions">
          <button class="btn btn-primary" @click="editCollaboration(collaborationDetail)">编辑</button>
          <button class="btn btn-secondary" @click="closeDetailDialog">关闭</button>
        </div>
      </div>
    </div>

    <!-- 统计报表对话框 -->
    <div v-if="showReportDialog" class="dialog-overlay" @click="closeReportDialog">
      <div class="dialog dialog-large" @click.stop>
        <div class="dialog-header">
          <h3>合作方统计报表</h3>
          <button class="close-btn" @click="closeReportDialog">×</button>
        </div>
        
        <div class="dialog-content">
          <form @submit.prevent="generateReport" class="report-form">
            <div class="form-row">
              <div class="form-group">
                <label>开始日期 *</label>
                <input 
                  type="date" 
                  v-model="reportForm.startDate" 
                  required
                  class="form-input"
                >
              </div>
              <div class="form-group">
                <label>结束日期 *</label>
                <input 
                  type="date" 
                  v-model="reportForm.endDate" 
                  required
                  class="form-input"
                >
              </div>
              <div class="form-group">
                <label>行业筛选</label>
                <input 
                  type="text" 
                  v-model="reportForm.industry" 
                  class="form-input"
                  placeholder="输入行业关键词筛选"
                >
              </div>
            </div>
            
            <div class="form-actions">
              <button type="submit" class="btn btn-primary" :disabled="generating">
                {{ generating ? '生成中...' : '生成报表' }}
              </button>
            </div>
          </form>

          <div v-if="reportData && reportData.length > 0" class="report-content">
            <h4>报表结果</h4>
            <div class="report-table">
              <table class="data-table">
                <thead>
                  <tr>
                    <th>合作方ID</th>
                    <th>活动数量</th>
                    <th>总投资额</th>
                    <th>平均收益</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="item in reportData" :key="item.CollaborationId">
                    <td>{{ item.CollaborationId }}</td>
                    <td>{{ item.EventCount }}</td>
                    <td class="amount">¥{{ item.TotalInvestment?.toLocaleString() || '0' }}</td>
                    <td class="amount">¥{{ item.AvgRevenue?.toLocaleString() || '0' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div v-else-if="reportData && reportData.length === 0" class="report-empty">
            <p>该时间段内没有找到相关数据</p>
          </div>
        </div>

        <div class="dialog-actions">
          <button v-if="reportData && reportData.length > 0" class="btn btn-primary" @click="exportReport">导出报表</button>
          <button class="btn btn-secondary" @click="closeReportDialog">关闭</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useUserStore } from '@/stores/user'

// 响应式数据
const loading = ref(false)
const collaborations = ref([])
const submitting = ref(false)
const generating = ref(false)

// 对话框显示状态
const showCreateDialog = ref(false)
const showEditDialog = ref(false)
const showDetailDialog = ref(false)
const showReportDialog = ref(false)

// 当前操作的合作方
const collaborationDetail = ref(null)
const reportData = ref(null)

// 用户信息
const userStore = useUserStore()

// 获取操作员账号ID（从用户信息中获取或使用默认值）
const getOperatorAccountId = () => {
  return userStore.userInfo?.username || 'admin' // 使用用户名或默认管理员账号
}

// 表单数据
const searchForm = reactive({
  id: null,
  name: '',
  contactor: ''
})

const currentCollaboration = reactive({
  CollaborationId: null,
  CollaborationName: '',
  Contactor: '',
  PhoneNumber: '',
  Email: ''
})

const reportForm = reactive({
  startDate: '',
  endDate: '',
  industry: ''
})

// 计算属性
const isEditing = computed(() => !!currentCollaboration.COLLABORATION_ID)

// API配置
const API_BASE = 'http://localhost:8081/api'

// 方法定义
const refreshCollaborations = () => {
  fetchCollaborations()
}

// 获取所有合作方
const fetchCollaborations = async () => {
  loading.value = true
  try {
    const params = new URLSearchParams({
      operatorAccountId: getOperatorAccountId()
    })

    // 添加搜索参数
    if (searchForm.id) params.append('id', searchForm.id)
    if (searchForm.name) params.append('name', searchForm.name)
    if (searchForm.contactor) params.append('contactor', searchForm.contactor)

    const response = await fetch(`${API_BASE}/Collaboration?${params}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      }
    })
    
    if (response.ok) {
      collaborations.value = await response.json()
    } else {
      const errorText = await response.text()
      console.error('获取合作方失败:', errorText)
      // 如果是权限问题，给出友好提示
      if (errorText.includes('权限') || errorText.includes('账号')) {
        alert('权限不足或账号信息有误，请联系管理员')
      } else {
        alert('获取合作方数据失败，请检查网络连接')
      }
    }
  } catch (error) {
    console.error('网络错误:', error)
    alert('网络连接错误，请稍后重试')
  } finally {
    loading.value = false
  }
}

// 搜索处理
const handleSearch = () => {
  fetchCollaborations()
}

// 清空搜索
const clearSearch = () => {
  Object.assign(searchForm, {
    id: null,
    name: '',
    contactor: ''
  })
  fetchCollaborations()
}

// 重置表单
const resetForm = () => {
  Object.assign(currentCollaboration, {
    CollaborationId: null,
    CollaborationName: '',
    Contactor: '',
    PhoneNumber: '',
    Email: ''
  })
  delete currentCollaboration.COLLABORATION_ID
}

// 关闭对话框
const closeDialogs = () => {
  showCreateDialog.value = false
  showEditDialog.value = false
  resetForm()
}

// 编辑合作方
const editCollaboration = (collaboration) => {
  Object.assign(currentCollaboration, {
    COLLABORATION_ID: collaboration.COLLABORATION_ID,
    CollaborationName: collaboration.COLLABORATION_NAME,
    Contactor: collaboration.CONTACTOR || '',
    PhoneNumber: collaboration.PHONE_NUMBER || '',
    Email: collaboration.EMAIL || ''
  })
  showEditDialog.value = true
  showDetailDialog.value = false
}

// 提交合作方（新建或编辑）
const submitCollaboration = async () => {
  submitting.value = true
  
  try {
    const operatorAccountId = getOperatorAccountId()
    
    if (isEditing.value) {
      // 编辑模式
      const updateData = {
        CollaborationName: currentCollaboration.CollaborationName,
        Contactor: currentCollaboration.Contactor,
        PhoneNumber: currentCollaboration.PhoneNumber,
        Email: currentCollaboration.Email
      }
      
      const response = await fetch(`${API_BASE}/Collaboration/${currentCollaboration.COLLABORATION_ID}?operatorAccountId=${operatorAccountId}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(updateData)
      })
      
      if (response.ok) {
        const result = await response.json()
        alert(result.message || '合作方更新成功！')
        closeDialogs()
        await fetchCollaborations()
      } else {
        const errorText = await response.text()
        alert(`更新失败: ${errorText}`)
      }
    } else {
      // 新建模式
      const response = await fetch(`${API_BASE}/Collaboration?operatorAccountId=${operatorAccountId}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(currentCollaboration)
      })
      
      if (response.ok) {
        const result = await response.json()
        alert(result.message || '合作方创建成功！')
        closeDialogs()
        await fetchCollaborations()
      } else {
        const errorText = await response.text()
        alert(`创建失败: ${errorText}`)
      }
    }
  } catch (error) {
    console.error('提交失败:', error)
    alert('网络错误，请稍后重试')
  } finally {
    submitting.value = false
  }
}

// 查看详情
const viewCollaborationDetail = (collaboration) => {
  collaborationDetail.value = collaboration
  showDetailDialog.value = true
}

const closeDetailDialog = () => {
  showDetailDialog.value = false
  collaborationDetail.value = null
}

// 报表相关方法
const generateReport = async () => {
  generating.value = true
  
  try {
    const params = new URLSearchParams({
      operatorAccountId: getOperatorAccountId(),
      startDate: reportForm.startDate,
      endDate: reportForm.endDate
    })
    
    if (reportForm.industry) {
      params.append('industry', reportForm.industry)
    }
    
    const response = await fetch(`${API_BASE}/Collaboration/report?${params}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      }
    })
    
    if (response.ok) {
      reportData.value = await response.json()
    } else {
      const errorText = await response.text()
      alert(`生成报表失败: ${errorText}`)
    }
  } catch (error) {
    console.error('生成报表失败:', error)
    alert('网络错误，请稍后重试')
  } finally {
    generating.value = false
  }
}

const closeReportDialog = () => {
  showReportDialog.value = false
  reportData.value = null
}

const exportReport = () => {
  if (!reportData.value || reportData.value.length === 0) return
  
  const reportContent = `
合作方统计报表
==============
报表时间: ${reportForm.startDate} ~ ${reportForm.endDate}
行业筛选: ${reportForm.industry || '全部'}

详细数据:
${reportData.value.map(item => 
  `合作方ID: ${item.CollaborationId}, 活动数量: ${item.EventCount}, 总投资额: ¥${item.TotalInvestment?.toLocaleString() || '0'}, 平均收益: ¥${item.AvgRevenue?.toLocaleString() || '0'}`
).join('\n')}

报告生成时间: ${new Date().toLocaleString('zh-CN')}
  `.trim()
  
  const blob = new Blob([reportContent], { type: 'text/plain;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `合作方统计报表_${reportForm.startDate}_${reportForm.endDate}.txt`
  a.click()
  URL.revokeObjectURL(url)
}

// 组件挂载时获取数据
onMounted(() => {
  fetchCollaborations()
  // 设置默认日期范围（最近30天）
  const today = new Date()
  const thirtyDaysAgo = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000)
  reportForm.startDate = thirtyDaysAgo.toISOString().split('T')[0]
  reportForm.endDate = today.toISOString().split('T')[0]
})
</script>

<style scoped>
.collaboration-management {
  padding: 24px;
}

/* 复用通用样式 */
.header-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.header-actions h2 {
  margin: 0;
  color: #303133;
  font-size: 20px;
  font-weight: 600;
}

.action-buttons {
  display: flex;
  gap: 12px;
}

.btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s ease;
}

.btn-primary {
  background: #409eff;
  color: white;
}

.btn-primary:hover {
  background: #337ecc;
}

.btn-secondary {
  background: #f4f4f5;
  color: #606266;
  border: 1px solid #dcdfe6;
}

.btn-secondary:hover {
  background: #ecf5ff;
  color: #409eff;
  border-color: #c6e2ff;
}

.btn-outline {
  background: transparent;
  color: #606266;
  border: 1px solid #dcdfe6;
}

.btn-outline:hover {
  background: #f4f4f5;
}

.btn-icon {
  font-size: 16px;
}

/* 筛选区域 */
.filter-section {
  margin-bottom: 24px;
}

.filter-group {
  display: flex;
  gap: 16px;
  align-items: center;
  flex-wrap: wrap;
}

.search-box {
  min-width: 150px;
}

.search-input {
  width: 100%;
  padding: 10px 16px;
  border: 1px solid #dcdfe6;
  border-radius: 6px;
  font-size: 14px;
  transition: border-color 0.3s ease;
}

.search-input:focus {
  outline: none;
  border-color: #409eff;
}

/* 合作方列表样式 */
.collaborations-list {
  min-height: 400px;
}

.loading,
.empty-state {
  text-align: center;
  padding: 60px 0;
  color: #909399;
  font-size: 16px;
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 16px;
}

.collaborations-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
  gap: 20px;
}

.collaboration-card {
  background: #fff;
  border: 1px solid #ebeef5;
  border-radius: 8px;
  padding: 20px;
  transition: all 0.3s ease;
}

.collaboration-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  transform: translateY(-2px);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
}

.collaboration-name {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #303133;
  flex: 1;
}

.collaboration-id {
  background: #f0f9ff;
  color: #409eff;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 500;
  white-space: nowrap;
  margin-left: 12px;
}

.collaboration-details {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 16px;
}

.detail-row {
  display: flex;
  font-size: 14px;
}

.detail-row .label {
  color: #909399;
  min-width: 60px;
  margin-right: 8px;
}

.detail-row .value {
  color: #606266;
  flex: 1;
}

.detail-row .value.email {
  word-break: break-all;
}

.card-actions {
  display: flex;
  gap: 8px;
}

.action-btn {
  padding: 6px 12px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 12px;
  font-weight: 500;
  transition: all 0.3s ease;
  white-space: nowrap;
}

.action-btn.edit {
  background: #f0f9ff;
  color: #409eff;
}

.action-btn.edit:hover {
  background: #409eff;
  color: white;
}

.action-btn.view {
  background: #f4f4f5;
  color: #606266;
}

.action-btn.view:hover {
  background: #909399;
  color: white;
}

/* 对话框样式 */
.dialog-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.dialog {
  background: white;
  border-radius: 8px;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
  max-width: 600px;
  width: 90%;
  max-height: 90vh;
  overflow: auto;
}

.dialog-large {
  max-width: 800px;
}

.dialog-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 24px;
  border-bottom: 1px solid #ebeef5;
}

.dialog-header h3 {
  margin: 0;
  font-size: 18px;
  color: #303133;
}

.close-btn {
  width: 30px;
  height: 30px;
  border: none;
  background: none;
  font-size: 20px;
  cursor: pointer;
  color: #909399;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
}

.close-btn:hover {
  background: #f4f4f5;
  color: #606266;
}

.dialog-content {
  padding: 24px;
}

.dialog-form,
.report-form {
  padding: 24px;
}

.form-group {
  margin-bottom: 20px;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.form-group label {
  display: block;
  margin-bottom: 6px;
  font-size: 14px;
  font-weight: 500;
  color: #606266;
}

.form-input {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  font-size: 14px;
  transition: border-color 0.3s ease;
  box-sizing: border-box;
}

.form-input:focus {
  outline: none;
  border-color: #409eff;
}

.form-actions,
.dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 24px;
  border-top: 1px solid #ebeef5;
  margin: 0;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* 详情页面 */
.detail-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 24px;
}

.detail-section {
  background: #f8f9fa;
  border-radius: 6px;
  padding: 16px;
}

.detail-section h4 {
  margin: 0 0 12px 0;
  color: #303133;
  font-size: 16px;
  font-weight: 600;
  border-bottom: 1px solid #ebeef5;
  padding-bottom: 8px;
}

.detail-item {
  display: flex;
  margin-bottom: 8px;
}

.detail-label {
  color: #909399;
  min-width: 100px;
  margin-right: 12px;
}

.detail-value {
  color: #606266;
  flex: 1;
  font-weight: 500;
}

/* 报表样式 */
.report-content {
  margin-top: 24px;
}

.report-content h4 {
  margin: 0 0 16px 0;
  color: #303133;
  font-size: 16px;
  font-weight: 600;
}

.report-table {
  overflow-x: auto;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  border: 1px solid #ebeef5;
  border-radius: 6px;
  overflow: hidden;
}

.data-table th,
.data-table td {
  padding: 12px 16px;
  text-align: left;
  border-bottom: 1px solid #ebeef5;
}

.data-table th {
  background: #f8f9fa;
  color: #303133;
  font-weight: 600;
}

.data-table td {
  color: #606266;
}

.data-table td.amount {
  color: #67c23a;
  font-weight: 600;
}

.data-table tr:last-child td {
  border-bottom: none;
}

.report-empty {
  text-align: center;
  padding: 40px 0;
  color: #909399;
}
</style>
