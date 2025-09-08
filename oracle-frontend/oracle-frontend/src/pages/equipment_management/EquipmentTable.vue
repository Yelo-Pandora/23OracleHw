<template>
  <div class="device-table-container">
    <div class="table-wrapper">
      <table class="device-table">
        <thead>
          <tr>
            <th>设备ID</th>
            <th>设备类型</th>
            <th>状态</th>
            <th>位置</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="5" class="loading-cell">
              <div class="loading-spinner"></div>
              <span>加载设备列表中...</span>
            </td>
          </tr>
          <tr v-else-if="devices.length === 0">
            <td colspan="5" class="no-data">
              <i class="fas fa-inbox"></i>
              <p>暂无设备数据</p>
            </td>
          </tr>
          <tr v-else v-for="device in paginatedDevices" :key="device.equipment_ID">
            <td>
              <a class="device-link" @click="viewDeviceDetail(device)">
                {{ device.equipment_ID }}
              </a>
            </td>
            <td>{{ device.equipment_TYPE }}</td>
            <td>
              <span :class="['status-indicator', getStatusClass(device.equipment_STATUS)]">
                {{ device.equipment_STATUS }}
              </span>
            </td>
            <td>
              <span v-if="device.area_ID" class="location-tag">{{ device.area_ID }}</span>
              <span v-else class="no-location">未绑定</span>
            </td>

            <td>
              <div class="action-buttons">
                <!-- 详情按钮，始终可用 -->
                <button class="btn-text" @click="viewDeviceDetail(device)">
                  <i class="fas fa-info-circle"></i> 详情
                </button>

                <!-- 更多按钮 -->
                <div class="dropdown">
                  <button class="btn-text dropdown-toggle"
                          @click.stop="toggleDropdown(device.equipment_ID)"
                          :disabled="isDeviceObsolete(device)"
                    >
                      更多
                  </button>

                  <!-- 下拉菜单-->
                  <div v-if="activeDropdown === device.equipment_ID && !isDeviceObsolete(device)" class="dropdown-menu">
                    <button class="dropdown-item" @click="operateDevice(device)">
                      <i class="fas fa-cog"></i> 操作设备
                    </button>
                    <button class="dropdown-item" @click="deleteDevice(device)">
                      <i class="fas fa-trash"></i> 删除设备
                    </button>
                  </div>
                </div>
              </div>
            </td>

          </tr>
        </tbody>
      </table>
    </div>

    <!-- 分页控件 -->
    <div v-if="devices.length > 0" class="pagination">
      <div class="pagination-info">
        显示 {{ startIndex + 1 }} 到 {{ endIndex }} 条，共 {{ devices.length }} 条记录
      </div>
      <div class="pagination-controls">
        <button :disabled="currentPage === 1" @click="currentPage--" class="pagination-btn">上一页</button>
        <span class="pagination-page">第 {{ currentPage }} 页 / 共 {{ totalPages }} 页</span>
        <button :disabled="currentPage === totalPages" @click="currentPage++" class="pagination-btn">下一页</button>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted, onBeforeUnmount, defineProps, defineEmits } from 'vue'

  const props = defineProps({
    devices: Array,
    loading: Boolean,
    operatorID: String
  })

  const emit = defineEmits(['view-detail', 'operate-device', 'delete-device'])

  const activeDropdown = ref(null)
  const currentPage = ref(1)
  const pageSize = ref(10)

  // 分页列表
  const paginatedDevices = computed(() => {
    const start = (currentPage.value - 1) * pageSize.value
    const end = start + pageSize.value
    return props.devices.slice(start, end)
  })

  // 总页数
  const totalPages = computed(() => Math.ceil(props.devices.length / pageSize.value))

  // 当前页起始和结束索引
  const startIndex = computed(() => (currentPage.value - 1) * pageSize.value)
  const endIndex = computed(() => Math.min(startIndex.value + pageSize.value, props.devices.length))

  // 获取状态类
  const getStatusClass = status => {
    const map = {
      '运行中': 'status-running',
      '待机': 'status-standby',
      '离线': 'status-offline',
      '故障': 'status-faulted',
      '维修中': 'status-maintenance',
      '废弃':'status-discarded'
    }
    return map[status] || 'status-offline'
  }

  const isDeviceObsolete = (device) => device.equipment_STATUS === '废弃';

  // 方法
  const viewDeviceDetail = device => emit('view-detail', device)
  const operateDevice = device => { emit('operate-device', device); activeDropdown.value = null }
  const deleteDevice = device => { emit('delete-device', device); activeDropdown.value = null }

  const toggleDropdown = deviceId => {
    activeDropdown.value = activeDropdown.value === deviceId ? null : deviceId
  }

  //点击空白关闭下拉
  const handleClickOutside = (event) => {
    const dropdowns = document.querySelectorAll('.dropdown')
    let clickedInside = false
    dropdowns.forEach(drop => {
      if (drop.contains(event.target)) clickedInside = true
    })
    if (!clickedInside) activeDropdown.value = null
  }

  onMounted(() => window.addEventListener('click', handleClickOutside))
  onBeforeUnmount(() => window.removeEventListener('click', handleClickOutside))
</script>

<style scoped>
  .device-table-container {
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 12px rgba(0,0,0,0.1);
    overflow: hidden;
  }

  .table-wrapper {
    overflow-x: auto;
  }

  .device-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 14px; 
  }

  .device-table th {
    background-color: #f8f9fa;
    padding: 12px 16px;
    text-align: left;
    font-weight: 600;
    color: #2c3e50;
    border-bottom: 2px solid #dee2e6;
  }

  .device-table td {
    padding: 12px 16px;
    border-bottom: 1px solid #dee2e6;
  }

  .loading-cell, .no-data {
    text-align: center;
    padding: 40px 20px;
    color: #6c757d;
    font-size: 14px;
  }

  .loading-spinner {
    width: 24px;
    height: 24px;
    border: 3px solid #f3f3f3;
    border-top: 3px solid #3498db;
    border-radius: 50%;
    animation: spin 1s linear infinite;
  }

  @keyframes spin {
    0% {
      transform: rotate(0deg);
    }

    100% {
      transform: rotate(360deg);
    }
  }

  .device-link {
    color: #3498db;
    text-decoration: none;
    cursor: pointer;
    font-weight: 500;
  }

  .device-link:hover {
    text-decoration: underline;
  }

  .status-indicator {
    display: inline-flex;
    align-items: center;
    padding: 4px 10px;
    border-radius: 20px;
    font-size: 13px;
    font-weight: 500;
  }

  .status-running {
    background-color: rgba(46, 204, 113,0.1);
    color: #27ae60;
  }

  .status-standby {
    background-color: rgba(241,196,15,0.1);
    color: #f39c12;
  }

  .status-offline {
    background-color: rgba(149,165,166,0.1);
    color: #7f8c8d;
  }

  .status-faulted {
    background-color: rgba(231,76,60,0.1);
    color: #c0392b;
  }

  .status-maintenance {
    background-color: rgba(52,152,219,0.1);
    color: #2980b9;
  }

  .status-discarded {
    background-color: rgba(149, 165, 166, 0.1); 
    color: #7f8c8d; 
  }

  .location-tag {
    display: inline-block;
    padding: 4px 10px;
    background-color: rgba(52,152,219,0.1);
    color: #3498db;
    border-radius: 4px;
    font-size: 13px;
  }

  .no-location {
    color: #95a5a6;
    font-style: italic;
    font-size: 13px;
  }

  .action-buttons {
    display: flex;
    gap: 8px;
  }

  .btn-text {
    background: none;
    border: none;
    color: #3498db;
    cursor: pointer;
    padding: 4px 8px;
    border-radius: 4px;
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 14px;
  }

  .btn-text:hover {
    background-color: rgba(52,152,219,0.1);
  }

  .btn-text:disabled {
    color: #95a5a6;
    background-color: #f0f0f0; 
    cursor: not-allowed; 
    opacity: 0.7;
  }

  .dropdown {
    position: relative;
    display: inline-block;
  }

  .dropdown-menu {
    position: absolute;
    right: 0;
    top: 100%;
    z-index: 10;
    min-width: 150px;
    background: white;
    border: 1px solid #ddd;
    border-radius: 4px;
    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
    padding: 5px 0;
    margin-top: 5px;
  }

  .dropdown-item {
    width: 100%;
    padding: 8px 16px;
    text-align: left;
    background: none;
    border: none;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 14px;
  }

  .dropdown-item:hover {
    background-color: #f8f9fa;
  }

  .pagination {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: 20px;
    padding: 15px 20px;
    border-top: 1px solid #dee2e6;
    font-size: 14px;
  }

  .pagination-controls {
    display: flex;
    align-items: center;
    gap: 15px;
  }

  .pagination-btn {
    padding: 6px 12px;
    background-color: #f8f9fa;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    cursor: pointer;
  }

  .pagination-btn:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }

  .pagination-btn:not(:disabled):hover {
    background-color: #e9ecef;
  }

  .pagination-page {
    font-weight: 500;
  }

  @media (max-width: 768px) {
    .device-table th, .device-table td {
      padding: 8px 10px;
    }

    .pagination {
      flex-direction: column;
      gap: 15px;
      align-items: flex-start;
    }

    .action-buttons {
      flex-direction: column;
      align-items: flex-start;
    }

    .dropdown-menu {
      right: auto;
      left: 0;
    }
  }
</style>
