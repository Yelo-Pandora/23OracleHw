<template>
  <DashboardLayout>
  <div class="parking-query">
    <h2>车位状态查询</h2>
    
    <!-- 停车场选择 -->
    <div class="parking-selector">
      <label>选择停车场：</label>
      <select v-model="selectedParkingLot" @change="loadParkingData">
        <option value="1001">停车场1001</option>
        <option value="1002">停车场1002</option>
        <option value="1003">停车场1003</option>
      </select>
    </div>

    <!-- 停车场概述 -->
    <div class="parking-overview">
      <h3>停车场概述 - {{ getSelectedParkingLotName() }}</h3>
      <div class="overview-stats">
        <div class="stat-item">
          <span class="stat-label">总车位数：</span>
          <span class="stat-value">{{ parkingSummary.totalSpaces }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">已占用：</span>
          <span class="stat-value occupied">{{ parkingSummary.occupiedSpaces }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">空闲：</span>
          <span class="stat-value available">{{ parkingSummary.availableSpaces }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">占用率：</span>
          <span class="stat-value">{{ (parkingSummary.occupancyRate * 100).toFixed(1) }}%</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">停车场状态：</span>
          <span class="stat-value" :class="getStatusClass(parkingSummary.status)">{{ parkingSummary.status }}</span>
        </div>
      </div>
    </div>

    <!-- 停车场平面图 -->
    <div class="parking-layout">
      <h3>停车场平面图</h3>
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
            
            <rect x="1100" y="20" width="80" height="30" fill="#F44336" stroke="#C62828" stroke-width="2" rx="5" />
            <text x="1140" y="37" text-anchor="middle" dominant-baseline="middle" fill="white" font-size="12" font-weight="bold">出口</text>
          </g>
          
          <!-- 车道指示 -->
          <g class="road-indicators">
            <line x1="0" y1="50" x2="1200" y2="50" stroke="#666" stroke-width="3" stroke-dasharray="10 5" />
            <text x="600" y="40" text-anchor="middle" dominant-baseline="middle" fill="#666" font-size="10">主车道</text>
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
  </div>
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '@/components/BoardLayout.vue';
import { ref, computed, onMounted } from 'vue'

// 响应式数据
const selectedParkingLot = ref('1001')
const parkingSlots = ref([])
const hoveredSlot = ref(null)
const loading = ref(false)
const parkingSummary = ref({
  totalSpaces: 0,
  occupiedSpaces: 0,
  availableSpaces: 0,
  occupancyRate: 0,
  status: '正常运营'
})

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
    
    // 加载停车场概述数据
    await loadParkingSummary()
    
    // 加载停车位数据
    await loadParkingSpaces()
  } catch (error) {
    console.error('加载停车场数据失败:', error)
  } finally {
    loading.value = false
  }
}

// 加载停车场概述数据
const loadParkingSummary = async () => {
  try {
    const response = await fetch('/api/Parking/summary?operatorAccount=admin')
    if (response.ok) {
      const data = await response.json()
      if ((data.success || data.Success) && (data.data || data.Data)) {
        const lot = (data.data || data.Data).find(l => l.AreaId.toString() === selectedParkingLot.value)
        if (lot) {
          parkingSummary.value = {
            totalSpaces: lot.TotalSpaces,
            occupiedSpaces: lot.OccupiedSpaces,
            availableSpaces: lot.AvailableSpaces,
            occupancyRate: lot.OccupancyRate / 100,
            status: lot.Status
          }
        }
      }
    }
  } catch (error) {
    console.error('加载停车场概述失败:', error)
  }
}

// 加载停车位数据
const loadParkingSpaces = async () => {
  try {
    const response = await fetch(`/api/Parking/spaces?operatorAccount=admin&areaId=${selectedParkingLot.value}`)
    if (response.ok) {
      const data = await response.json()
      if ((data.success || data.Success) && (data.data || data.Data)) {
        const totalSpaces = (data.data || data.Data).length
        const perRow = 10
        const cols = Math.ceil(totalSpaces / perRow)
        
        parkingSlots.value = (data.data || data.Data).map((space, index) => {
          const row = Math.floor(index / cols)
          const col = index % cols
          const x = 50 + col * 35
          const y = 120 + row * 70
          
          return {
            id: space.ParkingSpaceId || space.parkingSpaceId,
            no: (space.ParkingSpaceId || space.parkingSpaceId).toString(),
            x: x,
            y: y,
            w: 28,
            h: 16,
            skew: -6,
            occupied: (space.Status || space.status) === '占用',
            status: (space.Status || space.status) === '占用' ? 'occupied' : 'available',
            licensePlate: space.LicensePlateNumber || space.licensePlateNumber,
            parkStart: space.ParkStart || space.parkStart,
            updateTime: space.UpdateTime || space.updateTime
          }
        })
      }
    }
  } catch (error) {
    console.error('加载停车位数据失败:', error)
  }
}

const getSelectedParkingLotName = () => {
  return `停车场${selectedParkingLot.value}`
}

const getStatusClass = (status) => {
  switch (status) {
    case '正常运营': return 'status-normal'
    case '维护中': return 'status-maintenance'
    case '暂停服务': return 'status-suspended'
    default: return 'status-normal'
  }
}

const getSlotPoints = (slot) => {
  const { x, y, w, h, skew } = slot
  return `${x},${y} ${x+w},${y+skew} ${x+w},${y+h+skew} ${x},${y+h}`
}

const getSlotFill = (slot) => {
  if (slot.status === 'maintenance') return '#e6a23c'
  return slot.occupied ? '#d9534f' : '#5cb85c'
}

const onSvgClick = (event) => {
  // 可以在这里添加其他点击逻辑
}

const showSpaceDetail = (slot) => {
  console.log('点击车位:', slot)
}

const formatDateTime = (dateTime) => {
  if (!dateTime) return '-'
  const date = new Date(dateTime)
  return date.toLocaleString('zh-CN')
}

// 生命周期
onMounted(() => {
  loadParkingData()
})
</script>

<style scoped>
.parking-query {
  padding: 20px;
  max-width: 1400px;
  margin: 0 auto;
}

h2, h3 {
  color: #333;
  margin-bottom: 20px;
}

.parking-selector {
  margin-bottom: 20px;
  display: flex;
  align-items: center;
  gap: 10px;
}

.parking-selector label {
  font-weight: bold;
}

.parking-selector select {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}

/* 停车场概述 */
.parking-overview {
  background: white;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.overview-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 15px;
  margin-top: 15px;
}

.stat-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px;
  background: #f8f9fa;
  border-radius: 6px;
  border-left: 4px solid #007bff;
}

.stat-label {
  font-weight: bold;
  color: #333;
}

.stat-value {
  font-size: 18px;
  font-weight: bold;
}

.stat-value.occupied {
  color: #dc3545;
}

.stat-value.available {
  color: #28a745;
}

.status-normal {
  color: #28a745;
}

.status-maintenance {
  color: #ffc107;
}

.status-suspended {
  color: #dc3545;
}

.parking-layout {
  background: white;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
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
  background-color: #67c23a;
}

.legend-color.occupied {
  background-color: #f56c6c;
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
  border-radius: 8px;
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


</style>
