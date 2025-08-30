<template>
  <div class="parking-query">
    <h2>车位查询</h2>
    
    <!-- 停车场选择 -->
    <div class="parking-selector">
      <label>选择停车场：</label>
      <select v-model="selectedParkingLot" @change="loadParkingData">
        <option value="3001">停车场3001</option>
        <option value="3002">停车场3002</option>
        <option value="3003">停车场3003</option>
      </select>
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
      
      <!-- 真实的停车场布局 -->
      <div class="parking-lot">
        <!-- 入口 -->
        <div class="entrance">
          <span class="entrance-text">入口</span>
        </div>
        
        <!-- 主车道 -->
        <div class="main-road">
          <div class="road-line"></div>
        </div>
        
        <!-- 左侧车位区域 -->
        <div class="parking-area left-area">
          <!-- 第一行和第二行 -->
          <div class="parking-row" v-for="row in 2" :key="`left-${row}`">
            <div 
              v-for="col in 6" 
              :key="`left-${row}-${col}`"
              class="parking-space"
              :class="getSpaceClass(getSpaceByPosition('left', row, col))"
              :title="getSpaceTooltip(getSpaceByPosition('left', row, col))"
            >
              <span class="space-number">{{ getSpaceByPosition('left', row, col).id }}</span>
              <span v-if="getSpaceByPosition('left', row, col).occupied" class="car-icon">🚗</span>
              <span v-if="getSpaceByPosition('left', row, col).status === 'maintenance'" class="maintenance-icon">🔧</span>
            </div>
          </div>
          
          <!-- 过道 -->
          <div class="aisle"></div>
          
          <!-- 第三行和第四行 -->
          <div class="parking-row" v-for="row in [3, 4]" :key="`left-${row}`">
            <div 
              v-for="col in 6" 
              :key="`left-${row}-${col}`"
              class="parking-space"
              :class="getSpaceClass(getSpaceByPosition('left', row, col))"
              :title="getSpaceTooltip(getSpaceByPosition('left', row, col))"
            >
              <span class="space-number">{{ getSpaceByPosition('left', row, col).id }}</span>
              <span v-if="getSpaceByPosition('left', row, col).occupied" class="car-icon">🚗</span>
              <span v-if="getSpaceByPosition('left', row, col).status === 'maintenance'" class="maintenance-icon">🔧</span>
            </div>
          </div>
        </div>
        
        <!-- 右侧车位区域 -->
        <div class="parking-area right-area">
          <!-- 第一行和第二行 -->
          <div class="parking-row" v-for="row in 2" :key="`right-${row}`">
            <div 
              v-for="col in 6" 
              :key="`right-${row}-${col}`"
              class="parking-space"
              :class="getSpaceClass(getSpaceByPosition('right', row, col))"
              :title="getSpaceTooltip(getSpaceByPosition('right', row, col))"
            >
              <span class="space-number">{{ getSpaceByPosition('right', row, col).id }}</span>
              <span v-if="getSpaceByPosition('right', row, col).occupied" class="car-icon">🚗</span>
              <span v-if="getSpaceByPosition('right', row, col).status === 'maintenance'" class="maintenance-icon">🔧</span>
            </div>
          </div>
          
          <!-- 过道 -->
          <div class="aisle"></div>
          
          <!-- 第三行和第四行 -->
          <div class="parking-row" v-for="row in [3, 4]" :key="`right-${row}`">
            <div 
              v-for="col in 6" 
              :key="`right-${row}-${col}`"
              class="parking-space"
              :class="getSpaceClass(getSpaceByPosition('right', row, col))"
              :title="getSpaceTooltip(getSpaceByPosition('right', row, col))"
            >
              <span class="space-number">{{ getSpaceByPosition('right', row, col).id }}</span>
              <span v-if="getSpaceByPosition('right', row, col).occupied" class="car-icon">🚗</span>
              <span v-if="getSpaceByPosition('right', row, col).status === 'maintenance'" class="maintenance-icon">🔧</span>
            </div>
          </div>
        </div>
        
        <!-- 出口 -->
        <div class="exit">
          <span class="exit-text">出口</span>
        </div>
        
        <!-- 方向指示 -->
        <div class="direction-arrows">
          <div class="arrow left-arrow">←</div>
          <div class="arrow right-arrow">→</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'

// 响应式数据
const selectedParkingLot = ref('3001')
const allParkingSpaces = ref([])

// 计算属性
const filteredParkingSpaces = computed(() => {
  if (!selectedParkingLot.value) {
    return allParkingSpaces.value
  }
  return allParkingSpaces.value.filter(space => space.parkingLot === selectedParkingLot.value)
})

// 方法
const loadParkingData = () => {
  // 生成真实的停车场布局数据
  const spaces = []
  const parkingLots = ['3001', '3002', '3003']
  
  parkingLots.forEach(lot => {
    // 左侧区域：4行6列 = 24个车位
    for (let row = 1; row <= 4; row++) {
      for (let col = 1; col <= 6; col++) {
        const spaceId = parseInt(lot) * 1000 + (row - 1) * 6 + col
        const isOccupied = Math.random() > 0.6
        const isMaintenance = Math.random() > 0.9
        
        spaces.push({
          id: spaceId,
          parkingLot: lot,
          area: 'left',
          row: row,
          col: col,
          occupied: isOccupied && !isMaintenance,
          status: isMaintenance ? 'maintenance' : (isOccupied ? 'occupied' : 'available')
        })
      }
    }
    
    // 右侧区域：4行6列 = 24个车位
    for (let row = 1; row <= 4; row++) {
      for (let col = 1; col <= 6; col++) {
        const spaceId = parseInt(lot) * 1000 + 24 + (row - 1) * 6 + col
        const isOccupied = Math.random() > 0.6
        const isMaintenance = Math.random() > 0.9
        
        spaces.push({
          id: spaceId,
          parkingLot: lot,
          area: 'right',
          row: row,
          col: col,
          occupied: isOccupied && !isMaintenance,
          status: isMaintenance ? 'maintenance' : (isOccupied ? 'occupied' : 'available')
        })
      }
    }
  })
  
  allParkingSpaces.value = spaces
}

const getSpaceByPosition = (area, row, col) => {
  return allParkingSpaces.value.find(space => 
    space.parkingLot === selectedParkingLot.value && space.area === area && space.row === row && space.col === col
  ) || { id: 0, occupied: false, status: 'available' }
}

const getSpaceClass = (space) => {
  if (space.status === 'maintenance') return 'maintenance'
  return space.occupied ? 'occupied' : 'available'
}

const getSpaceTooltip = (space) => {
  return `车位ID: ${space.id}\n停车场: ${space.parkingLot}\n状态: ${space.status === 'maintenance' ? '维护中' : (space.occupied ? '占用' : '空闲')}`
}

// 生命周期
onMounted(() => {
  loadParkingData()
})
</script>

<style scoped>
.parking-query {
  padding: 20px;
  max-width: 1000px;
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

.parking-layout {
  background: white;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 20px;
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

/* 真实停车场布局 */
.parking-lot {
  position: relative;
  width: 100%;
  max-width: 800px;
  margin: 0 auto;
  background: #f5f5f5;
  border: 2px solid #333;
  border-radius: 8px;
  padding: 20px;
}

.entrance, .exit {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  background: #4CAF50;
  color: white;
  padding: 8px 16px;
  border-radius: 4px;
  font-weight: bold;
  font-size: 14px;
}

.entrance {
  left: -60px;
}

.exit {
  right: -60px;
}

.main-road {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 80%;
  height: 60px;
  background: #666;
  border-radius: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.road-line {
  width: 100%;
  height: 4px;
  background: linear-gradient(to right, transparent 0%, #fff 20%, #fff 80%, transparent 100%);
  border-radius: 2px;
}

.parking-area {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.left-area {
  position: absolute;
  left: 20px;
  top: 20px;
  width: 300px;
}

.right-area {
  position: absolute;
  right: 20px;
  top: 20px;
  width: 300px;
}

.parking-row {
  display: flex;
  gap: 8px;
}

.parking-space {
  width: 40px;
  height: 60px;
  border-radius: 4px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  border: 2px solid #333;
  position: relative;
}

.parking-space.available {
  background-color: #67c23a;
  color: white;
}

.parking-space.occupied {
  background-color: #f56c6c;
  color: white;
}

.parking-space.maintenance {
  background-color: #e6a23c;
  color: white;
}

.aisle {
  width: 100%;
  height: 8px;
  background-color: #ccc;
  margin: 16px 0;
}

.space-number {
  font-size: 10px;
  font-weight: bold;
  margin-bottom: 2px;
}

.car-icon,
.maintenance-icon {
  font-size: 12px;
  margin-top: 2px;
}

.direction-arrows {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  display: flex;
  gap: 200px;
  pointer-events: none;
}

.arrow {
  font-size: 24px;
  color: #fff;
  font-weight: bold;
  text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.5);
}

/* 响应式设计 */
@media (max-width: 768px) {
  .parking-lot {
    padding: 10px;
  }
  
  .left-area, .right-area {
    width: 200px;
  }
  
  .parking-space {
    width: 30px;
    height: 45px;
  }
  
  .aisle {
    height: 3px; /* Adjust aisle height for smaller screens */
  }

  .space-number {
    font-size: 8px;
  }
  
  .car-icon,
  .maintenance-icon {
    font-size: 10px;
  }
  
  .entrance, .exit {
    font-size: 12px;
    padding: 6px 12px;
  }
  
  .entrance {
    left: -50px;
  }
  
  .exit {
    right: -50px;
  }
}
</style>
