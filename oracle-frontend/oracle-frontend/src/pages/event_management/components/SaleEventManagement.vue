<template>
  <div class="sale-event-management">
    <!-- 顶部操作栏 -->
    <div class="header-actions">
      <h2>促销活动管理</h2>
      <div class="action-buttons">
        <button class="btn btn-primary" @click="showCreateDialog = true">
          <span class="btn-icon">+</span>
          新建促销活动
        </button>
        <button class="btn btn-secondary" @click="refreshEvents">
          <span class="btn-icon">↻</span>
          刷新
        </button>
      </div>
    </div>

    <!-- 搜索和筛选 -->
    <div class="filter-section">
      <div class="search-box">
        <input 
          type="text" 
          v-model="searchKeyword" 
          placeholder="搜索活动名称..."
          class="search-input"
          @input="searchEvents"
        >
      </div>
    </div>

    <!-- 活动列表 -->
    <div class="events-list">
      <div v-if="loading" class="loading">
        正在加载活动数据...
      </div>
      
      <div v-else-if="filteredEvents.length === 0" class="empty-state">
        <div class="empty-icon">📋</div>
        <p>暂无促销活动数据</p>
      </div>

      <div v-else class="events-grid">
        <div 
          v-for="event in filteredEvents" 
          :key="event.EVENT_ID"
          class="event-card"
        >
          <div class="event-header">
            <h3 class="event-title">{{ event.EVENT_NAME }}</h3>
            <div class="event-actions">
              <button class="action-btn edit" @click="editEvent(event)" title="编辑">
                ✏️
              </button>
              <button class="action-btn report" @click="generateReport(event)" title="生成报告">
                📊
              </button>
              <button class="action-btn delete" @click="deleteEvent(event)" title="删除">
                🗑️
              </button>
            </div>
          </div>
          
          <div class="event-details">
            <div class="detail-row">
              <span class="label">活动时间:</span>
              <span class="value">{{ formatDate(event.EVENT_START) }} ~ {{ formatDate(event.EVENT_END) }}</span>
            </div>
            <div class="detail-row">
              <span class="label">活动成本:</span>
              <span class="value cost">¥{{ event.Cost?.toLocaleString() || '0' }}</span>
            </div>
            <div class="detail-row">
              <span class="label">活动描述:</span>
              <span class="value description">{{ event.Description || '暂无描述' }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 新建/编辑活动对话框 -->
    <div v-if="showCreateDialog || showEditDialog" class="dialog-overlay" @click="closeDialogs">
      <div class="dialog" @click.stop>
        <div class="dialog-header">
          <h3>{{ isEditing ? '编辑促销活动' : '新建促销活动' }}</h3>
          <button class="close-btn" @click="closeDialogs">×</button>
        </div>
        
        <form @submit.prevent="submitEvent" class="dialog-form">
          <div class="form-group">
            <label>活动名称 *</label>
            <input 
              type="text" 
              v-model="currentEvent.EventName" 
              required
              class="form-input"
              placeholder="请输入活动名称"
            >
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>开始时间 *</label>
              <input 
                type="datetime-local" 
                v-model="currentEvent.EventStart" 
                required
                class="form-input"
              >
            </div>
            <div class="form-group">
              <label>结束时间 *</label>
              <input 
                type="datetime-local" 
                v-model="currentEvent.EventEnd" 
                required
                class="form-input"
              >
            </div>
          </div>

          <div class="form-group">
            <label>活动成本 *</label>
            <input 
              type="number" 
              v-model.number="currentEvent.Cost" 
              required
              min="0"
              step="0.01"
              class="form-input"
              placeholder="请输入活动成本（元）"
            >
          </div>

          <div class="form-group">
            <label>活动描述</label>
            <textarea 
              v-model="currentEvent.Description" 
              class="form-textarea"
              placeholder="请输入活动描述"
              rows="4"
            ></textarea>
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

    <!-- 报告对话框 -->
    <div v-if="showReportDialog" class="dialog-overlay" @click="closeReportDialog">
      <div class="dialog dialog-large" @click.stop>
        <div class="dialog-header">
          <h3>活动报告 - {{ reportData?.EventName }}</h3>
          <button class="close-btn" @click="closeReportDialog">×</button>
        </div>
        
        <div class="dialog-content" v-if="reportData">
          <div class="report-grid">
            <div class="report-card">
              <div class="report-label">参与商铺数量</div>
              <div class="report-value">{{ reportData.ShopCount }}</div>
            </div>
            <div class="report-card">
              <div class="report-label">销售增长</div>
              <div class="report-value positive">{{ reportData.SalesIncrement?.toLocaleString() || '0' }}</div>
            </div>
            <div class="report-card">
              <div class="report-label">活动成本</div>
              <div class="report-value">¥{{ reportData.Cost?.toLocaleString() || '0' }}</div>
            </div>
            <div class="report-card">
              <div class="report-label">投资回报率</div>
              <div class="report-value" :class="{ positive: reportData.ROI > 0, negative: reportData.ROI < 0 }">
                {{ (reportData.ROI * 100).toFixed(2) }}%
              </div>
            </div>
            <div class="report-card">
              <div class="report-label">优惠券使用率</div>
              <div class="report-value">{{ (reportData.CouponRedemptionRate * 100).toFixed(2) }}%</div>
            </div>
          </div>
        </div>

        <div class="dialog-actions">
          <button class="btn btn-primary" @click="exportReport">导出报告</button>
          <button class="btn btn-secondary" @click="closeReportDialog">关闭</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'

// 响应式数据
const loading = ref(false)
const events = ref([])
const searchKeyword = ref('')
const showCreateDialog = ref(false)
const showEditDialog = ref(false)
const showReportDialog = ref(false)
const submitting = ref(false)
const reportData = ref(null)

// 当前编辑的活动
const currentEvent = reactive({
  EventName: '',
  EventStart: '',
  EventEnd: '',
  Cost: 0,
  Description: ''
})

// 计算属性
const isEditing = computed(() => !!currentEvent.EVENT_ID)
const filteredEvents = computed(() => {
  if (!searchKeyword.value) return events.value
  return events.value.filter(event => 
    event.EVENT_NAME?.toLowerCase().includes(searchKeyword.value.toLowerCase())
  )
})

// API配置
const API_BASE = 'http://localhost:8081/api'

// 方法定义
const formatDate = (dateString) => {
  if (!dateString) return ''
  const date = new Date(dateString)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// 获取所有促销活动
const fetchEvents = async () => {
  loading.value = true
  try {
    const response = await fetch(`${API_BASE}/SaleEvent`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      }
    })
    
    if (response.ok) {
      events.value = await response.json()
    } else {
      console.error('获取活动失败:', response.statusText)
      alert('获取活动数据失败，请检查网络连接')
    }
  } catch (error) {
    console.error('网络错误:', error)
    alert('网络连接错误，请稍后重试')
  } finally {
    loading.value = false
  }
}

// 刷新活动列表
const refreshEvents = () => {
  fetchEvents()
}

// 搜索活动
const searchEvents = () => {
  // 搜索逻辑由computed属性处理
}

// 重置表单
const resetForm = () => {
  Object.assign(currentEvent, {
    EventName: '',
    EventStart: '',
    EventEnd: '',
    Cost: 0,
    Description: ''
  })
  delete currentEvent.EVENT_ID
}

// 关闭对话框
const closeDialogs = () => {
  showCreateDialog.value = false
  showEditDialog.value = false
  resetForm()
}

// 编辑活动
const editEvent = (event) => {
  Object.assign(currentEvent, {
    EVENT_ID: event.EVENT_ID,
    EventName: event.EVENT_NAME,
    EventStart: event.EVENT_START ? new Date(event.EVENT_START).toISOString().slice(0, 16) : '',
    EventEnd: event.EVENT_END ? new Date(event.EVENT_END).toISOString().slice(0, 16) : '',
    Cost: event.Cost || 0,
    Description: event.Description || ''
  })
  showEditDialog.value = true
}

// 提交活动（新建或编辑）
const submitEvent = async () => {
  submitting.value = true
  
  try {
    const url = isEditing.value 
      ? `${API_BASE}/SaleEvent/${currentEvent.EVENT_ID}`
      : `${API_BASE}/SaleEvent`
    
    const method = isEditing.value ? 'PUT' : 'POST'
    
    const response = await fetch(url, {
      method,
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(currentEvent)
    })
    
    if (response.ok) {
      alert(isEditing.value ? '活动更新成功！' : '活动创建成功！')
      closeDialogs()
      await fetchEvents()
    } else {
      const errorText = await response.text()
      alert(`操作失败: ${errorText}`)
    }
  } catch (error) {
    console.error('提交失败:', error)
    alert('网络错误，请稍后重试')
  } finally {
    submitting.value = false
  }
}

// 删除活动
const deleteEvent = async (event) => {
  if (!confirm(`确定要删除活动"${event.EVENT_NAME}"吗？此操作不可恢复。`)) {
    return
  }
  
  try {
    const response = await fetch(`${API_BASE}/SaleEvent/${event.EVENT_ID}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      }
    })
    
    if (response.ok) {
      alert('活动删除成功！')
      await fetchEvents()
    } else {
      const errorText = await response.text()
      alert(`删除失败: ${errorText}`)
    }
  } catch (error) {
    console.error('删除失败:', error)
    alert('网络错误，请稍后重试')
  }
}

// 生成报告
const generateReport = async (event) => {
  try {
    const response = await fetch(`${API_BASE}/SaleEvent/${event.EVENT_ID}/report`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      }
    })
    
    if (response.ok) {
      reportData.value = await response.json()
      showReportDialog.value = true
    } else {
      const errorText = await response.text()
      alert(`生成报告失败: ${errorText}`)
    }
  } catch (error) {
    console.error('生成报告失败:', error)
    alert('网络错误，请稍后重试')
  }
}

// 关闭报告对话框
const closeReportDialog = () => {
  showReportDialog.value = false
  reportData.value = null
}

// 导出报告
const exportReport = () => {
  if (!reportData.value) return
  
  const reportContent = `
促销活动报告
============
活动名称: ${reportData.value.EventName}
参与商铺数量: ${reportData.value.ShopCount}
销售增长: ${reportData.value.SalesIncrement?.toLocaleString() || '0'}
活动成本: ¥${reportData.value.Cost?.toLocaleString() || '0'}
投资回报率: ${(reportData.value.ROI * 100).toFixed(2)}%
优惠券使用率: ${(reportData.value.CouponRedemptionRate * 100).toFixed(2)}%

报告生成时间: ${new Date().toLocaleString('zh-CN')}
  `.trim()
  
  const blob = new Blob([reportContent], { type: 'text/plain;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `促销活动报告_${reportData.value.EventName}_${new Date().toISOString().slice(0, 10)}.txt`
  a.click()
  URL.revokeObjectURL(url)
}

// 组件挂载时获取数据
onMounted(() => {
  fetchEvents()
})
</script>

<style scoped>
.sale-event-management {
  padding: 24px;
}

/* 顶部操作栏 */
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

.btn-icon {
  font-size: 16px;
}

/* 搜索筛选 */
.filter-section {
  margin-bottom: 24px;
}

.search-box {
  max-width: 300px;
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

/* 活动列表 */
.events-list {
  min-height: 400px;
}

.loading {
  text-align: center;
  padding: 60px 0;
  color: #909399;
  font-size: 16px;
}

.empty-state {
  text-align: center;
  padding: 60px 0;
  color: #909399;
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 16px;
}

.events-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
  gap: 20px;
}

.event-card {
  background: #fff;
  border: 1px solid #ebeef5;
  border-radius: 8px;
  padding: 20px;
  transition: all 0.3s ease;
}

.event-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  transform: translateY(-2px);
}

.event-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
}

.event-title {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #303133;
  flex: 1;
}

.event-actions {
  display: flex;
  gap: 8px;
  margin-left: 12px;
}

.action-btn {
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  transition: all 0.3s ease;
  background: #f4f4f5;
}

.action-btn:hover {
  transform: scale(1.1);
}

.action-btn.edit:hover {
  background: #ecf5ff;
}

.action-btn.report:hover {
  background: #f0f9ff;
}

.action-btn.delete:hover {
  background: #fef0f0;
}

.event-details {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.detail-row {
  display: flex;
  font-size: 14px;
}

.detail-row .label {
  color: #909399;
  min-width: 80px;
  margin-right: 8px;
}

.detail-row .value {
  color: #606266;
  flex: 1;
}

.detail-row .value.cost {
  color: #f56c6c;
  font-weight: 600;
}

.detail-row .value.description {
  word-break: break-word;
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

.dialog-form {
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

.form-input,
.form-textarea {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  font-size: 14px;
  transition: border-color 0.3s ease;
  box-sizing: border-box;
}

.form-input:focus,
.form-textarea:focus {
  outline: none;
  border-color: #409eff;
}

.form-textarea {
  resize: vertical;
  font-family: inherit;
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

/* 报告样式 */
.report-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 20px;
}

.report-card {
  text-align: center;
  padding: 20px;
  background: #f8f9fa;
  border-radius: 8px;
  border: 1px solid #ebeef5;
}

.report-label {
  font-size: 14px;
  color: #909399;
  margin-bottom: 8px;
}

.report-value {
  font-size: 24px;
  font-weight: 600;
  color: #303133;
}

.report-value.positive {
  color: #67c23a;
}

.report-value.negative {
  color: #f56c6c;
}
</style>
