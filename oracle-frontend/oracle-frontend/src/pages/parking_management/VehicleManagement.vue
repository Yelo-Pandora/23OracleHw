<template>
  <div class="vehicle-management">
    <div class="page-header">
      <button @click="goBack" class="back-btn">←</button>
      <h2>车辆管理</h2>
    </div>
    
    <!-- 查询窗口 -->
    <div class="search-section">
      <h3>车辆查询</h3>
      <div class="search-form">
        <div class="search-group">
          <label>车牌号查询：</label>
          <input 
            v-model="searchLicensePlate" 
            type="text" 
            placeholder="请输入车牌号"
            @keyup.enter="searchByLicensePlate"
          />
          <button @click="searchByLicensePlate" class="search-btn">查询</button>
        </div>
        
        <div class="search-group">
          <label>按停车场查询：</label>
          <select v-model="selectedParkingLot" @change="searchByParkingLot">
            <option value="">选择停车场</option>
            <option v-for="lot in parkingLotOptions" :key="lot.AreaId" :value="String(lot.AreaId)">
              {{ lot.ParkingLotName || (`停车场${lot.AreaId}`) }}
            </option>
          </select>
          <button @click="searchByParkingLot" class="search-btn">查询</button>
        </div>
      </div>
    </div>

    <!-- 查询结果 -->
    <div class="results-section" v-if="searchResults.length > 0">
      <h3>查询结果</h3>
      <div class="results-table-container">
        <table class="results-table">
          <thead>
            <tr>
              <th>车牌号</th>
              <th>停车场</th>
              <th>车位编号</th>
              <th>入场时间</th>
              <th>出场时间</th>
              <th>停车时长</th>
              <th>状态</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="vehicle in searchResults" :key="vehicle.id">
              <td class="license-plate">{{ vehicle.licensePlate }}</td>
              <td>{{ vehicle.parkingLot }}</td>
              <td>{{ vehicle.parkingSpaceId }}</td>
              <td>{{ vehicle.displayParkStart || (vehicle.parkStart ? formatDateTime(vehicle.parkStart) : '---') }}</td>
              <td>{{ vehicle.displayParkEnd || (vehicle.parkEnd ? formatDateTime(vehicle.parkEnd) : '---') }}</td>
              <td>{{ calculateDuration(vehicle.parkStart, vehicle.parkEnd, vehicle.currentDuration) }}</td>
              <td>
                <span :class="getStatusClass(vehicle.status)">
                  {{ vehicle.status }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 无结果提示 -->
    <div class="no-results" v-if="hasSearched && searchResults.length === 0">
      <p>未找到相关车辆信息</p>
    </div>


  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'

// 路由
const router = useRouter()

// 响应式数据
const searchLicensePlate = ref('')
const selectedParkingLot = ref('')
const searchResults = ref([])
const hasSearched = ref(false)
const loading = ref(false)

// 停车场下拉选项（从后端动态加载）
const parkingLotOptions = ref([])

const loadParkingLotOptions = async () => {
  try {
    const resp = await fetch('/api/Parking/ParkingLots')
    if (resp.ok) {
      const data = await resp.json()
      const list = (data.data || data.Data || data) || []
      parkingLotOptions.value = list
      // 如果未选择，默认选第一个
      if (!selectedParkingLot.value && list.length > 0) {
        selectedParkingLot.value = String(list[0].AreaId)
      }
    } else {
      console.error('加载停车场列表失败，状态码:', resp.status)
    }
  } catch (e) {
    console.error('加载停车场列表出错:', e)
  }
}


// 方法
const searchByLicensePlate = async () => {
  if (!searchLicensePlate.value.trim()) {
    alert('请输入车牌号')
    return
  }
  
  try {
    loading.value = true
    hasSearched.value = true
    
    // 调用后端API查询车辆信息
    const response = await fetch(`/api/Parking/VehicleStatus/${encodeURIComponent(searchLicensePlate.value)}`)
    
    if (response.ok) {
      const data = await response.json()
      console.log('API返回数据:', data)
      if ((data.success || data.Success) && (data.data || data.Data) && !Array.isArray(data.data || data.Data)) {
        // 转换数据格式
        const vehicleData = data.data || data.Data
        searchResults.value = [{
          id: vehicleData.ParkingSpaceId,
          licensePlate: vehicleData.LicensePlateNumber,
          parkingLot: vehicleData.ParkingLotName || `停车场${vehicleData.AreaId}`,
          parkingSpaceId: vehicleData.ParkingSpaceId,
          parkStart: vehicleData.ParkStart,
          parkEnd: null, // 当前在停车辆没有出场时间
          displayParkStart: formatDateTime(vehicleData.ParkStart),
          displayParkEnd: '---',
          status: vehicleData.IsCurrentlyParked ? '在停' : '已离开',
          currentDuration: vehicleData.CurrentDuration
        }]
        console.log('转换后的数据:', searchResults.value)
      } else {
        console.log('API返回失败或无数据:', data)
        searchResults.value = []
      }
    } else {
      console.error('查询车辆信息失败，状态码:', response.status)
      searchResults.value = []
    }
  } catch (error) {
    console.error('查询车辆信息出错:', error)
    searchResults.value = []
  } finally {
    loading.value = false
  }
}

const searchByParkingLot = async () => {
  if (!selectedParkingLot.value) {
    alert('请选择停车场')
    return
  }
  
  try {
    loading.value = true
    hasSearched.value = true
    
    // 调用后端API查询停车场内所有车辆
    const response = await fetch(`/api/Parking/CurrentVehicles?areaId=${selectedParkingLot.value}`)
    
    if (response.ok) {
      const data = await response.json()
      console.log('停车场车辆API返回数据:', data)
      if ((data.success || data.Success) && (data.data || data.Data) && Array.isArray(data.data || data.Data)) {
        // 转换数据格式
        const vehicles = (data.data || data.Data).map(vehicle => ({
          id: vehicle.ParkingSpaceId,
          licensePlate: vehicle.LicensePlateNumber,
          parkingLot: vehicle.ParkingLotName || `停车场${vehicle.AreaId}`,
          parkingSpaceId: vehicle.ParkingSpaceId,
          parkStart: vehicle.ParkStart,
          parkEnd: null, // 当前在停车辆没有出场时间
          displayParkStart: formatDateTime(vehicle.ParkStart),
          displayParkEnd: '---',
          status: vehicle.IsCurrentlyParked ? '在停' : '已离开',
          currentDuration: vehicle.CurrentDuration
        }))
        
        console.log('转换后的车辆数据:', vehicles)
        searchResults.value = vehicles

      } else {
        console.log('停车场车辆API返回失败或无数据:', data)
        searchResults.value = []

      }
    } else {
      console.error('查询停车场车辆失败，状态码:', response.status)
      searchResults.value = []
      parkingLotStats.value = null
    }
  } catch (error) {
    console.error('查询停车场车辆出错:', error)
    searchResults.value = []
    parkingLotStats.value = null
  } finally {
    loading.value = false
  }
}

const BEIJING_OFFSET_MIN = 8 * 60
const parseToUtcMillis = (dateTime) => {
  if (dateTime == null || dateTime === '') return null
  if (typeof dateTime === 'number') return Number(dateTime)
  const s = String(dateTime).trim()
  // 带时区：直接解析为UTC毫秒
  if (/Z$|[+-]\d{2}:\d{2}$/.test(s)) return new Date(s).getTime()
  // 无时区：按UTC裸时间解释（与 Query 页一致）
  let m = s.match(/^(\d{4})-(\d{1,2})-(\d{1,2})[ T](\d{1,2}):(\d{1,2})(?::(\d{1,2}))?$/)
  if (m) {
    const [, y, mo, d, h, mi, se] = m
    return Date.UTC(Number(y), Number(mo) - 1, Number(d), Number(h), Number(mi), Number(se || '0'))
  }
  m = s.match(/^(\d{4})\/(\d{1,2})\/(\d{1,2})[ T](\d{1,2}):(\d{1,2})(?::(\d{1,2}))?$/)
  if (m) {
    const [, y, mo, d, h, mi, se] = m
    return Date.UTC(Number(y), Number(mo) - 1, Number(d), Number(h), Number(mi), Number(se || '0'))
  }
  const t = new Date(s).getTime()
  return isNaN(t) ? null : t
}
const formatDateTime = (dateTime) => {
  const utcMs = parseToUtcMillis(dateTime)
  if (utcMs == null) return '-'
  return new Date(utcMs).toLocaleString('zh-CN', { timeZone: 'Asia/Shanghai', hour12: false })
}

const calculateDuration = (parkStart, parkEnd, currentDuration) => {
  // 优先根据入/出场时间计算（使用统一的 UTC 解析，避免时区偏差）
  const startMs = parseToUtcMillis(parkStart)
  const endMs = parkEnd ? parseToUtcMillis(parkEnd) : Date.now()
  if (startMs != null) {
    let diff = (endMs != null ? endMs : Date.now()) - startMs
    // 合理性校验（避免负数或超过50年这种明显异常）
    const MAX_MS = 50 * 365 * 24 * 60 * 60 * 1000
    if (!isFinite(diff) || diff < 0 || diff > MAX_MS) diff = 0

    const days = Math.floor(diff / (1000 * 60 * 60 * 24))
    const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60))
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))

    if (days > 0) return `${days}天${hours}小时${minutes}分钟`
    if (hours > 0) return `${hours}小时${minutes}分钟`
    return `${minutes}分钟`
  }

  // 兜底：若没有 parkStart，才使用后端的 currentDuration（.NET TimeSpan 格式）
  if (currentDuration) {
    let totalMinutes = 0
    if (typeof currentDuration === 'string') {
      // 解析 d.hh:mm:ss[.fffffff] 或 hh:mm:ss[.fffffff]
      const withDay = currentDuration.match(/^(\d+)\.(\d{1,2}):(\d{2}):(\d{2})(?:\.\d+)?$/)
      const noDay = currentDuration.match(/^(\d{1,2}):(\d{2}):(\d{2})(?:\.\d+)?$/)
      if (withDay) {
        const days = parseInt(withDay[1]) || 0
        const hours = parseInt(withDay[2]) || 0
        const minutes = parseInt(withDay[3]) || 0
        totalMinutes = days * 24 * 60 + hours * 60 + minutes
      } else if (noDay) {
        const hours = parseInt(noDay[1]) || 0
        const minutes = parseInt(noDay[2]) || 0
        totalMinutes = hours * 60 + minutes
      }
    } else if (typeof currentDuration === 'number') {
      // 如果是毫秒数
      totalMinutes = Math.floor(currentDuration / 60000)
    }

    const days = Math.floor(totalMinutes / (24 * 60))
    const hours = Math.floor((totalMinutes % (24 * 60)) / 60)
    const mins = totalMinutes % 60

    if (days > 0) return `${days}天${hours}小时${mins}分钟`
    if (hours > 0) return `${hours}小时${mins}分钟`
    return `${mins}分钟`
  }

  return '-'
}

const getStatusClass = (status) => {
  switch (status) {
    case '在停': return 'status-parking'
    case '已离开': return 'status-departed'
    default: return 'status-unknown'
  }
}

// 返回上一页
const goBack = () => {
  router.push('/parking-management')
}

// 生命周期
onMounted(() => {
  // 页面加载时的初始化
  loadParkingLotOptions()
})
</script>

<style scoped>
.vehicle-management {
  padding: 20px;
  max-width: 1400px;
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

h2, h3 {
  color: #333;
  margin-bottom: 20px;
}

/* 查询窗口 */
.search-section {
  background: white;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.search-form {
  display: flex;
  gap: 30px;
  align-items: end;
}

.search-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 300px;
}

.search-group label {
  font-weight: bold;
  color: #333;
}

.search-group input,
.search-group select {
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}

.search-group input:focus,
.search-group select:focus {
  outline: none;
  border-color: #007bff;
  box-shadow: 0 0 0 2px rgba(0, 123, 255, 0.25);
}

.search-btn {
  padding: 10px 20px;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.2s;
}

.search-btn:hover {
  background: #0056b3;
}

/* 查询结果 */
.results-section {
  background: white;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.results-table-container {
  overflow-x: auto;
}

.results-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}

.results-table th,
.results-table td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #ddd;
}

.results-table th {
  background: #f8f9fa;
  font-weight: bold;
  color: #333;
}

.results-table tbody tr:hover {
  background: #f8f9fa;
}

.license-plate {
  font-weight: bold;
  color: #007bff;
}

/* 状态样式 */
.status-parking {
  color: #28a745;
  font-weight: bold;
}

.status-departed {
  color: #6c757d;
  font-weight: bold;
}

.status-unknown {
  color: #ffc107;
  font-weight: bold;
}

/* 无结果提示 */
.no-results {
  background: #f8f9fa;
  border: 1px solid #dee2e6;
  border-radius: 8px;
  padding: 40px;
  text-align: center;
  color: #6c757d;
  font-size: 16px;
}



/* 响应式设计 */
@media (max-width: 768px) {
  .search-form {
    flex-direction: column;
    gap: 20px;
  }
  
  .search-group {
    min-width: auto;
  }
  
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  
  .results-table {
    font-size: 12px;
  }
  
  .results-table th,
  .results-table td {
    padding: 8px;
  }
}
</style>
