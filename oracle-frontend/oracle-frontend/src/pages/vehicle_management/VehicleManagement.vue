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
            <option value="1001">停车场1001</option>
            <option value="1002">停车场1002</option>
            <option value="1003">停车场1003</option>
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
              <td>{{ formatDateTime(vehicle.parkStart) }}</td>
              <td>{{ vehicle.parkEnd ? formatDateTime(vehicle.parkEnd) : '---' }}</td>
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



const formatDateTime = (dateTime) => {
  if (!dateTime) return '-'
  const date = new Date(dateTime)
  return date.toLocaleString('zh-CN')
}

const calculateDuration = (parkStart, parkEnd, currentDuration) => {
  if (parkEnd) {
    // 如果已离开，计算总停车时长
    const start = new Date(parkStart)
    const end = new Date(parkEnd)
    const diff = end - start
    
    const days = Math.floor(diff / (1000 * 60 * 60 * 24))
    const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60))
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
    
    if (days > 0) {
      return `${days}天${hours}小时${minutes}分钟`
    } else if (hours > 0) {
      return `${hours}小时${minutes}分钟`
    } else {
      return `${minutes}分钟`
    }
  } else if (currentDuration) {
    // 如果还在停车，使用后端返回的当前时长
    // currentDuration是TimeSpan格式，如"17:48:30.7141460"或"1.17:48:30.7141460"
    let totalMinutes = 0
    if (typeof currentDuration === 'string') {
      // 解析TimeSpan字符串，可能包含天数 "d.hh:mm:ss.ffffff"
      if (currentDuration.includes('.')) {
        // 包含天数的格式 "1.17:48:30.7141460"
        const dayPart = currentDuration.split('.')[0]
        const timePart = currentDuration.split('.')[1]
        const days = parseInt(dayPart) || 0
        const timeParts = timePart.split(':')
        const hours = parseInt(timeParts[0]) || 0
        const minutes = parseInt(timeParts[1]) || 0
        totalMinutes = days * 24 * 60 + hours * 60 + minutes
      } else {
        // 不包含天数的格式 "17:48:30.7141460"
        const parts = currentDuration.split(':')
        if (parts.length >= 3) {
          const hours = parseInt(parts[0]) || 0
          const minutes = parseInt(parts[1]) || 0
          totalMinutes = hours * 60 + minutes
        }
      }
    } else if (typeof currentDuration === 'number') {
      // 如果是毫秒数
      totalMinutes = Math.floor(currentDuration / 60000)
    }
    
    // 正确计算天、小时、分钟
    const days = Math.floor(totalMinutes / (24 * 60))
    const hours = Math.floor((totalMinutes % (24 * 60)) / 60)
    const mins = totalMinutes % 60
    
    // 显示格式：如果有天数就显示天数
    if (days > 0) {
      return `${days}天${hours}小时${mins}分钟`
    } else if (hours > 0) {
      return `${hours}小时${mins}分钟`
    } else {
      return `${mins}分钟`
    }
  } else {
    return '-'
  }
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
