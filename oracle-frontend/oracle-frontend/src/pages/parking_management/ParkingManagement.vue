<template>
  <DashboardLayout>
    <div class="parking-management" v-if="!isChildActive">
    <h2>停车场信息管理</h2>
    
    <!-- 上半部分：停车场状态表格 -->
    <div class="parking-status-section">
      <h3>停车场状态概览</h3>
      <div class="status-table-container">
        <table class="status-table">
          <thead>
            <tr>
              <th>停车场ID</th>
              <th>停车场名称</th>
              <th>总车位数</th>
              <th>已占用</th>
              <th>空闲</th>
              <th>占用率</th>
              <th>停车费(元/小时)</th>
              <th>状态</th>
              <th>最后更新</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="lot in parkingLots" :key="lot.areaId" 
                :class="{ 'selected-row': selectedParkingLot === lot.areaId.toString() }"
                @click="selectParkingLot(lot.areaId.toString())">
              <td>{{ lot.areaId }}</td>
              <td>{{ lot.name }}</td>
              <td>{{ lot.totalSpaces }}</td>
              <td class="occupied-count">{{ lot.occupiedSpaces }}</td>
              <td class="available-count">{{ lot.availableSpaces }}</td>
              <td>
                <div class="occupancy-bar">
                  <div class="occupancy-fill" :style="{ width: (lot.occupancyRate * 100) + '%' }"></div>
                  <span class="occupancy-text">{{ (lot.occupancyRate * 100).toFixed(1) }}%</span>
                </div>
              </td>
              <td class="fee">¥{{ lot.parkingFee }}</td>
              <td>
                <span :class="getStatusClass(lot.status)">{{ lot.status }}</span>
              </td>
              <td>{{ formatDateTime(lot.lastUpdateTime) }}</td>
              <td>
                <button @click="editParkingLot(lot)" class="edit-btn">修改</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 下半部分：停车场平面图 -->
    <div class="parking-layout-section">
      <div class="section-header">
        <h3>停车场平面图 - {{ getSelectedParkingLotName() }}</h3>
        <div class="layout-controls">
          <button @click="refreshData" class="refresh-btn">🔄 刷新</button>
          <button @click="goToVehicleManagement" class="action-btn">🚗 车辆管理</button>
          <button @click="goToParkingBilling" class="action-btn">💰 计费管理</button>
          <button @click="goToParkingReport" class="action-btn">📈 停车报表</button>
        </div>
      </div>
      
      <div class="legend">
        <div class="legend-item">
          <div class="legend-color available"></div>
          <span>空闲</span>
        </div>
        <div class="legend-item">
          <div class="legend-color occupied"></div>
          <span>占用</span>
        </div>
        <div class="legend-item">
          <div class="legend-color maintenance"></div>
          <span>维护中</span>
        </div>
      </div>
      
      <!-- SVG停车场布局 -->
      <div class="svg-container">
        <svg :viewBox="`0 0 ${canvasSize.w} ${canvasSize.h}`" preserveAspectRatio="xMidYMid meet" @click="onSvgClick">
          <defs>
            <marker id="arrow" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto-start-reverse">
              <path d="M0,0 L0,6 L9,3 z" fill="#666" />
            </marker>
          </defs>
          
          <!-- 背景 -->
          <rect :width="canvasSize.w" :height="canvasSize.h" fill="#f7f7f7" stroke="#666" stroke-width="2" />
          
          <!-- 走道/过道 -->
          <g class="walkways">
            <rect :x="walk.inner.x" :y="walk.inner.y" :width="walk.inner.w" :height="walk.inner.h" 
                  fill="none" stroke="#bbb" stroke-width="1.5" stroke-dasharray="6 6" />
            <line v-for="(x,i) in walk.vertical" :key="'wv-'+i" 
                  :x1="x" :y1="walk.inner.y" :x2="x" :y2="walk.inner.y + walk.inner.h" 
                  stroke="#bbb" stroke-dasharray="8 8" />
            <line v-for="(y,i) in walk.horizontal" :key="'wh-'+i" 
                  :x1="walk.inner.x" :y1="y" :x2="walk.inner.x + walk.inner.w" :y2="y" 
                  stroke="#bbb" stroke-dasharray="8 8" />
          </g>
          
          <!-- 停车位网格 -->
          <g class="parking-slots">
            <g v-for="slot in parkingSlots" :key="slot.id">
              <polygon
                :points="getSlotPoints(slot)"
                :fill="getSlotFill(slot)"
                stroke="#222" 
                stroke-width="1"
                @click.stop="showSpaceDetail(slot)"
                @mouseenter="hoveredSlot = slot"
                @mouseleave="hoveredSlot = null"
                style="cursor: pointer;"
              />
              <text 
                :x="slot.x + slot.w/2" 
                :y="slot.y + slot.h/2" 
                text-anchor="middle" 
                dominant-baseline="middle" 
                fill="#fff" 
                font-size="10"
                font-weight="bold"
              >
                {{ slot.no }}
              </text>
            </g>
          </g>
          
          <!-- 入口和出口标识 -->
          <g class="entrance-exit">
            <rect x="20" y="20" width="80" height="30" fill="#4CAF50" stroke="#2E7D32" stroke-width="2" rx="5" />
            <text x="60" y="37" text-anchor="middle" dominant-baseline="middle" fill="white" font-size="12" font-weight="bold">入口</text>
            
            <rect :x="canvasSize.w - 100" y="20" width="80" height="30" fill="#F44336" stroke="#C62828" stroke-width="2" rx="5" />
            <text :x="canvasSize.w - 60" y="37" text-anchor="middle" dominant-baseline="middle" fill="white" font-size="12" font-weight="bold">出口</text>
          </g>
          
          <!-- 车道指示 -->
          <g class="road-indicators">
            <line x1="0" y1="50" x2="canvasSize.w" y2="50" stroke="#666" stroke-width="3" stroke-dasharray="10 5" />
            <text x="canvasSize.w/2" y="40" text-anchor="middle" dominant-baseline="middle" fill="#666" font-size="10">主车道</text>
          </g>
        </svg>
        
        <!-- 悬停提示 -->
        <div v-if="hoveredSlot" class="tooltip" :style="{ left: tooltipPosition.x + 'px', top: tooltipPosition.y + 'px' }">
          <div><b>车位编号：</b>{{ hoveredSlot.id }}</div>
          <div><b>状态：</b>{{ hoveredSlot.occupied ? '占用' : '空闲' }}</div>
          <div v-if="hoveredSlot.occupied && hoveredSlot.licensePlate">
            <b>车牌号：</b>{{ hoveredSlot.licensePlate }}
            </div>
          <div v-if="hoveredSlot.occupied && hoveredSlot.parkStart">
            <b>入场时间：</b>{{ formatDateTime(hoveredSlot.parkStart) }}
          </div>
        </div>
            </div>
          </div>
          
    <!-- 修改停车场信息弹窗 -->
    <div v-if="showEditModal" class="modal-overlay" @click="closeEditModal">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>修改停车场信息</h3>
          <button @click="closeEditModal" class="close-btn">×</button>
        </div>
        <div class="modal-body" v-if="editingLot">
          <div class="edit-form">
            <div class="form-group">
              <label>停车场ID：</label>
              <span class="readonly-field">{{ editingLot.areaId }}</span>
            </div>
            <div class="form-group">
              <label>停车场名称：</label>
              <span class="readonly-field">{{ editingLot.name }}</span>
            </div>
            <div class="form-group">
              <label>停车费(元/小时)：</label>
              <input type="number" v-model="editForm.parkingFee" min="0" class="form-input" />
            </div>
            <div class="form-group">
              <label>状态：</label>
              <select v-model="editForm.status" class="form-select">
                <option value="正常运营">正常运营</option>
                <option value="维护中">维护中</option>
                <option value="暂停服务">暂停服务</option>
              </select>
            </div>
            <div class="form-group">
              <label>备注：</label>
              <textarea v-model="editForm.remarks" class="form-textarea" placeholder="可选"></textarea>
            </div>
            <div class="form-actions">
              <button @click="saveParkingLotChanges" class="save-btn" :disabled="saving">保存</button>
              <button @click="closeEditModal" class="cancel-btn">取消</button>
            </div>
          </div>
        </div>
            </div>
          </div>
          
    <!-- 车位详情弹窗 -->
    <div v-if="showDetailModal" class="modal-overlay" @click="closeDetailModal">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>车位详情</h3>
          <button @click="closeDetailModal" class="close-btn">×</button>
        </div>
        <div class="modal-body" v-if="selectedSlot">
          <div class="detail-item">
            <span class="label">车位编号：</span>
            <span class="value">{{ selectedSlot.id }}</span>
          </div>
          <div class="detail-item">
            <span class="label">状态：</span>
            <span class="value" :class="selectedSlot.occupied ? 'occupied' : 'available'">
              {{ selectedSlot.occupied ? '占用' : '空闲' }}
            </span>
            </div>
          <div class="detail-item" v-if="selectedSlot.occupied">
            <span class="label">车牌号：</span>
            <span class="value">{{ selectedSlot.licensePlate || '未知' }}</span>
          </div>
          <div class="detail-item" v-if="selectedSlot.occupied">
            <span class="label">入场时间：</span>
            <span class="value">{{ selectedSlot.parkStart ? formatDateTime(selectedSlot.parkStart) : '未知' }}</span>
        </div>
          <div class="detail-item" v-if="selectedSlot.occupied">
            <span class="label">停放时长：</span>
            <span class="value">{{ selectedSlot.parkStart ? calculateDuration(selectedSlot.parkStart) : '未知' }}</span>
        </div>
        </div>
      </div>
    </div>
  </div>
  <router-view v-else />
</DashboardLayout>
</template>

<script setup>
import DashboardLayout from '@/components/BoardLayout.vue';
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useUserStore } from '@/user/user'

// 响应式数据
const selectedParkingLot = ref('')
const parkingLots = ref([])
const parkingSlots = ref([])
const showDetailModal = ref(false)
const selectedSlot = ref(null)
const hoveredSlot = ref(null)
const loading = ref(false)
const showEditModal = ref(false)
const editingLot = ref(null)
const editForm = ref({
  parkingFee: 0,
  status: '正常运营',
  remarks: ''
})
const saving = ref(false)
const currentUserAuthority = ref(0) // 当前用户权限等级
const currentUserAccount = ref('') // 当前用户账号

// 定时器
let refreshTimer = null

// 路由
const router = useRouter()
const userStore = useUserStore()
const route = useRoute()
const isChildActive = computed(() => (route.path || '').startsWith('/parking-management/') )

// SVG画布尺寸
const canvasSize = { w: 1200, h: 800 }

// 走道配置
const walk = computed(() => {
  const m = 20
  const inner = { x: m, y: m, w: canvasSize.w - 2*m, h: canvasSize.h - 2*m }
  const horizontal = [120, 200, 280, 360, 440, 520, 600, 680, 760, 840]
  return { inner, vertical: [], horizontal }
})

// 工具提示位置
const tooltipPosition = computed(() => {
  if (!hoveredSlot.value) return { x: 0, y: 0 }
  return { x: hoveredSlot.value.x + 50, y: hoveredSlot.value.y - 30 }
})

// 方法
const loadParkingData = async () => {
  try {
    loading.value = true
    console.log('开始加载停车场数据...')

    // 先加载停车场概览数据
    await loadParkingLots()
    console.log('停车场概览数据加载完成:', parkingLots.value)

    // 若未选择任何停车场，默认选中第一个
    if (!selectedParkingLot.value && parkingLots.value.length > 0) {
      selectedParkingLot.value = String(parkingLots.value[0].areaId)
      console.log('默认选择停车场:', selectedParkingLot.value)
    }

    // 然后加载选中停车场的车位数据（若有选择）
    await loadParkingSpaces()
    console.log('停车位数据加载完成:', parkingSlots.value.length, '个车位')
  } catch (error) {
    console.warn('加载停车场数据出现问题(已降级处理):', error?.message || error)
  } finally {
    loading.value = false
  }
}

// 加载停车场概览数据
const loadParkingLots = async () => {
  try {
    console.log('开始调用API: /api/Parking/summary?operatorAccount=' + getCurrentUserAccount())
    const response = await fetch(`/api/Parking/summary?operatorAccount=${encodeURIComponent(getCurrentUserAccount())}`)
    console.log('API响应状态:', response.status, response.ok)
    
    if (response.ok) {
      const data = await response.json()
      console.log('API返回的原始数据:', data)
      
      // 修改这里：正确处理后端的ApiResponseDto格式
      if (data.Success && Array.isArray(data.Data)) {
        parkingLots.value = data.Data.map(lot => ({
          areaId: lot.AreaId,
          name: `停车场${lot.AreaId}`,
          totalSpaces: lot.TotalSpaces,
          occupiedSpaces: lot.OccupiedSpaces,
          availableSpaces: lot.AvailableSpaces,
          occupancyRate: lot.OccupancyRate / 100, // 注意：后端返回的是百分比
          parkingFee: lot.ParkingFee,
          status: lot.Status,
          lastUpdateTime: lot.LastUpdateTime,
          canPark: lot.CanPark  // 添加这个字段
        }))
        console.log('从API加载停车场数据成功:', parkingLots.value)
        return
      } else {
        console.error('API返回格式错误:', data)
        throw new Error('API返回数据格式不正确')
      }
    }
    throw new Error(`HTTP错误: ${response.status}`)
  } catch (error) {
    console.error('API调用失败:', error)
    throw new Error('无法从数据库加载停车场数据')
  }
}
// 加载停车位数据
const loadParkingSpaces = async () => {
  try {
    if (!selectedParkingLot.value) {
      console.warn('未选择停车场，跳过加载车位数据')
      return
    }

    const params = new URLSearchParams()
    params.set('areaId', String(selectedParkingLot.value))
    const acc = getCurrentUserAccount()
    if (acc) params.set('operatorAccount', acc)

    const url = `/api/Parking/spaces?${params.toString()}`
    console.log('开始调用停车位API:', url)
    const response = await fetch(url)
    console.log('停车位API响应状态:', response.status, response.ok)

    if (!response.ok) {
      console.warn('停车位API未返回数据，使用空车位集合以保证页面可用')
      parkingSlots.value = []
      updateParkingLotStats()
      return
    }

    const data = await response.json().catch(() => ({}))
    console.log('停车位API返回的原始数据:', data)

    const list = (data.data || data.Data || data) || []
    if (Array.isArray(list)) {
      const totalSpaces = list.length
      const perRow = 10 // 固定10行
      const cols = Math.ceil(totalSpaces / perRow)

      parkingSlots.value = list.map((space, index) => {
        const row = Math.floor(index / cols)
        const col = index % cols
        const x = 50 + col * 35  // 增加列间距，每行车位之间有空隙
        const y = 120 + row * 70

        return {
          id: space.ParkingSpaceId || space.parkingSpaceId || space.id || index + 1,
          no: String(space.ParkingSpaceId || space.parkingSpaceId || space.id || index + 1),
          x,
          y,
          w: 28,
          h: 16,
          skew: -6,
          occupied: (space.Status || space.status) === '占用' || String(space.Status || space.status).toLowerCase() === 'occupied',
          status: ((space.Status || space.status) === '占用' || String(space.Status || space.status).toLowerCase() === 'occupied') ? 'occupied' : 'available',
          licensePlate: space.LicensePlateNumber || space.licensePlateNumber,
          parkStart: space.ParkStart || space.parkStart,
          updateTime: space.UpdateTime || space.updateTime
        }
      })

      console.log('从API加载停车位数据成功:', parkingSlots.value.length, '个车位')
      // 更新表格中的统计数据
      updateParkingLotStats()
      return
    }

    console.warn('停车位API数据格式非数组，使用空集合')
    parkingSlots.value = []
    updateParkingLotStats()
  } catch (error) {
    console.warn('停车位API调用失败(已降级处理):', error?.message || error)
    parkingSlots.value = []
    updateParkingLotStats()
  }
}



// 更新停车场统计数据
const updateParkingLotStats = () => {
  const selectedLot = parkingLots.value.find(l => l.areaId.toString() === selectedParkingLot.value)
  if (selectedLot && parkingSlots.value.length > 0) {
    const occupiedCount = parkingSlots.value.filter(slot => slot.occupied).length
    const totalCount = parkingSlots.value.length
    
    // 只更新实际统计数据，不改变总车位数
    selectedLot.occupiedSpaces = occupiedCount
    selectedLot.availableSpaces = totalCount - occupiedCount
    selectedLot.occupancyRate = totalCount > 0 ? occupiedCount / totalCount : 0
    selectedLot.lastUpdateTime = new Date()
    
    console.log(`更新统计数据: 停车场${selectedLot.areaId}, 总数=${totalCount}, 占用=${occupiedCount}, 占用率=${(selectedLot.occupancyRate * 100).toFixed(1)}%`)
  }
}

// 选择停车场
const selectParkingLot = (areaId) => {
  console.log('选择停车场:', areaId)
  selectedParkingLot.value = areaId
  loadParkingSpaces()
}

// 获取选中停车场名称
const getSelectedParkingLotName = () => {
  const lot = parkingLots.value.find(l => l.areaId.toString() === selectedParkingLot.value)
  return lot ? lot.name : `停车场${selectedParkingLot.value}`
}

// 获取状态样式类
const getStatusClass = (status) => {
  switch (status) {
    case '正常运营': return 'status-normal'
    case '维护中': return 'status-maintenance'
    case '暂停服务': return 'status-suspended'
    default: return 'status-normal'
  }
}

// 获取停车位多边形点
const getSlotPoints = (slot) => {
  const { x, y, w, h, skew } = slot
  return `${x},${y} ${x+w},${y+skew} ${x+w},${y+h+skew} ${x},${y+h}`
}

// 获取停车位填充颜色
const getSlotFill = (slot) => {
  if (slot.status === 'maintenance') return '#e6a23c'
  return slot.occupied ? '#d9534f' : '#5cb85c'
}

// SVG点击事件
const onSvgClick = (event) => {
  // 可以在这里添加其他点击逻辑
}

const refreshData = () => {
  loadParkingData()
}

const goToVehicleManagement = () => {
  if (currentUserAuthority.value !== 1) {
    alert('权限不足：需要管理员(权限=1)才能访问“车辆管理”。')
    return
  }
  router.push('/parking-management/vehicle-management')
}

const goToParkingBilling = () => {
  if (currentUserAuthority.value !== 1) {
    alert('权限不足：需要管理员(权限=1)才能访问“计费管理”。')
    return
  }
  router.push('/parking-management/parking-billing')
}

const goToParkingReport = () => {
  if (currentUserAuthority.value !== 1) {
    alert('权限不足：需要管理员(权限=1)才能访问“停车报表”。')
    return
  }
  router.push('/parking-management/parking-report')
}

const showSpaceDetail = (slot) => {
  selectedSlot.value = slot
  showDetailModal.value = true
}

const closeDetailModal = () => {
  showDetailModal.value = false
  selectedSlot.value = null
}

// 修改停车场信息
const editParkingLot = (lot) => {
  console.log('当前用户权限:', currentUserAuthority.value)
  console.log('权限检查结果(需要=1):', currentUserAuthority.value === 1)
  
  // 检查用户权限：只有权限为1（数据库管理员）才能修改
  if (currentUserAuthority.value !== 1) {
    alert(`没有权限修改停车场信息，需要管理员权限(当前=${currentUserAuthority.value})`)
    return
  }
  
  editingLot.value = lot
  editForm.value = {
    parkingFee: lot.parkingFee,
    status: lot.status,
    remarks: ''
  }
  showEditModal.value = true
}

// 关闭修改弹窗
const closeEditModal = () => {
  showEditModal.value = false
  editingLot.value = null
  editForm.value = {
    parkingFee: 0,
    status: '正常运营',
    remarks: ''
  }
}

// 保存停车场信息修改
const saveParkingLotChanges = async () => {
  if (!editingLot.value) return
  
  try {
    saving.value = true
    
    const response = await fetch(`/api/Parking/UpdateParkingLotInfo/${editingLot.value.areaId}`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        areaId: editingLot.value.areaId,
        parkingFee: editForm.value.parkingFee,
        status: editForm.value.status,
        operatorAccount: getCurrentUserAccount(), // 获取当前登录用户账号
        remarks: editForm.value.remarks
      })
    })
    
    if (response.ok) {
      const result = await response.json()
      console.log('修改成功:', result)
      
      // 更新本地数据
      const lotIndex = parkingLots.value.findIndex(l => l.areaId === editingLot.value.areaId)
      if (lotIndex !== -1) {
        parkingLots.value[lotIndex].parkingFee = editForm.value.parkingFee
        parkingLots.value[lotIndex].status = editForm.value.status
        parkingLots.value[lotIndex].lastUpdateTime = new Date()
      }
      
      alert('停车场信息修改成功！')
      closeEditModal()
    } else {
      const error = await response.json()
      alert(`修改失败: ${error.error || '未知错误'}`)
    }
  } catch (error) {
    console.error('修改停车场信息失败:', error)
    alert('修改失败，请检查网络连接')
  } finally {
    saving.value = false
  }
}

// 获取当前用户账号（优先从Pinia）
const getCurrentUserAccount = () => {
  const accFromStore = userStore?.userInfo?.account || userStore?.token
  return currentUserAccount.value || accFromStore || 'unknown'
}

// 获取当前用户权限和账号
const getUserAuthority = async () => {
  try {
    // 1) 先从 Pinia 取（登录时已写入）
    if (userStore?.userInfo?.authority != null) {
      currentUserAuthority.value = userStore.userInfo.authority
      currentUserAccount.value = userStore.userInfo.account || userStore.token || ''
      console.log('从Pinia获取用户信息:', {
        account: currentUserAccount.value,
        authority: currentUserAuthority.value
      })
      return
    }

    // 2) 退化：根据账号向后端查询权限（/api/Accounts/getauth，POST，query传参）
    const account = userStore?.token
    if (account) {
      const resp = await fetch(`/api/Accounts/getauth?account=${encodeURIComponent(account)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      })
      if (resp.ok) {
        const auth = await resp.json()
        currentUserAuthority.value = typeof auth === 'number' ? auth : parseInt(auth, 10) || 0
        currentUserAccount.value = account
        console.log('从后端获取权限:', {
          account: currentUserAccount.value,
          authority: currentUserAuthority.value
        })
        return
      }
    }

    // 3) 实在拿不到则降级为0
    currentUserAuthority.value = 0
    currentUserAccount.value = ''
  } catch (error) {
    console.error('获取用户信息出错:', error)
    currentUserAuthority.value = 0
    currentUserAccount.value = ''
  }
}

// 统一北京时间显示/计算（与 Query 页一致）：
// 1) 带时区(Z/±HH:mm)：按其时区解析
// 2) 无时区(包含 2025/9/5 17:44:04、2025-09-05 17:44:04)：按“UTC裸时间”解释，再以北京时间显示
const BJ_MIN = 8 * 60
const parseToUtcMs = (val) => {
  if (val == null || val === '') return null
  if (val instanceof Date) return val.getTime()
  if (typeof val === 'number') return Number(val)
  const s = String(val).trim()
  if (/Z$|[+-]\d{2}:\d{2}$/.test(s)) return new Date(s).getTime()
  let m = s.match(/^(\d{4})-(\d{1,2})-(\d{1,2})[ T](\d{1,2}):(\d{1,2})(?::(\d{1,2}))?$/)
  if (m) {
    const [, y, mo, d, h, mi, se] = m
    // 无时区：按UTC裸时间解释
    return Date.UTC(Number(y), Number(mo) - 1, Number(d), Number(h), Number(mi), Number(se || '0'))
  }
  m = s.match(/^(\d{4})\/(\d{1,2})\/(\d{1,2})[ T](\d{1,2}):(\d{1,2})(?::(\d{1,2}))?$/)
  if (m) {
    const [, y, mo, d, h, mi, se] = m
    // 斜杠格式，无时区：按UTC裸时间解释
    return Date.UTC(Number(y), Number(mo) - 1, Number(d), Number(h), Number(mi), Number(se || '0'))
  }
  const t = new Date(s).getTime()
  return isNaN(t) ? null : t
}

const formatDateTime = (dateTime) => {
  const ms = parseToUtcMs(dateTime)
  if (ms == null) return '-'
  return new Date(ms).toLocaleString('zh-CN', { timeZone: 'Asia/Shanghai', hour12: false })
}

const calculateDuration = (parkStart) => {
  const startMs = parseToUtcMs(parkStart)
  if (startMs == null) return '-'
  let diff = Date.now() - startMs
  const MAX_MS = 50 * 365 * 24 * 60 * 60 * 1000
  if (!isFinite(diff) || diff < 0 || diff > MAX_MS) diff = 0
  const hours = Math.floor(diff / (1000 * 60 * 60))
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
  if (hours > 0) return `${hours}小时${minutes}分钟`
  return `${minutes}分钟`
}

// 生命周期
onMounted(() => {
  getUserAuthority() // 获取用户权限
  loadParkingData()
  // 设置定时刷新
  refreshTimer = setInterval(loadParkingData, 30000)
})

onUnmounted(() => {
  if (refreshTimer) {
    clearInterval(refreshTimer)
  }
})
</script>

<style scoped>
.parking-management {
  padding: 20px;
  max-width: 1400px;
  margin: 0 auto;
}

.parking-management h2 {
  color: #333;
  margin-bottom: 20px;
}

/* 上半部分：停车场状态表格 */
.parking-status-section {
  background: white;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 30px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.parking-status-section h3 {
  margin: 0 0 20px 0;
  color: #333;
}

.status-table-container {
  overflow-x: auto;
}

.status-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}

.status-table th,
.status-table td {
  padding: 12px 8px;
  text-align: left;
  border-bottom: 1px solid #eee;
}

.status-table th {
  background: #f8f9fa;
  font-weight: 600;
  color: #333;
  position: sticky;
  top: 0;
}

.status-table tbody tr {
  cursor: pointer;
  transition: background-color 0.2s;
}

.status-table tbody tr:hover {
  background-color: #f5f5f5;
}

.selected-row {
  background-color: #e3f2fd !important;
}

.occupied-count {
  color: #f56c6c;
  font-weight: 500;
}

.available-count {
  color: #67c23a;
  font-weight: 500;
}

.occupancy-bar {
  position: relative;
  width: 80px;
  height: 20px;
  background: #f0f0f0;
  border-radius: 10px;
  overflow: hidden;
}

.occupancy-fill {
  height: 100%;
  background: linear-gradient(90deg, #67c23a 0%, #e6a23c 50%, #f56c6c 100%);
  transition: width 0.3s ease;
}

.occupancy-text {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  font-size: 10px;
  font-weight: bold;
  color: #333;
  text-shadow: 1px 1px 1px rgba(255,255,255,0.8);
}

.fee {
  color: #409eff;
  font-weight: 500;
}

.status-normal {
  color: #67c23a;
  font-weight: 500;
}

.status-maintenance {
  color: #e6a23c;
  font-weight: 500;
}

.status-suspended {
  color: #f56c6c;
  font-weight: 500;
}

/* 下半部分：停车场平面图 */
.parking-layout-section {
  background: white;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.section-header h3 {
  margin: 0;
  color: #333;
}

.layout-controls {
  display: flex;
  gap: 10px;
}

.refresh-btn {
  padding: 8px 16px;
  background: #409eff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
}

.refresh-btn:hover {
  background: #337ecc;
}

.detail-btn {
  padding: 8px 16px;
  background: #67c23a;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
}

.detail-btn:hover {
  background: #5daf34;
}

.action-btn {
  padding: 8px 16px;
  background: #17a2b8;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
}

.action-btn:hover {
  background: #138496;
}

.legend {
  display: flex;
  gap: 20px;
  margin-bottom: 20px;
  justify-content: center;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.legend-color {
  width: 20px;
  height: 20px;
  border-radius: 4px;
  border: 1px solid #ccc;
}

.legend-color.available {
  background-color: #5cb85c;
}

.legend-color.occupied {
  background-color: #d9534f;
}

.legend-color.maintenance {
  background-color: #e6a23c;
}

/* SVG容器 */
.svg-container {
  position: relative;
  width: 100%;
  height: 600px;
  border: 1px solid #ddd;
  border-radius: 4px;
  overflow: hidden;
}

.svg-container svg {
  width: 100%;
  height: 100%;
}

/* 工具提示 */
.tooltip {
  position: absolute;
  background: rgba(0, 0, 0, 0.8);
  color: white;
  padding: 8px 12px;
  border-radius: 4px;
  font-size: 12px;
  pointer-events: none;
  z-index: 1000;
  white-space: nowrap;
}

.tooltip div {
  margin: 2px 0;
}

/* 弹窗样式 */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  border-radius: 8px;
  max-width: 500px;
  width: 90%;
  max-height: 80vh;
  overflow-y: auto;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #eee;
}

.close-btn {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #666;
}

.close-btn:hover {
  color: #333;
}

.modal-body {
  padding: 20px;
}

.detail-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 0;
  border-bottom: 1px solid #f0f0f0;
}

.detail-item:last-child {
  border-bottom: none;
}

.detail-item .label {
  font-weight: bold;
  color: #666;
}

.detail-item .value {
  color: #333;
}

.detail-item .value.occupied {
  color: #f56c6c;
}

.detail-item .value.available {
  color: #67c23a;
}

/* 修改按钮样式 */
.edit-btn {
  padding: 6px 12px;
  background: #409eff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 12px;
}

.edit-btn:hover {
  background: #337ecc;
}

/* 编辑表单样式 */
.edit-form {
  padding: 20px 0;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 8px;
  font-weight: 600;
  color: #333;
}

.form-input,
.form-select,
.form-textarea {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}

.form-textarea {
  height: 80px;
  resize: vertical;
}

.readonly-field {
  display: inline-block;
  padding: 10px;
  background: #f5f5f5;
  border: 1px solid #ddd;
  border-radius: 4px;
  color: #666;
  font-size: 14px;
}

.form-actions {
  display: flex;
  gap: 15px;
  justify-content: flex-end;
  margin-top: 30px;
}

.save-btn {
  padding: 10px 20px;
  background: #67c23a;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
}

.save-btn:hover:not(:disabled) {
  background: #5daf34;
}

.save-btn:disabled {
  background: #c0c4cc;
  cursor: not-allowed;
}

.cancel-btn {
  padding: 10px 20px;
  background: #909399;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
}

.cancel-btn:hover {
  background: #82848a;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .section-header {
    flex-direction: column;
    gap: 15px;
    align-items: stretch;
  }
  
  .layout-controls {
    justify-content: center;
  }
  
  .svg-container {
    height: 400px;
  }
  
  .status-table {
    font-size: 12px;
  }
  
  .status-table th,
  .status-table td {
    padding: 8px 4px;
  }
}
</style>





