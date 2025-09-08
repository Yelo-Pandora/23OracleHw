<template>
  <div class="device-detail">
    <!-- 返回导航 -->
    <div class="detail-header">
      <button class="btn-back" @click="goBack">
        <i class="fas fa-arrow-left"></i> 返回列表
      </button>

      <h2>设备详情 - {{ device.equipment_TYPE }} {{ device.equipment_ID }}</h2>

      <div class="device-status">
        <span :class="['status-badge', getStatusClass(device.equipment_STATUS)]">
          {{ device.equipment_STATUS }}
        </span>
      </div>
    </div>

    <!-- 标签页导航 -->
    <div class="detail-tabs">
      <div class="tab-list">
        <button :class="['tab-item', { active: activeTab === 'info' }]"
                @click="activeTab = 'info'">
          <i class="fas fa-info-circle"></i> 基本信息
        </button>

        <button :class="['tab-item', { active: activeTab === 'location' }]"
                @click="activeTab = 'location'">
          <i class="fas fa-map-marker-alt"></i> 位置信息
        </button>

        <button :class="['tab-item', { active: activeTab === 'orders' }]"
                @click="activeTab = 'orders'">
          <i class="fas fa-clipboard-list"></i> 工单管理
        </button>
      </div>

      <!-- 标签页内容 -->
      <div class="tab-content">
        <div v-show="activeTab === 'info' && !loading" class="tab-pane">
          <DeviceInfo :device="device" />
        </div>

        <div v-show="activeTab === 'location' && !loading" class="tab-pane">
          <DeviceLocation :device="device" :operatorID="operatorID" @location-updated="handleLocationUpdated" />
        </div>

        <div v-show="activeTab === 'orders' && !loading" class="tab-pane">
          <DeviceOrders :deviceId="deviceId"
                        :operatorID="operatorID"
                        :deviceStatus="device.equipment_STATUS"
                        @order-updated="fetchDeviceDetail" />
        </div>
      </div>
    </div>

    <!-- 加载状态 -->
    <div v-if="loading" class="loading-overlay">
      <div class="loading-content">
        <div class="spinner"></div>
        <p>加载设备详情中...</p>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, onMounted, computed, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import axios from 'axios'
  import { useUserStore } from '@/user/user';

  import DeviceInfo from './DeviceInfo.vue'
  import DeviceLocation from './DeviceLocation.vue'
  import DeviceOrders from './DeviceOrders.vue'

  const route = useRoute()
  const router = useRouter()

  // 用户信息
  const userStore = useUserStore()
  const currentUserRole = userStore.role
  const operatorID = userStore.token

  // 当前设备ID
  const deviceId = computed(() => route.params.id)

  // 响应式数据
  const device = ref({
    equipment_ID: '',
    equipment_TYPE: '',
    equipment_STATUS: '',
    PORT: '',
    equipment_COST: null,
    BUY_TIME: null,
    area_ID: null
  })

  const areas = ref([])
  const loading = ref(true)
  const activeTab = ref('info')

  // 获取设备详情
  const fetchDeviceDetail = async () => {
    if (!deviceId.value) return
    loading.value = true
    try {
      console.log('开始获取设备详情，当前 device:', device.value)
      const response = await axios.get('/api/Equipment/EquipmentDetail', {
        params: {
          equipmentID: deviceId.value,
          OperatorID: operatorID
        }
      })
      const data = response.data
      console.log('接口返回数据 data:', data)

      device.value = {
        equipment_ID: deviceId.value,
        equipment_TYPE: data.EQUIPMENT_TYPE || '',
        equipment_STATUS: data.EQUIPMENT_STATUS || '离线',
        PORT: data.PORT || '',
        equipment_COST: data.EQUIPMENT_COST || null,
        BUY_TIME: data.BUY_TIME ? new Date(data.BUY_TIME) : null,
        area_ID: data.AREA_ID || null
      }
      console.log('更新后 device:', device.value)
    } catch (error) {
      console.error('获取设备详情失败:', error.message)
      alert('获取设备详情失败，请重试')
    } finally {
      loading.value = false
    }
  }

  // 获取状态对应CSS类
  const getStatusClass = (status) => {
    const statusClassMap = {
      '运行中': 'status-running',
      '待机': 'status-standby',
      '离线': 'status-offline',
      '故障': 'status-faulted',
      '维修中': 'status-maintenance'
    }
    return statusClassMap[status] || 'status-offline'
  }

  // 返回列表
  const goBack = () => {
    router.push('/equipment_management')
  }
  const handleLocationUpdated = (newAreaID) => {
  device.value = {
    ...device.value,
    area_ID: newAreaID
  }
}

  // 监听设备ID变化
  watch(deviceId, (newVal) => {
    if (newVal) {
      fetchDeviceDetail()
    }
  })

  // 页面初始化
  onMounted(() => {
    if (deviceId.value) {
      fetchDeviceDetail()
    }
  })
</script>

<style scoped>
  .device-detail {
    padding: 20px;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
    position: relative;
    min-height: 500px;
  }

  .detail-header {
    display: flex;
    align-items: center;
    gap: 20px;
    margin-bottom: 24px;
    padding-bottom: 16px;
    border-bottom: 1px solid #eaeaea;
  }

  .btn-back {
    background: none;
    border: 1px solid #ddd;
    padding: 8px 16px;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 6px;
    color: #555;
  }

    .btn-back:hover {
      background-color: #f5f5f5;
    }

  .detail-header h2 {
    margin: 0;
    color: #2c3e50;
    font-size: 24px;
  }

  .device-status {
    margin-left: auto;
  }

  .status-badge {
    padding: 6px 12px;
    border-radius: 20px;
    font-size: 14px;
    font-weight: 500;
  }

  .status-running {
    background-color: rgba(46, 204, 113, 0.1);
    color: #27ae60;
  }

  .status-standby {
    background-color: rgba(241, 196, 15, 0.1);
    color: #f39c12;
  }

  .status-offline {
    background-color: rgba(149, 165, 166, 0.1);
    color: #7f8c8d;
  }

  .status-faulted {
    background-color: rgba(231, 76, 60, 0.1);
    color: #c0392b;
  }

  .status-maintenance {
    background-color: rgba(52, 152, 219, 0.1);
    color: #2980b9;
  }

  .detail-tabs {
    margin-top: 16px;
  }

  .tab-list {
    display: flex;
    border-bottom: 1px solid #ddd;
    margin-bottom: 20px;
  }

  .tab-item {
    padding: 12px 24px;
    background: none;
    border: none;
    cursor: pointer;
    font-size: 16px;
    color: #7f8c8d;
    border-bottom: 2px solid transparent;
    transition: all 0.3s;
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .tab-item:hover {
    color: #3498db;
  }

  .tab-item.active {
    color: #3498db;
    border-bottom-color: #3498db;
  }

  .tab-content {
    padding: 0 10px;
  }


  @keyframes fadeIn {
    from {
      opacity: 0;
    }

    to {
      opacity: 1;
    }
  }

  .loading-overlay {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(255, 255, 255, 0.8);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 10;
    border-radius: 8px;
  }

  .loading-content {
    text-align: center;
    color: #7f8c8d;
  }

  .spinner {
    width: 40px;
    height: 40px;
    border: 4px solid #f3f3f3;
    border-top: 4px solid #3498db;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin: 0 auto 16px;
  }

  @keyframes spin {
    0% {
      transform: rotate(0deg);
    }

    100% {
      transform: rotate(360deg);
    }
  }

  /* 响应式设计 */
  @media (max-width: 768px) {
    .detail-header {
      flex-direction: column;
      align-items: flex-start;
      gap: 12px;
    }

    .device-status {
      margin-left: 0;
    }

    .tab-list {
      flex-wrap: wrap;
    }

    .tab-item {
      padding: 10px 16px;
      font-size: 14px;
    }
  }
</style>
