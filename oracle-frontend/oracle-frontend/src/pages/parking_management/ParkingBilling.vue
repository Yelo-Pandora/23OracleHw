<template>
  <div class="parking-billing">
    <div class="page-header">
      <button @click="goBack" class="back-btn">←</button>
      <h2>停车场出入车计费</h2>
    </div>

    <!-- 功能选择 -->
    <div class="function-tabs">
      <button 
        @click="activeTab = 'entry'" 
        :class="['tab-btn', { active: activeTab === 'entry' }]"
      >
        🚗 车辆入场
      </button>
      <button 
        @click="activeTab = 'exit'" 
        :class="['tab-btn', { active: activeTab === 'exit' }]"
      >
        🚪 车辆出场
      </button>
      <button 
        @click="activeTab = 'payment'" 
        :class="['tab-btn', { active: activeTab === 'payment' }]"
      >
        💰 停车费支付
      </button>
      <button 
        @click="activeTab = 'records'" 
        :class="['tab-btn', { active: activeTab === 'records' }]"
      >
        📋 支付记录
      </button>
    </div>

    <!-- 车辆入场 -->
    <div v-if="activeTab === 'entry'" class="tab-content">
      <div class="form-section">
        <h3>车辆入场登记</h3>
        <div class="form-group">
          <label>车牌号：</label>
          <input 
            v-model="entryForm.licensePlate" 
            type="text" 
            placeholder="请输入车牌号"
            class="form-input"
          />
        </div>
        <div class="form-group">
          <label>选择停车场：</label>
          <select v-model="selectedParkingLot" class="form-select" @change="loadAvailableSpaces">
            <option v-for="lot in parkingLotOptions" :key="lot.AreaId" :value="String(lot.AreaId)">
              {{ lot.ParkingLotName || (`停车场${lot.AreaId}`) }}
            </option>
          </select>
        </div>
        <div class="form-group">
          <label>选择车位：</label>
          <select v-model="entryForm.parkingSpaceId" class="form-select">
            <option value="">请选择车位</option>
            <option v-for="space in availableSpaces" :key="space.id" :value="space.id">
              {{ space.name }} ({{ space.status }})
            </option>
          </select>
        </div>
        <button @click="processVehicleEntry" class="submit-btn" :disabled="loading">
          {{ loading ? '处理中...' : '确认入场' }}
        </button>
      </div>
    </div>

    <!-- 车辆出场 -->
    <div v-if="activeTab === 'exit'" class="tab-content">
      <div class="form-section">
        <h3>车辆出场登记</h3>
        <div class="form-group">
          <label>车牌号：</label>
          <input 
            v-model="exitForm.licensePlate" 
            type="text" 
            placeholder="请输入车牌号"
            class="form-input"
          />
        </div>
        <button @click="processVehicleExit" class="submit-btn" :disabled="loading">
          {{ loading ? '处理中...' : '确认出场' }}
        </button>
        
        <!-- 出场结果面板移除：改为直接跳转支付页，避免在此冗余展示 -->
      </div>
    </div>

    <!-- 停车费支付 -->
    <div v-if="activeTab === 'payment'" class="tab-content">
      <div class="form-section">
        <h3>停车费支付</h3>
        <div class="form-group">
          <label>车牌号：</label>
          <input 
            v-model="paymentForm.licensePlate" 
            type="text" 
            placeholder="请输入车牌号"
            class="form-input"
          />
        </div>
        <div class="form-group">
          <label>车位号：</label>
          <input 
            v-model="paymentForm.parkingSpaceId" 
            type="number" 
            placeholder="请输入车位号"
            class="form-input"
            :readonly="true"
          />
        </div>
        <div class="form-group">
          <label>停车开始时间：</label>
          <input 
            v-model="paymentForm.parkStart" 
            type="datetime-local" 
            class="form-input"
            :readonly="true"
          />
        </div>
        <div class="form-group">
          <label>停车结束时间：</label>
          <input 
            v-model="paymentForm.parkEnd" 
            type="datetime-local" 
            class="form-input"
            :readonly="true"
          />
        </div>
        <div class="form-group">
          <label>总费用：</label>
          <input 
            v-model="paymentForm.totalFee" 
            type="number" 
            step="0.01"
            placeholder="请输入费用"
            class="form-input"
            :readonly="true"
          />
        </div>
        <div class="form-group">
          <label>支付方式：</label>
          <select v-model="paymentForm.paymentMethod" class="form-select">
            <option value="">请选择支付方式</option>
            <option value="现金">现金</option>
            <option value="微信">微信</option>
            <option value="支付宝">支付宝</option>
            <option value="银行卡">银行卡</option>
          </select>
        </div>
        <div class="form-group">
          <label>支付凭证号：</label>
          <input 
            v-model="paymentForm.paymentReference" 
            type="text" 
            placeholder="请输入支付凭证号（可选）"
            class="form-input"
          />
        </div>
        <button @click="processPayment" class="submit-btn" :disabled="loading">
          {{ loading ? '处理中...' : '确认支付' }}
        </button>
      </div>
    </div>

    <!-- 支付记录 -->
    <div v-if="activeTab === 'records'" class="tab-content">
      <div class="records-section">
        <h3>支付记录查询</h3>
                 <div class="filter-controls">
           <select v-model="recordFilter.status" @change="loadPaymentRecords" class="form-select">
             <option value="all">全部记录</option>
             <option value="paid">已支付</option>
             <option value="unpaid">未支付</option>
           </select>
           <button @click="loadPaymentRecords" class="refresh-btn">🔄 刷新</button>
           <button @click="generatePaymentRecords" class="generate-btn">⚡ 生成支付记录</button>
           <span class="record-count">当前记录数: {{ paymentRecords.length }}</span>
         </div>
        
                 <div class="records-table-container">
           <div v-if="paymentRecords.length === 0" class="empty-state">
             <div class="empty-icon">📋</div>
             <h4>暂无支付记录</h4>
             <p>当前没有找到支付记录，请点击"生成支付记录"按钮来为现有停车记录生成支付信息。</p>
             <button @click="generatePaymentRecords" class="generate-btn">⚡ 立即生成支付记录</button>
           </div>
           <table v-else class="records-table">
             <thead>
               <tr>
                 <th>车牌号</th>
                 <th>车位号</th>
                 <th>入场时间</th>
                 <th>出场时间</th>
                 <th>停车时长</th>
                 <th>费用</th>
                 <th>支付状态</th>
                 <th>支付时间</th>
                 <th>支付方式</th>
               </tr>
             </thead>
             <tbody>
               <tr v-for="record in paymentRecords" :key="record.id">
                 <td>{{ record.licensePlateNumber }}</td>
                 <td>{{ record.parkingSpaceId }}</td>
                 <td>{{ record.displayParkStart || '-' }}</td>
                 <td>{{ record.displayParkEnd || '-' }}</td>
                 <td>{{ formatDuration(record.parkingDuration) }}</td>
                 <td>¥{{ record.totalFee }}</td>
                 <td>
                   <span :class="getPaymentStatusClass(record.paymentStatus)">
                     {{ record.paymentStatus }}
                   </span>
                 </td>
                 <td>{{ record.paymentTime ? formatDateTime(record.paymentTime) : '-' }}</td>
                 <td>{{ record.paymentMethod || '-' }}</td>
               </tr>
             </tbody>
           </table>
         </div>
      </div>
    </div>

    <!-- 消息提示 -->
    <div v-if="message" :class="['message', messageType]">
      {{ message }}
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'

// 路由
const router = useRouter()
const userStore = useUserStore()
const getOperatorAccount = () => userStore?.userInfo?.account || userStore?.token || 'unknown'

// 响应式数据
const activeTab = ref('entry')
const loading = ref(false)
const message = ref('')
const messageType = ref('success')

// 表单数据
const entryForm = ref({
  licensePlate: '',
  parkingSpaceId: ''
})

const exitForm = ref({
  licensePlate: ''
})

const paymentForm = ref({
  licensePlate: '',
  parkingSpaceId: '',
  parkStart: '',
  parkEnd: '',
  totalFee: '',
  paymentMethod: '',
  paymentReference: ''
})

const recordFilter = ref({
  status: 'all'
})

// 数据
const parkingLotOptions = ref([])
const selectedParkingLot = ref('')
const availableSpaces = ref([])
const exitResult = ref(null)
const paymentRecords = ref([])

// 方法
const processVehicleEntry = async () => {
  if (!entryForm.value.licensePlate || !entryForm.value.parkingSpaceId) {
    showMessage('请填写完整信息', 'error')
    return
  }

  try {
    loading.value = true
    const response = await fetch('/api/Parking/Entry', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        licensePlateNumber: entryForm.value.licensePlate,
        parkingSpaceId: parseInt(entryForm.value.parkingSpaceId),
        operatorAccount: getOperatorAccount()
      })
    })

    const data = await response.json()
    if (response.ok && (data.success || data.Success)) {
      showMessage('车辆入场成功！', 'success')
      entryForm.value = { licensePlate: '', parkingSpaceId: '' }
      loadAvailableSpaces()
      
      // 车辆入场成功后，也刷新支付记录
      setTimeout(() => {
        loadPaymentRecords()
      }, 1000)
    } else {
      showMessage(data.error || '车辆入场失败', 'error')
    }
  } catch (error) {
    console.error('车辆入场出错:', error)
    showMessage('车辆入场时发生错误', 'error')
  } finally {
    loading.value = false
  }
}

const processVehicleExit = async () => {
  if (!exitForm.value.licensePlate) {
    showMessage('请输入车牌号', 'error')
    return
  }

  try {
    loading.value = true
    const response = await fetch('/api/Parking/Exit', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        licensePlateNumber: exitForm.value.licensePlate,
        operatorAccount: getOperatorAccount()
      })
    })

    const data = await response.json()
    if (response.ok && (data.success || data.Success)) {
      exitResult.value = data.data || data.Data
      showMessage('车辆出场成功！', 'success')
      
      // 车辆出场成功后，自动刷新支付记录
      setTimeout(() => {
        loadPaymentRecords()
      }, 1000)

      // 自动跳转到支付页并预填信息（仅需选择支付方式）
      try {
        const r = exitResult.value || {}
        activeTab.value = 'payment'
        paymentForm.value = {
          licensePlate: r.licensePlateNumber || '',
          parkingSpaceId: r.parkingSpaceId != null ? String(r.parkingSpaceId) : '',
          parkStart: toBeijingInput(r.parkStart),
          parkEnd: toBeijingInput(r.parkEnd),
          // keep raw full-precision timestamps for backend matching
          rawParkStart: r.parkStart || '',
          rawParkEnd: r.parkEnd || '',
          totalFee: r.totalFee != null ? String(r.totalFee) : '',
          paymentMethod: '',
          paymentReference: ''
        }
      } catch (_) {}
    } else {
      showMessage(data.error || '车辆出场失败', 'error')
    }
  } catch (error) {
    console.error('车辆出场出错:', error)
    showMessage('车辆出场时发生错误', 'error')
  } finally {
    loading.value = false
  }
}

const processPayment = async () => {
  if (!paymentForm.value.licensePlate || !paymentForm.value.parkingSpaceId || 
      !paymentForm.value.parkStart || !paymentForm.value.totalFee || 
      !paymentForm.value.paymentMethod) {
    showMessage('请填写完整信息', 'error')
    return
  }

  try {
    loading.value = true
    const response = await fetch(`/api/Parking/Pay?operatorAccount=${encodeURIComponent(getOperatorAccount())}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        licensePlateNumber: paymentForm.value.licensePlate,
        parkingSpaceId: parseInt(paymentForm.value.parkingSpaceId),
        // use raw timestamps if present to avoid precision/timezone mismatch
        parkStart: paymentForm.value.rawParkStart || beijingInputToIso(paymentForm.value.parkStart),
        parkEnd: paymentForm.value.rawParkEnd || (paymentForm.value.parkEnd ? beijingInputToIso(paymentForm.value.parkEnd) : null),
        totalFee: parseFloat(paymentForm.value.totalFee),
        paymentMethod: paymentForm.value.paymentMethod,
        paymentReference: paymentForm.value.paymentReference || ''
      })
    })

    const data = await response.json()
    if (response.ok && (data.success || data.Success)) {
      showMessage('支付成功！', 'success')
      paymentForm.value = {
        licensePlate: '',
        parkingSpaceId: '',
        parkStart: '',
        parkEnd: '',
        rawParkStart: '',
        rawParkEnd: '',
        totalFee: '',
        paymentMethod: '',
        paymentReference: ''
      }
      // 切到“支付记录”并查看“已支付”
      activeTab.value = 'records'
      recordFilter.value.status = 'paid'
      await loadPaymentRecords()
    } else {
      showMessage(data.error || '支付失败', 'error')
    }
  } catch (error) {
    console.error('支付出错:', error)
    showMessage('支付时发生错误', 'error')
  } finally {
    loading.value = false
  }
}

const loadPaymentRecords = async () => {
  try {
    console.log('开始加载支付记录...')
    // 先尝试获取支付记录
    const response = await fetch(`/api/Parking/PaymentRecords?status=${recordFilter.value.status}`)
    const data = await response.json()
    
    console.log('支付记录API响应:', response.status, data)
    
    if (response.ok && (data.success || data.Success)) {
      const records = data.data || data.Data || []
      console.log('获取到的支付记录数量:', records.length)
      if (records.length > 0) {
        console.log('样例原始记录:', records[0])
      }
      
      // 统一字段命名，生成稳定的 key，避免大小写不一致导致模板取值为空
      const normalized = records.map((r, index) => {
        const licensePlateNumber = r.licensePlateNumber || r.LicensePlateNumber || ''
        const parkStart = r.parkStart || r.ParkStart || null
        const parkEnd = r.parkEnd || r.ParkEnd || null
        return {
          id: `${licensePlateNumber}-${parkStart || index}`,
          licensePlateNumber,
          parkingSpaceId: r.parkingSpaceId ?? r.ParkingSpaceId ?? '-',
          parkStart,
          parkEnd,
          displayParkStart: formatDateTime(parkStart),
          displayParkEnd: parkEnd ? formatDateTime(parkEnd) : '-',
          // 有些后端返回 decimal -> string/number，统一成数字并保底
          totalFee: typeof (r.totalFee ?? r.TotalFee) === 'string' 
            ? parseFloat(r.totalFee ?? r.TotalFee) || 0 
            : (r.totalFee ?? r.TotalFee ?? 0),
          paymentStatus: r.paymentStatus || r.PaymentStatus || '未支付',
          paymentTime: r.paymentTime || r.PaymentTime || null,
          paymentMethod: r.paymentMethod || r.PaymentMethod || ''
        }
      })
      
      paymentRecords.value = normalized
      if (normalized.length > 0) {
        console.log('样例标准化后记录:', normalized[0])
      }
      
      
      
    } else {
      console.error('加载支付记录失败:', data.error)
      showMessage('加载支付记录失败: ' + (data.error || '未知错误'), 'error')
    }
  } catch (error) {
    console.error('加载支付记录出错:', error)
    showMessage('加载支付记录时发生错误', 'error')
  }
}


const generatePaymentRecords = async () => {
  try {
    console.log('开始生成支付记录...')
    
    // 使用更宽的时间范围，确保能覆盖到您的测试数据
    const startDate = new Date('2025-08-01T00:00:00').toISOString()
    const endDate = new Date('2025-12-31T23:59:59').toISOString()
    
    console.log('生成支付记录时间范围:', startDate, '到', endDate)
    
    const response = await fetch('/api/Parking/GeneratePaymentRecords', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        startDate: startDate,
        endDate: endDate,
        forceRegenerate: false  // 不强制，每次只在不存在时生成
      })
    })
    
    console.log('生成支付记录API响应:', response.status)
    const data = await response.json()
    console.log('生成支付记录API数据:', data)
    
    if (response.ok && (data.success || data.Success)) {
      console.log('支付记录生成成功:', data.message)
      showMessage('已自动生成支付记录', 'success')
      // 生成成功后自动刷新记录
      setTimeout(() => {
        loadPaymentRecords()
      }, 1000)
      return true
    } else {
      console.log('生成支付记录失败:', data.error)
      showMessage('生成支付记录失败: ' + (data.error || '未知错误'), 'error')
      return false
    }
  } catch (error) {
    console.error('生成支付记录出错:', error)
    showMessage('生成支付记录时发生错误', 'error')
    return false
  }
}

const loadAvailableSpaces = async () => {
  try {
    if (!selectedParkingLot.value) {
      availableSpaces.value = []
      return
    }
    const params = new URLSearchParams()
    params.set('areaId', String(Number(selectedParkingLot.value)))
    const acc = getOperatorAccount && getOperatorAccount()
    if (acc && acc !== 'guest-token' && acc !== 'unknown') {
      params.set('operatorAccount', acc)
    }
    const response = await fetch(`/api/Parking/spaces?${params.toString()}`)
    const data = await response.json().catch(() => ({}))
    
    if (response.ok && (data.success || data.Success || Array.isArray(data))) {
      const spaces = (data.data || data.Data || data) || []
      // 过滤出可用的车位（兼容中英文与大小写）
      availableSpaces.value = spaces
        .map(s => ({
          id: s.parkingSpaceId ?? s.ParkingSpaceId ?? s.id,
          status: s.status ?? s.Status ?? '',
        }))
        .filter(s => {
          const st = String(s.status).toLowerCase()
          return st === '空闲' || st === '无车' || st === 'available' || st === 'free'
        })
        .map(s => ({ id: s.id, name: `车位${s.id}`, status: s.status }))
    } else {
      console.error('获取车位信息失败:', response.status, data?.error || '')
      availableSpaces.value = []
    }
  } catch (error) {
    console.error('获取车位信息出错:', error)
    availableSpaces.value = []
  }
}

const goBack = () => {
  router.push('/parking-management')
}

const showMessage = (msg, type = 'success') => {
  message.value = msg
  messageType.value = type
  setTimeout(() => {
    message.value = ''
  }, 3000)
}

const formatDateTime = (dateTime) => {
  if (!dateTime) return '-'
  const s = String(dateTime).trim()
  // 1) 无时区的斜杠/连字符格式：按 UTC 裸时间解析，再转北京时间
  let m = s.match(/^(\d{4})[\/\-](\d{1,2})[\/\-](\d{1,2})[ T](\d{1,2}):(\d{1,2})(?::(\d{1,2}))?$/)
  if (m) {
    const [, y, mo, d, h, mi, se] = m
    const utcMs = Date.UTC(Number(y), Number(mo) - 1, Number(d), Number(h), Number(mi), Number(se || '0'))
    const bj = new Date(utcMs + 8 * 60 * 60 * 1000) // UTC+8
    const pad = (n) => String(n).padStart(2, '0')
    const yyyy = bj.getUTCFullYear()
    const MM = pad(bj.getUTCMonth() + 1)
    const DD = pad(bj.getUTCDate())
    const HH = pad(bj.getUTCHours())
    const MMm = pad(bj.getUTCMinutes())
    const SS = pad(bj.getUTCSeconds())
    return `${yyyy}/${MM}/${DD} ${HH}:${MMm}:${SS}`
  }
  // 2) 标准 ISO（带 Z/±HH:mm）：按其时区解析后以北京时间显示
  if (/Z$|[+-]\d{2}:\d{2}$/.test(s)) {
    return new Date(s).toLocaleString('zh-CN', { timeZone: 'Asia/Shanghai', hour12: false })
  }
  // 3) 其他兜底
  const t = new Date(s)
  if (isNaN(t.getTime())) return '-'
  return t.toLocaleString('zh-CN', { timeZone: 'Asia/Shanghai', hour12: false })
}

// 固定使用北京时间(UTC+8)进行显示/解析
const BEIJING_OFFSET_MIN = 8 * 60

// 解析任意后端时间到 UTC 毫秒
const parseToUtcMillis = (dateTime) => {
  if (dateTime == null || dateTime === '') return null
  if (typeof dateTime === 'number') return Number(dateTime)
  const s = String(dateTime).trim()
  // 含 Z 或时区偏移，直接按内置解析为 UTC 毫秒
  if (/Z$|[+-]\d{2}:\d{2}$/.test(s)) return new Date(s).getTime()
  // 无时区：按 UTC 时间解释（常见后端行为），再转换
  let m = s.match(/^(\d{4})-(\d{1,2})-(\d{1,2})[ T](\d{1,2}):(\d{1,2})(?::(\d{1,2}))?$/)
  if (m) {
    const [, y, mo, d, h, mi, se] = m
    // 无时区：按UTC时间解释
    return Date.UTC(Number(y), Number(mo) - 1, Number(d), Number(h), Number(mi), Number(se || '0'))
  }
  // 支持斜杠格式 YYYY/M/D HH:mm:ss 也按UTC解释
  m = s.match(/^(\d{4})\/(\d{1,2})\/(\d{1,2})[ T](\d{1,2}):(\d{1,2})(?::(\d{1,2}))?$/)
  if (m) {
    const [, y, mo, d, h, mi, se] = m
    return Date.UTC(Number(y), Number(mo) - 1, Number(d), Number(h), Number(mi), Number(se || '0'))
  }
  // 兜底
  const t = new Date(s).getTime()
  return isNaN(t) ? null : t
}

// 将任意时间转换为北京时间的 "YYYY-MM-DDTHH:MM" 字符串（用于 input[type=datetime-local]）
const toBeijingInput = (dateTime) => {
  const utc = parseToUtcMillis(dateTime)
  if (utc == null) return ''
  const bj = new Date(utc + BEIJING_OFFSET_MIN * 60000) // 转到北京时区的本地表现
  const pad = (n) => String(n).padStart(2, '0')
  const yyyy = bj.getUTCFullYear()
  const mm = pad(bj.getUTCMonth() + 1)
  const dd = pad(bj.getUTCDate())
  const hh = pad(bj.getUTCHours())
  const mi = pad(bj.getUTCMinutes())
  return `${yyyy}-${mm}-${dd}T${hh}:${mi}`
}

// 将北京时间的 "YYYY-MM-DDTHH:MM" 输入值转回 ISO(UTC) 字符串（给后端）
const beijingInputToIso = (ymdHm) => {
  if (!ymdHm) return null
  const m = String(ymdHm).trim().match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/)
  if (!m) return null
  const [, y, mo, d, h, mi] = m
  // 先得到“北京时间对应的 UTC 毫秒”（减去 8 小时）
  const utcMs = Date.UTC(Number(y), Number(mo) - 1, Number(d), Number(h), Number(mi)) - BEIJING_OFFSET_MIN * 60000
  return new Date(utcMs).toISOString()
}

const formatDuration = (duration) => {
  if (!duration) return '-'
  if (typeof duration === 'string') {
    // 解析TimeSpan格式
    const parts = duration.split(':')
    if (parts.length >= 3) {
      const hours = parseInt(parts[0]) || 0
      const minutes = parseInt(parts[1]) || 0
      if (hours > 0) {
        return `${hours}小时${minutes}分钟`
      } else {
        return `${minutes}分钟`
      }
    }
  }
  return duration.toString()
}

const getPaymentStatusClass = (status) => {
  switch (status) {
    case '已支付': return 'status-paid'
    case '未支付': return 'status-unpaid'
    default: return 'status-unknown'
  }
}

// 根据车牌自动获取未支付记录并填充支付表单
const autofillPaymentByPlate = async (plate) => {
  if (!plate) return
  try {
    const response = await fetch(`/api/Parking/PaymentRecords?status=unpaid`)
    const data = await response.json()
    if (response.ok && (data.success || data.Success)) {
      const records = data.data || data.Data || []
      // 兼容大小写字段
      const normalized = records.map((r, index) => {
        const licensePlateNumber = r.licensePlateNumber || r.LicensePlateNumber || ''
        const parkStart = r.parkStart || r.ParkStart || null
        return {
          id: `${licensePlateNumber}-${parkStart || index}`,
          licensePlateNumber,
          parkingSpaceId: r.parkingSpaceId ?? r.ParkingSpaceId ?? '-',
          parkStart,
          parkEnd: r.parkEnd || r.ParkEnd || null,
          totalFee: typeof (r.totalFee ?? r.TotalFee) === 'string'
            ? parseFloat(r.totalFee ?? r.TotalFee) || 0
            : (r.totalFee ?? r.TotalFee ?? 0),
          paymentStatus: r.paymentStatus || r.PaymentStatus || '未支付',
          paymentTime: r.paymentTime || r.PaymentTime || null,
          paymentMethod: r.paymentMethod || r.PaymentMethod || ''
        }
      })
      // 匹配车牌（不区分大小写，忽略空格）
      const normalizePlate = (s) => (s || '').toString().trim().toUpperCase()
      const target = normalized.find(x => normalizePlate(x.licensePlateNumber) === normalizePlate(plate))
      if (target) {
        paymentForm.value = {
          licensePlate: target.licensePlateNumber,
          parkingSpaceId: target.parkingSpaceId != null ? String(target.parkingSpaceId) : '',
          parkStart: toBeijingInput(target.parkStart),
          parkEnd: toBeijingInput(target.parkEnd),
          rawParkStart: target.parkStart || '',
          rawParkEnd: target.parkEnd || '',
          totalFee: target.totalFee != null ? String(target.totalFee) : '',
          paymentMethod: '',
          paymentReference: ''
        }
        showMessage('已根据车牌自动填充未支付记录', 'success')
      } else {
        showMessage('未找到该车牌的未支付记录', 'error')
      }
    } else {
      showMessage('查询未支付记录失败', 'error')
    }
  } catch (e) {
    console.error('自动填充失败:', e)
    showMessage('自动填充时发生错误', 'error')
  }
}

// 监听车牌输入，自动填充其余字段（防抖）
let autofillTimer = null
watch(() => paymentForm.value.licensePlate, (val) => {
  if (autofillTimer) clearTimeout(autofillTimer)
  if (!val) return
  autofillTimer = setTimeout(() => {
    autofillPaymentByPlate(val)
  }, 400)
})

// 加载停车场选项
const loadParkingLotOptions = async () => {
  try {
    const resp = await fetch('/api/Parking/ParkingLots')
    if (resp.ok) {
      const data = await resp.json()
      const list = (data.data || data.Data || data) || []
      parkingLotOptions.value = list
      if (!selectedParkingLot.value && list.length > 0) {
        selectedParkingLot.value = String(list[0].AreaId)
      }
      await loadAvailableSpaces()
    } else {
      console.error('加载停车场列表失败:', resp.status)
    }
  } catch (e) {
    console.error('加载停车场列表出错:', e)
  }
}

// 生命周期
onMounted(() => {
  loadParkingLotOptions()
  loadPaymentRecords()
})
</script>

<style scoped>
.parking-billing {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

/* 页面头部 */
.page-header {
  display: flex;
  align-items: center;
  gap: 15px;
  margin-bottom: 20px;
}

.back-btn {
  background: #007bff;
  color: white;
  border: none;
  border-radius: 50%;
  width: 40px;
  height: 40px;
  font-size: 18px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.back-btn:hover {
  background: #0056b3;
  transform: translateY(-1px);
  box-shadow: 0 4px 8px rgba(0,0,0,0.15);
}

h2, h3, h4 {
  color: #333;
  margin-bottom: 20px;
}

/* 功能标签 */
.function-tabs {
  display: flex;
  gap: 10px;
  margin-bottom: 30px;
  border-bottom: 2px solid #e9ecef;
}

.tab-btn {
  padding: 12px 24px;
  border: none;
  background: #f8f9fa;
  color: #6c757d;
  border-radius: 8px 8px 0 0;
  cursor: pointer;
  font-size: 16px;
  font-weight: 500;
  transition: all 0.2s;
}

.tab-btn:hover {
  background: #e9ecef;
  color: #495057;
}

.tab-btn.active {
  background: #007bff;
  color: white;
}

/* 标签内容 */
.tab-content {
  background: white;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 30px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

/* 表单样式 */
.form-section {
  max-width: 600px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  font-weight: bold;
  color: #333;
  margin-bottom: 8px;
}

.form-input, .form-select {
  width: 100%;
  padding: 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  transition: border-color 0.2s;
}

.form-input:focus, .form-select:focus {
  outline: none;
  border-color: #007bff;
  box-shadow: 0 0 0 2px rgba(0, 123, 255, 0.25);
}

.submit-btn {
  background: #28a745;
  color: white;
  border: none;
  padding: 12px 30px;
  border-radius: 6px;
  font-size: 16px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.submit-btn:hover:not(:disabled) {
  background: #218838;
  transform: translateY(-1px);
}

.submit-btn:disabled {
  background: #6c757d;
  cursor: not-allowed;
  transform: none;
}

/* 出场结果 */
.exit-result {
  margin-top: 30px;
  padding: 20px;
  background: #f8f9fa;
  border-radius: 8px;
  border: 1px solid #dee2e6;
}

.fee-details {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 15px;
}

.fee-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px;
  background: white;
  border-radius: 4px;
  border: 1px solid #e9ecef;
}

.fee-item.total-fee {
  background: #d4edda;
  border-color: #c3e6cb;
  font-weight: bold;
  font-size: 16px;
}

.fee-item .label {
  font-weight: 500;
  color: #495057;
}

.fee-item .value {
  color: #212529;
  font-weight: 500;
}

/* 支付记录 */
.records-section {
  width: 100%;
}

.filter-controls {
  display: flex;
  gap: 15px;
  margin-bottom: 20px;
  align-items: center;
}

.refresh-btn {
  background: #17a2b8;
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 4px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.refresh-btn:hover {
  background: #138496;
}

.generate-btn {
  background: #ffc107;
  color: #212529;
  border: none;
  padding: 8px 16px;
  border-radius: 4px;
  cursor: pointer;
  transition: background-color 0.2s;
  font-weight: 500;
}

.generate-btn:hover {
  background: #e0a800;
}

.record-count {
  color: #6c757d;
  font-size: 14px;
  font-weight: 500;
  margin-left: 10px;
}

.empty-state {
  text-align: center;
  padding: 60px 20px;
  color: #6c757d;
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 20px;
}

.empty-state h4 {
  color: #495057;
  margin-bottom: 10px;
}

.empty-state p {
  margin-bottom: 30px;
  line-height: 1.6;
}

.records-table-container {
  overflow-x: auto;
}

.records-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}

.records-table th,
.records-table td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #ddd;
}

.records-table th {
  background: #f8f9fa;
  font-weight: bold;
  color: #333;
}

.records-table tbody tr:hover {
  background: #f8f9fa;
}

/* 状态样式 */
.status-paid {
  color: #28a745;
  font-weight: bold;
}

.status-unpaid {
  color: #dc3545;
  font-weight: bold;
}

.status-unknown {
  color: #6c757d;
  font-weight: bold;
}

/* 消息提示 */
.message {
  position: fixed;
  top: 20px;
  right: 20px;
  padding: 15px 20px;
  border-radius: 6px;
  font-weight: 500;
  z-index: 1000;
  max-width: 400px;
}

.message.success {
  background: #d4edda;
  color: #155724;
  border: 1px solid #c3e6cb;
}

.message.error {
  background: #f8d7da;
  color: #721c24;
  border: 1px solid #f5c6cb;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .function-tabs {
    flex-wrap: wrap;
  }
  
  .tab-btn {
    flex: 1;
    min-width: 120px;
  }
  
  .fee-details {
    grid-template-columns: 1fr;
  }
  
  .filter-controls {
    flex-direction: column;
    align-items: stretch;
  }
  
  .records-table {
    font-size: 12px;
  }
  
  .records-table th,
  .records-table td {
    padding: 8px;
  }
}
</style>
