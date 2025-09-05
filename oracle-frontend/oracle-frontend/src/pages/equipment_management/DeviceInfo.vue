<template>
  <div class="device-info">
    <div class="info-header">
      <h3>设备基本信息</h3>
    </div>

    <div class="info-content" v-if="hasDeviceData">
      <div class="info-grid">
        <div class="info-item">
          <label>设备ID:</label>
          <span>{{ device.equipment_ID }}</span>
        </div>
        <div class="info-item">
          <label>设备类型:</label>
          <span>{{ device.equipment_TYPE }}</span>
        </div>
        <div class="info-item">
          <label>设备状态:</label>
          <span :class="['status-text', getStatusClass(device.equipment_STATUS)]">
            {{ device.equipment_STATUS }}
          </span>
        </div>
        <div class="info-item">
          <label>设备接口:</label>
          <span>{{ device.PORT || '未设置' }}</span>
        </div>
        <div class="info-item">
          <label>设备花费:</label>
          <span>{{ device.equipment_COST ? `¥${device.equipment_COST}` : '未设置' }}</span>
        </div>
        <div class="info-item">
          <label>购入时间:</label>
          <span>{{ formatDate(device.BUY_TIME) }}</span>
        </div>
        <div class="info-item">
          <label>位置ID:</label>
          <span>{{ device.area_ID || '未绑定' }}</span>
        </div>
      </div>
    </div>
   <!--添加加载状态--> 
  <div v-else class="loading-placeholder">
      <p>加载设备信息中...</p>
    </div>
  </div>
</template>

<script setup>
  import { ref, onMounted,computed,watch } from 'vue'

  const props = defineProps({
    device: {
      type: Object,
      required: true,
      default: () => ({})
    }
  })
const hasDeviceData = computed(() => {
  const hasData = props.device && Object.keys(props.device).length > 0 && props.device.equipment_ID
  return hasData
})
  //获取状态对应的CSS类
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

  //格式化日期
  const formatDate = (date) => {
    if (!date) return '未知'
    return new Date(date).toLocaleDateString('zh-CN')
  }

</script>

<style scoped>
  .device-info {
    padding: 16px;
  }

  .info-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
  }

    .info-header h3 {
      margin: 0;
      color: #2c3e50;
    }

  .btn-edit {
    background: none;
    border: 1px solid #ddd;
    padding: 6px 12px;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 6px;
    color: #555;
  }

    .btn-edit:hover {
      background-color: #f5f5f5;
    }

  .info-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 16px;
  }

  .info-item {
    display: flex;
    flex-direction: column;
    padding: 12px;
    background-color: #f8f9fa;
    border-radius: 4px;
  }

    .info-item label {
      font-weight: 600;
      color: #6c757d;
      margin-bottom: 4px;
      font-size: 14px;
    }

    .info-item span {
      color: #2c3e50;
      font-size: 16px;
    }

  .status-text {
    font-weight: 500;
    padding: 2px 8px;
    border-radius: 4px;
    display: inline-block;
    width: fit-content;
  }

  .edit-form {
    padding: 16px;
    border: 1px solid #eaeaea;
    border-radius: 8px;
    background-color: #f8f9fa;
  }

  .form-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
    margin-bottom: 16px;
  }

  .form-group {
    display: flex;
    flex-direction: column;
  }

    .form-group label {
      font-weight: 600;
      margin-bottom: 6px;
      color: #2c3e50;
    }

  .form-control {
    padding: 8px 12px;
    border: 1px solid #ddd;
    border-radius: 4px;
    font-size: 14px;
  }

    .form-control:focus {
      outline: none;
      border-color: #3498db;
      box-shadow: 0 0 0 2px rgba(52, 152, 219, 0.2);
    }

  .form-actions {
    display: flex;
    justify-content: flex-end;
    gap: 12px;
    margin-top: 20px;
  }

  .btn-cancel {
    padding: 8px 16px;
    background-color: #6c757d;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
  }

    .btn-cancel:hover {
      background-color: #5a6268;
    }

  .btn-save {
    padding: 8px 16px;
    background-color: #3498db;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
  }

    .btn-save:hover {
      background-color: #2980b9;
    }

  .loading-placeholder {
    padding: 40px;
    text-align: center;
    color: #7f8c8d;
  }
  /* 响应式设计 */
  @media (max-width: 768px) {
    .form-row {
      grid-template-columns: 1fr;
    }

    .info-grid {
      grid-template-columns: 1fr;
    }
  }
</style>
