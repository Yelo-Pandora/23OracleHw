<template>
  <div class="venue-event-management">
    <!-- 顶部操作栏 -->
    <div class="header-actions">
      <h2>场地活动管理</h2>
      <div class="action-buttons">
        <button class="btn btn-primary" @click="showReservationDialog = true">
          <span class="btn-icon">📅</span>
          新建场地预约
        </button>
        <button class="btn btn-secondary" @click="showReportDialog = true">
          <span class="btn-icon">📊</span>
          统计报表
        </button>
        <button class="btn btn-secondary" @click="refreshEvents">
          <span class="btn-icon">↻</span>
          刷新
        </button>
      </div>
    </div>

    <!-- 筛选和搜索 -->
    <div class="filter-section">
      <div class="filter-group">
        <div class="search-box">
          <input 
            type="text" 
            v-model="searchKeyword" 
            placeholder="搜索活动名称..."
            class="search-input"
            @input="searchEvents"
          >
        </div>
        
        <div class="filter-dropdown">
          <select v-model="statusFilter" @change="filterEvents" class="filter-select">
            <option value="">全部状态</option>
            <option value="待审批">待审批</option>
            <option value="已通过">已通过</option>
            <option value="已驳回">已驳回</option>
            <option value="筹备中">筹备中</option>
            <option value="进行中">进行中</option>
            <option value="已结束">已结束</option>
            <option value="已取消">已取消</option>
            <option value="已结算">已结算</option>
          </select>
        </div>

        <div class="filter-dropdown">
          <select v-model="areaFilter" @change="filterEvents" class="filter-select">
            <option value="">全部区域</option>
            <option v-for="area in areas" :key="area.id" :value="area.id">
              区域 {{ area.id }}
            </option>
          </select>
        </div>
      </div>
    </div>

    <!-- 活动列表 -->
    <div class="events-list">
      <div v-if="loading" class="loading">
        正在加载活动数据...
      </div>
      
      <div v-else-if="filteredEvents.length === 0" class="empty-state">
        <div class="empty-icon">🏢</div>
        <p>暂无场地活动数据</p>
      </div>

      <div v-else class="events-grid">
        <div 
          v-for="event in filteredEvents" 
          :key="event.EVENT_ID"
          class="event-card"
          :class="{ 'pending-approval': event.STATUS === '待审批' }"
        >
          <div class="event-header">
            <h3 class="event-title">{{ event.EventName }}</h3>
            <div class="event-status" :class="getStatusClass(event.STATUS)">
              {{ event.STATUS }}
            </div>
          </div>
          
          <div class="event-details">
            <div class="detail-row">
              <span class="label">活动时间:</span>
              <span class="value">{{ formatDate(event.RENT_START) }} ~ {{ formatDate(event.RENT_END) }}</span>
            </div>
            <div class="detail-row">
              <span class="label">场地区域:</span>
              <span class="value">区域 {{ event.AREA_ID }}</span>
            </div>
            <div class="detail-row">
              <span class="label">合作方:</span>
              <span class="value">{{ event.CollaborationName }}</span>
            </div>
            <div class="detail-row">
              <span class="label">预计人数:</span>
              <span class="value">{{ event.Headcount || '未设定' }}</span>
            </div>
            <div class="detail-row">
              <span class="label">预计费用:</span>
              <span class="value cost">¥{{ event.Fee?.toLocaleString() || '0' }}</span>
            </div>
          </div>

          <div class="event-actions">
            <!-- 审批操作 - 仅待审批状态显示 -->
            <template v-if="event.STATUS === '待审批'">
              <button class="action-btn approve" @click="approveEvent(event)" title="审批通过">
                ✅ 通过
              </button>
              <button class="action-btn reject" @click="rejectEvent(event)" title="审批驳回">
                ❌ 驳回
              </button>
            </template>

            <!-- 管理操作 - 已通过状态显示 -->
            <template v-else-if="['已通过', '筹备中', '进行中'].includes(event.STATUS)">
              <button class="action-btn edit" @click="editEvent(event)" title="编辑活动">
                ✏️ 编辑
              </button>
              <button class="action-btn cancel" @click="cancelEvent(event)" title="取消活动">
                🚫 取消
              </button>
            </template>

            <!-- 结算操作 - 已结束状态显示 -->
            <template v-if="event.STATUS === '已结束'">
              <button class="action-btn settle" @click="settleEvent(event)" title="活动结算">
                💰 结算
              </button>
            </template>

            <!-- 通用操作 -->
            <button class="action-btn view" @click="viewEventDetail(event)" title="查看详情">
              👁️ 详情
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 场地预约对话框 -->
    <div v-if="showReservationDialog" class="dialog-overlay" @click="closeReservationDialog">
      <div class="dialog" @click.stop>
        <div class="dialog-header">
          <h3>新建场地预约</h3>
          <button class="close-btn" @click="closeReservationDialog">×</button>
        </div>
        
        <form @submit.prevent="submitReservation" class="dialog-form">
          <div class="form-row">
            <div class="form-group">
              <label>活动名称 *</label>
              <input 
                type="text" 
                v-model="reservationForm.EventName" 
                required
                class="form-input"
                placeholder="请输入活动名称"
              >
            </div>
            <div class="form-group">
              <label>合作方ID *</label>
              <input 
                type="number" 
                v-model.number="reservationForm.CollaborationId" 
                required
                min="1"
                class="form-input"
                placeholder="请输入合作方ID"
              >
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>场地区域ID *</label>
              <input 
                type="number" 
                v-model.number="reservationForm.AreaId" 
                required
                min="1"
                class="form-input"
                placeholder="请输入区域ID"
              >
            </div>
            <div class="form-group">
              <label>合作方名称 *</label>
              <input 
                type="text" 
                v-model="reservationForm.CollaborationName" 
                required
                class="form-input"
                placeholder="请输入合作方名称"
              >
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>开始时间 *</label>
              <input 
                type="datetime-local" 
                v-model="reservationForm.RentStartTime" 
                required
                class="form-input"
              >
            </div>
            <div class="form-group">
              <label>结束时间 *</label>
              <input 
                type="datetime-local" 
                v-model="reservationForm.RentEndTime" 
                required
                class="form-input"
              >
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>员工职位 *</label>
              <input 
                type="text" 
                v-model="reservationForm.StaffPosition" 
                required
                readonly
                class="form-input readonly-input"
                :placeholder="getCurrentUserRole()"
              >
              <small class="form-hint">自动填入当前登录用户身份</small>
            </div>
            <div class="form-group">
              <label>预计人数</label>
              <input 
                type="number" 
                v-model.number="reservationForm.ExpectedHeadcount" 
                min="1"
                class="form-input"
                placeholder="预计参与人数"
              >
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>预计费用</label>
              <input 
                type="number" 
                v-model.number="reservationForm.ExpectedFee" 
                min="0"
                step="0.01"
                class="form-input"
                placeholder="预计费用（元）"
              >
            </div>
            <div class="form-group">
              <label>场地容量</label>
              <input 
                type="number" 
                v-model.number="reservationForm.Capacity" 
                min="1"
                class="form-input"
                placeholder="场地容量"
              >
            </div>
          </div>

          <div class="form-group">
            <label>租用目的</label>
            <textarea 
              v-model="reservationForm.RentPurpose" 
              class="form-textarea"
              placeholder="请简要描述租用目的"
              rows="3"
            ></textarea>
          </div>

          <div class="form-actions">
            <button type="button" class="btn btn-secondary" @click="closeReservationDialog">
              取消
            </button>
            <button type="submit" class="btn btn-primary" :disabled="submitting">
              {{ submitting ? '提交中...' : '提交预约' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- 活动编辑对话框 -->
    <div v-if="showEditDialog" class="dialog-overlay" @click="closeEditDialog">
      <div class="dialog" @click.stop>
        <div class="dialog-header">
          <h3>编辑活动信息</h3>
          <button class="close-btn" @click="closeEditDialog">×</button>
        </div>
        
        <form @submit.prevent="submitEdit" class="dialog-form">
          <div class="form-group">
            <label>活动名称</label>
            <input 
              type="text" 
              v-model="editForm.EventName" 
              class="form-input"
              placeholder="活动名称"
            >
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>实际人数</label>
              <input 
                type="number" 
                v-model.number="editForm.Headcount" 
                min="1"
                class="form-input"
                placeholder="实际参与人数"
              >
            </div>
            <div class="form-group">
              <label>活动状态</label>
              <select v-model="editForm.Status" class="form-input">
                <option value="筹备中">筹备中</option>
                <option value="进行中">进行中</option>
                <option value="已结束">已结束</option>
              </select>
            </div>
          </div>

          <div class="form-group">
            <label>活动描述</label>
            <textarea 
              v-model="editForm.Description" 
              class="form-textarea"
              placeholder="活动描述"
              rows="4"
            ></textarea>
          </div>

          <div class="form-group">
            <label>参与人员账号 (每行一个)</label>
            <textarea 
              v-model="participantsText" 
              class="form-textarea"
              placeholder="请输入参与人员账号，每行一个账号"
              rows="4"
            ></textarea>
          </div>

          <div class="form-actions">
            <button type="button" class="btn btn-secondary" @click="closeEditDialog">
              取消
            </button>
            <button type="submit" class="btn btn-primary" :disabled="submitting">
              {{ submitting ? '更新中...' : '更新活动' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- 活动结算对话框 -->
    <div v-if="showSettlementDialog" class="dialog-overlay" @click="closeSettlementDialog">
      <div class="dialog" @click.stop>
        <div class="dialog-header">
          <h3>活动结算 - {{ currentEvent?.EventName }}</h3>
          <button class="close-btn" @click="closeSettlementDialog">×</button>
        </div>
        
        <form @submit.prevent="submitSettlement" class="dialog-form">
          <div class="form-row">
            <div class="form-group">
              <label>场地费用 *</label>
              <input 
                type="number" 
                v-model.number="settlementForm.VenueFee" 
                required
                min="0"
                step="0.01"
                class="form-input"
                placeholder="场地使用费用（元）"
              >
            </div>
            <div class="form-group">
              <label>额外服务费用</label>
              <input 
                type="number" 
                v-model.number="settlementForm.AdditionalServiceFee" 
                min="0"
                step="0.01"
                class="form-input"
                placeholder="额外服务费用（元）"
              >
            </div>
          </div>

          <div class="form-group">
            <label>支付方式 *</label>
            <select v-model="settlementForm.PaymentMethod" required class="form-input">
              <option value="">请选择支付方式</option>
              <option value="现金">现金</option>
              <option value="银行转账">银行转账</option>
              <option value="支付宝">支付宝</option>
              <option value="微信支付">微信支付</option>
              <option value="信用卡">信用卡</option>
            </select>
          </div>

          <div class="form-group">
            <label>发票信息</label>
            <textarea 
              v-model="settlementForm.InvoiceInfo" 
              class="form-textarea"
              placeholder="发票抬头、税号等信息"
              rows="3"
            ></textarea>
          </div>

          <div class="settlement-summary">
            <div class="summary-item">
              <span>场地费用:</span>
              <span>¥{{ (settlementForm.VenueFee || 0).toLocaleString() }}</span>
            </div>
            <div class="summary-item">
              <span>服务费用:</span>
              <span>¥{{ (settlementForm.AdditionalServiceFee || 0).toLocaleString() }}</span>
            </div>
            <div class="summary-item total">
              <span>总计费用:</span>
              <span>¥{{ ((settlementForm.VenueFee || 0) + (settlementForm.AdditionalServiceFee || 0)).toLocaleString() }}</span>
            </div>
          </div>

          <div class="form-actions">
            <button type="button" class="btn btn-secondary" @click="closeSettlementDialog">
              取消
            </button>
            <button type="submit" class="btn btn-primary" :disabled="submitting">
              {{ submitting ? '结算中...' : '确认结算' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- 活动详情对话框 -->
    <div v-if="showDetailDialog" class="dialog-overlay" @click="closeDetailDialog">
      <div class="dialog dialog-large" @click.stop>
        <div class="dialog-header">
          <h3>活动详情 - {{ eventDetail?.EventName }}</h3>
          <button class="close-btn" @click="closeDetailDialog">×</button>
        </div>
        
        <div class="dialog-content" v-if="eventDetail">
          <div class="detail-grid">
            <div class="detail-section">
              <h4>基本信息</h4>
              <div class="detail-item">
                <span class="detail-label">活动名称:</span>
                <span class="detail-value">{{ eventDetail.EventName }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">活动状态:</span>
                <span class="detail-value" :class="getStatusClass(eventDetail.STATUS)">{{ eventDetail.STATUS }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">场地区域:</span>
                <span class="detail-value">区域 {{ eventDetail.AREA_ID }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">合作方:</span>
                <span class="detail-value">{{ eventDetail.CollaborationName }}</span>
              </div>
            </div>

            <div class="detail-section">
              <h4>时间信息</h4>
              <div class="detail-item">
                <span class="detail-label">开始时间:</span>
                <span class="detail-value">{{ formatDate(eventDetail.RENT_START) }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">结束时间:</span>
                <span class="detail-value">{{ formatDate(eventDetail.RENT_END) }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">持续时长:</span>
                <span class="detail-value">{{ calculateDuration(eventDetail.RENT_START, eventDetail.RENT_END) }}</span>
              </div>
            </div>

            <div class="detail-section">
              <h4>人员和费用</h4>
              <div class="detail-item">
                <span class="detail-label">预计人数:</span>
                <span class="detail-value">{{ eventDetail.Headcount || '未设定' }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">场地容量:</span>
                <span class="detail-value">{{ eventDetail.Capacity || '未设定' }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">预计费用:</span>
                <span class="detail-value">¥{{ eventDetail.Fee?.toLocaleString() || '0' }}</span>
              </div>
              <div class="detail-item">
                <span class="detail-label">区域费率:</span>
                <span class="detail-value">¥{{ eventDetail.AreaFee?.toLocaleString() || '0' }}/小时</span>
              </div>
            </div>

            <div class="detail-section" v-if="eventDetail.Participants && eventDetail.Participants.length > 0">
              <h4>参与人员</h4>
              <div class="participants-list">
                <span v-for="participant in eventDetail.Participants" :key="participant" class="participant-tag">
                  {{ participant }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <div class="dialog-actions">
          <button class="btn btn-secondary" @click="closeDetailDialog">关闭</button>
        </div>
      </div>
    </div>

    <!-- 统计报表对话框 -->
    <div v-if="showReportDialog" class="dialog-overlay" @click="closeReportDialog">
      <div class="dialog dialog-large" @click.stop>
        <div class="dialog-header">
          <h3>场地活动统计报表</h3>
          <button class="close-btn" @click="closeReportDialog">×</button>
        </div>
        
        <div class="dialog-content">
          <form @submit.prevent="generateReport" class="report-form">
            <div class="form-row">
              <div class="form-group">
                <label>开始日期 *</label>
                <input 
                  type="date" 
                  v-model="reportForm.StartDate" 
                  required
                  class="form-input"
                >
              </div>
              <div class="form-group">
                <label>结束日期 *</label>
                <input 
                  type="date" 
                  v-model="reportForm.EndDate" 
                  required
                  class="form-input"
                >
              </div>
              <div class="form-group">
                <label>报表类型</label>
                <select v-model="reportForm.ReportType" class="form-input">
                  <option value="daily">日报</option>
                  <option value="weekly">周报</option>
                  <option value="monthly">月报</option>
                </select>
              </div>
            </div>
            
            <div class="form-actions">
              <button type="submit" class="btn btn-primary" :disabled="generating">
                {{ generating ? '生成中...' : '生成报表' }}
              </button>
            </div>
          </form>

          <div v-if="reportData" class="report-content">
            <div class="report-summary">
              <div class="summary-card">
                <div class="summary-label">总活动数</div>
                <div class="summary-value">{{ reportData.TotalEvents }}</div>
              </div>
              <div class="summary-card">
                <div class="summary-label">总租用时长</div>
                <div class="summary-value">{{ reportData.TotalRentHours }}小时</div>
              </div>
              <div class="summary-card">
                <div class="summary-label">总收入</div>
                <div class="summary-value">¥{{ reportData.TotalRevenue?.toLocaleString() }}</div>
              </div>
              <div class="summary-card">
                <div class="summary-label">平均入座率</div>
                <div class="summary-value">{{ reportData.AverageOccupancy }}%</div>
              </div>
            </div>

            <div v-if="reportData.PopularVenues && reportData.PopularVenues.length > 0" class="popular-venues">
              <h4>热门场地排行</h4>
              <div class="venues-list">
                <div v-for="venue in reportData.PopularVenues" :key="venue.AreaId" class="venue-item">
                  <span class="venue-name">区域 {{ venue.AreaId }}</span>
                  <span class="venue-count">{{ venue.EventCount }} 次活动</span>
                  <span class="venue-revenue">¥{{ venue.TotalRevenue?.toLocaleString() }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="dialog-actions">
          <button v-if="reportData" class="btn btn-primary" @click="exportReport">导出报表</button>
          <button class="btn btn-secondary" @click="closeReportDialog">关闭</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useUserStore } from '@/stores/user'

// 用户store
const userStore = useUserStore()

// 响应式数据
const loading = ref(false)
const events = ref([])
const areas = ref([])
const searchKeyword = ref('')
const statusFilter = ref('')
const areaFilter = ref('')
const submitting = ref(false)
const generating = ref(false)

// 对话框显示状态
const showReservationDialog = ref(false)
const showEditDialog = ref(false)
const showSettlementDialog = ref(false)
const showDetailDialog = ref(false)
const showReportDialog = ref(false)

// 当前操作的活动
const currentEvent = ref(null)
const eventDetail = ref(null)
const reportData = ref(null)

// 表单数据
const reservationForm = reactive({
  EventName: '',
  CollaborationId: null,
  AreaId: null,
  RentStartTime: '',
  RentEndTime: '',
  RentPurpose: '',
  CollaborationName: '',
  StaffPosition: '',
  ExpectedHeadcount: null,
  ExpectedFee: null,
  Capacity: null
})

const editForm = reactive({
  EventName: '',
  Headcount: null,
  Description: '',
  Status: ''
})

const settlementForm = reactive({
  VenueFee: null,
  AdditionalServiceFee: null,
  PaymentMethod: '',
  InvoiceInfo: ''
})

const reportForm = reactive({
  StartDate: '',
  EndDate: '',
  ReportType: 'monthly'
})

const participantsText = ref('')

// 计算属性
const filteredEvents = computed(() => {
  let filtered = events.value

  if (searchKeyword.value) {
    filtered = filtered.filter(event => 
      event.EventName?.toLowerCase().includes(searchKeyword.value.toLowerCase())
    )
  }

  if (statusFilter.value) {
    filtered = filtered.filter(event => event.STATUS === statusFilter.value)
  }

  if (areaFilter.value) {
    filtered = filtered.filter(event => event.AREA_ID === parseInt(areaFilter.value))
  }

  return filtered
})

// API配置
const API_BASE = 'http://localhost:8081/api'

// 工具方法
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

// 获取当前用户身份
const getCurrentUserRole = () => {
  return userStore.role || '员工'
}

const calculateDuration = (start, end) => {
  if (!start || !end) return ''
  const startDate = new Date(start)
  const endDate = new Date(end)
  const hours = Math.round((endDate - startDate) / (1000 * 60 * 60))
  return `${hours} 小时`
}

const getStatusClass = (status) => {
  const statusMap = {
    '待审批': 'status-pending',
    '已通过': 'status-approved',
    '已驳回': 'status-rejected',
    '筹备中': 'status-preparing',
    '进行中': 'status-ongoing',
    '已结束': 'status-finished',
    '已取消': 'status-cancelled',
    '已结算': 'status-settled'
  }
  return statusMap[status] || 'status-default'
}

// API方法
const fetchEvents = async () => {
  loading.value = true
  try {
    const response = await fetch(`${API_BASE}/VenueEvent/events`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      }
    })
    
    if (response.ok) {
      events.value = await response.json()
      // 提取区域信息
      const uniqueAreas = [...new Set(events.value.map(e => e.AREA_ID))]
      areas.value = uniqueAreas.map(id => ({ id }))
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

const refreshEvents = () => {
  fetchEvents()
}

const searchEvents = () => {
  // 搜索逻辑由computed属性处理
}

const filterEvents = () => {
  // 筛选逻辑由computed属性处理
}

// 预约相关方法
const resetReservationForm = () => {
  Object.assign(reservationForm, {
    EventName: '',
    CollaborationId: null,
    AreaId: null,
    RentStartTime: '',
    RentEndTime: '',
    RentPurpose: '',
    CollaborationName: '',
    StaffPosition: getCurrentUserRole(), // 自动填入当前用户身份
    ExpectedHeadcount: null,
    ExpectedFee: null,
    Capacity: null
  })
}

const closeReservationDialog = () => {
  showReservationDialog.value = false
  resetReservationForm()
}

const submitReservation = async () => {
  submitting.value = true
  
  try {
    const response = await fetch(`${API_BASE}/VenueEvent/reservations`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(reservationForm)
    })
    
    if (response.ok) {
      const result = await response.json()
      alert(result.message || '预约提交成功！')
      closeReservationDialog()
      await fetchEvents()
    } else {
      const errorText = await response.text()
      alert(`预约失败: ${errorText}`)
    }
  } catch (error) {
    console.error('提交失败:', error)
    alert('网络错误，请稍后重试')
  } finally {
    submitting.value = false
  }
}

// 审批相关方法
const approveEvent = async (event) => {
  if (!confirm(`确定要审批通过活动"${event.EventName}"吗？`)) {
    return
  }
  
  try {
    const response = await fetch(`${API_BASE}/VenueEvent/reservations/${event.EVENT_ID}/approve`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify('审批通过')
    })
    
    if (response.ok) {
      const result = await response.json()
      alert(result.message || '审批成功！')
      await fetchEvents()
    } else {
      const errorText = await response.text()
      alert(`审批失败: ${errorText}`)
    }
  } catch (error) {
    console.error('审批失败:', error)
    alert('网络错误，请稍后重试')
  }
}

const rejectEvent = async (event) => {
  const reason = prompt(`请输入驳回理由：`)
  if (!reason) return
  
  try {
    const response = await fetch(`${API_BASE}/VenueEvent/reservations/${event.EVENT_ID}/reject`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(reason)
    })
    
    if (response.ok) {
      const result = await response.json()
      alert(result.message || '已驳回申请！')
      await fetchEvents()
    } else {
      const errorText = await response.text()
      alert(`驳回失败: ${errorText}`)
    }
  } catch (error) {
    console.error('驳回失败:', error)
    alert('网络错误，请稍后重试')
  }
}

// 编辑相关方法
const editEvent = (event) => {
  currentEvent.value = event
  Object.assign(editForm, {
    EventName: event.EventName || '',
    Headcount: event.Headcount,
    Description: '',
    Status: event.STATUS || '筹备中'
  })
  participantsText.value = ''
  showEditDialog.value = true
}

const closeEditDialog = () => {
  showEditDialog.value = false
  currentEvent.value = null
  participantsText.value = ''
}

const submitEdit = async () => {
  submitting.value = true
  
  try {
    const updateData = { ...editForm }
    if (participantsText.value.trim()) {
      updateData.ParticipantAccounts = participantsText.value.split('\n').filter(line => line.trim())
    }
    
    const response = await fetch(`${API_BASE}/VenueEvent/events/${currentEvent.value.EVENT_ID}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(updateData)
    })
    
    if (response.ok) {
      const result = await response.json()
      alert(result.message || '活动更新成功！')
      closeEditDialog()
      await fetchEvents()
    } else {
      const errorText = await response.text()
      alert(`更新失败: ${errorText}`)
    }
  } catch (error) {
    console.error('更新失败:', error)
    alert('网络错误，请稍后重试')
  } finally {
    submitting.value = false
  }
}

// 取消活动
const cancelEvent = async (event) => {
  if (!confirm(`确定要取消活动"${event.EventName}"吗？此操作不可恢复。`)) {
    return
  }
  
  try {
    const response = await fetch(`${API_BASE}/VenueEvent/events/${event.EVENT_ID}/cancel`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      }
    })
    
    if (response.ok) {
      const result = await response.json()
      alert(result.message || '活动已取消！')
      await fetchEvents()
    } else {
      const errorText = await response.text()
      alert(`取消失败: ${errorText}`)
    }
  } catch (error) {
    console.error('取消失败:', error)
    alert('网络错误，请稍后重试')
  }
}

// 结算相关方法
const settleEvent = (event) => {
  currentEvent.value = event
  Object.assign(settlementForm, {
    VenueFee: 0,
    AdditionalServiceFee: 0,
    PaymentMethod: '',
    InvoiceInfo: ''
  })
  showSettlementDialog.value = true
}

const closeSettlementDialog = () => {
  showSettlementDialog.value = false
  currentEvent.value = null
}

const submitSettlement = async () => {
  submitting.value = true
  
  try {
    const response = await fetch(`${API_BASE}/VenueEvent/events/${currentEvent.value.EVENT_ID}/settlement`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(settlementForm)
    })
    
    if (response.ok) {
      const result = await response.json()
      alert(result.message || '结算成功！')
      closeSettlementDialog()
      await fetchEvents()
    } else {
      const errorText = await response.text()
      alert(`结算失败: ${errorText}`)
    }
  } catch (error) {
    console.error('结算失败:', error)
    alert('网络错误，请稍后重试')
  } finally {
    submitting.value = false
  }
}

// 查看详情
const viewEventDetail = async (event) => {
  try {
    const response = await fetch(`${API_BASE}/VenueEvent/events/${event.EVENT_ID}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      }
    })
    
    if (response.ok) {
      eventDetail.value = await response.json()
      showDetailDialog.value = true
    } else {
      const errorText = await response.text()
      alert(`获取详情失败: ${errorText}`)
    }
  } catch (error) {
    console.error('获取详情失败:', error)
    alert('网络错误，请稍后重试')
  }
}

const closeDetailDialog = () => {
  showDetailDialog.value = false
  eventDetail.value = null
}

// 报表相关方法
const generateReport = async () => {
  generating.value = true
  
  try {
    const params = new URLSearchParams({
      StartDate: reportForm.StartDate,
      EndDate: reportForm.EndDate,
      ReportType: reportForm.ReportType
    })
    
    const response = await fetch(`${API_BASE}/VenueEvent/reports?${params}`, {
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
  if (!reportData.value) return
  
  const reportContent = `
场地活动统计报表
==============
报表时间: ${reportForm.StartDate} ~ ${reportForm.EndDate}
报表类型: ${reportForm.ReportType}

总体统计:
- 总活动数: ${reportData.value.TotalEvents}
- 总租用时长: ${reportData.value.TotalRentHours} 小时
- 总收入: ¥${reportData.value.TotalRevenue?.toLocaleString()}
- 平均入座率: ${reportData.value.AverageOccupancy}%

${reportData.value.PopularVenues && reportData.value.PopularVenues.length > 0 ? `
热门场地排行:
${reportData.value.PopularVenues.map((venue, index) => 
  `${index + 1}. 区域 ${venue.AreaId} - ${venue.EventCount} 次活动 - ¥${venue.TotalRevenue?.toLocaleString()}`
).join('\n')}
` : ''}

报告生成时间: ${new Date().toLocaleString('zh-CN')}
  `.trim()
  
  const blob = new Blob([reportContent], { type: 'text/plain;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `场地活动统计报表_${reportForm.StartDate}_${reportForm.EndDate}.txt`
  a.click()
  URL.revokeObjectURL(url)
}

// 组件挂载时获取数据
onMounted(() => {
  fetchEvents()
  // 设置默认日期范围（最近30天）
  const today = new Date()
  const thirtyDaysAgo = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000)
  reportForm.StartDate = thirtyDaysAgo.toISOString().split('T')[0]
  reportForm.EndDate = today.toISOString().split('T')[0]
  
  // 初始化员工职位为当前用户身份
  reservationForm.StaffPosition = getCurrentUserRole()
})
</script>

<style scoped>
.venue-event-management {
  padding: 24px;
}

/* 复用前面的通用样式 */
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

/* 筛选区域 */
.filter-section {
  margin-bottom: 24px;
}

.filter-group {
  display: flex;
  gap: 16px;
  align-items: center;
}

.search-box {
  flex: 1;
  max-width: 300px;
}

.search-input,
.filter-select {
  width: 100%;
  padding: 10px 16px;
  border: 1px solid #dcdfe6;
  border-radius: 6px;
  font-size: 14px;
  transition: border-color 0.3s ease;
}

.search-input:focus,
.filter-select:focus {
  outline: none;
  border-color: #409eff;
}

.filter-dropdown {
  min-width: 150px;
}

/* 活动列表样式 */
.events-list {
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

.events-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(450px, 1fr));
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

.event-card.pending-approval {
  border-left: 4px solid #e6a23c;
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

.event-status {
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 500;
  white-space: nowrap;
  margin-left: 12px;
}

.status-pending { background: #fdf6ec; color: #e6a23c; }
.status-approved { background: #f0f9ff; color: #409eff; }
.status-rejected { background: #fef0f0; color: #f56c6c; }
.status-preparing { background: #f4f4f5; color: #909399; }
.status-ongoing { background: #e1f3d8; color: #67c23a; }
.status-finished { background: #ebeef5; color: #606266; }
.status-cancelled { background: #fef0f0; color: #f56c6c; }
.status-settled { background: #e1f3d8; color: #67c23a; }

.event-details {
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

.event-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
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

.action-btn.approve {
  background: #e1f3d8;
  color: #67c23a;
}

.action-btn.approve:hover {
  background: #67c23a;
  color: white;
}

.action-btn.reject {
  background: #fef0f0;
  color: #f56c6c;
}

.action-btn.reject:hover {
  background: #f56c6c;
  color: white;
}

.action-btn.edit {
  background: #f0f9ff;
  color: #409eff;
}

.action-btn.edit:hover {
  background: #409eff;
  color: white;
}

.action-btn.cancel {
  background: #fdf6ec;
  color: #e6a23c;
}

.action-btn.cancel:hover {
  background: #e6a23c;
  color: white;
}

.action-btn.settle {
  background: #e1f3d8;
  color: #67c23a;
}

.action-btn.settle:hover {
  background: #67c23a;
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
  max-width: 900px;
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

.readonly-input {
  background-color: #f5f7fa !important;
  color: #606266 !important;
  cursor: not-allowed;
}

.form-hint {
  display: block;
  color: #909399;
  font-size: 12px;
  margin-top: 4px;
  line-height: 1.4;
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

/* 结算摘要 */
.settlement-summary {
  background: #f8f9fa;
  border-radius: 6px;
  padding: 16px;
  margin-bottom: 20px;
}

.summary-item {
  display: flex;
  justify-content: space-between;
  padding: 8px 0;
}

.summary-item.total {
  border-top: 1px solid #dcdfe6;
  font-weight: 600;
  font-size: 16px;
  color: #303133;
}

/* 活动详情 */
.detail-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
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

.participants-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.participant-tag {
  background: #ecf5ff;
  color: #409eff;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
}

/* 报表样式 */
.report-content {
  margin-top: 24px;
}

.report-summary {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 16px;
  margin-bottom: 24px;
}

.summary-card {
  text-align: center;
  padding: 20px;
  background: #f8f9fa;
  border-radius: 8px;
  border: 1px solid #ebeef5;
}

.summary-label {
  font-size: 14px;
  color: #909399;
  margin-bottom: 8px;
}

.summary-value {
  font-size: 24px;
  font-weight: 600;
  color: #303133;
}

.popular-venues {
  background: #f8f9fa;
  border-radius: 6px;
  padding: 16px;
}

.popular-venues h4 {
  margin: 0 0 12px 0;
  color: #303133;
}

.venues-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.venue-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  background: white;
  border-radius: 4px;
  border: 1px solid #ebeef5;
}

.venue-name {
  font-weight: 500;
  color: #303133;
}

.venue-count {
  color: #909399;
  font-size: 14px;
}

.venue-revenue {
  color: #67c23a;
  font-weight: 600;
}
</style>
