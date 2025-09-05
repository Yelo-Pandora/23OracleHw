<template>
  <DashboardLayout>
    <div class="device-list-container">
      <div class="page-header">
        <h2>设备列表</h2>

        <!--添加设备-->
        <div class="header-actions">
          <button class="btn-primary" @click="showAddModal = true">
            <i class="fas fa-plus"></i>添加设备
          </button>
        </div>
      </div>

      <!--搜索设置-->
      <div class="table-toolbar">
        <div class="search-box">
          <input v-model="searchKeyword" placeholder="搜索设备ID、类型或状态..." @keyup.enter="handleSearch" />
        </div>

        <div class="filter-group">
          <select v-model="statusFilter" @change="applyFilters">
            <option value="">所有状态</option>
            <option value="运行中">运行中</option>
            <option value="待机">待机</option>
            <option value="离线">离线</option>
            <option value="故障">故障</option>
            <option value="维修中">维修中</option>
          </select>

          <select v-model="typeFilter" @change="applyFilters">
            <option value="">所有类型</option>
            <option value="空调">空调</option>
            <option value="照明">照明</option>
            <option value="电梯">电梯</option>
          </select>

          <button class="btn-primary" @click="resetFilters">重置筛选</button>
        </div>
      </div>

      <!-- 设备表格 -->
      <DeviceTable :devices="filteredDevices"
                   :loading="loading"
                   :operator-id="operatorID"
                   @view-detail="viewDeviceDetail"
                   @operate-device="operateDevice"
                   @delete-device="deleteDevice" />

      <!-- 添加/操作模态框 -->
      <DeviceFormModal v-if="showAddModal" :operator-ID="operatorID" @close="showAddModal = false" @saved="handleDeviceAdded" />
      <DeviceActionModal v-if="showActionModal" :device="selectedDevice" :operator-ID="operatorID" @close="showActionModal = false" @action-completed="handleActionCompleted" />

    </div>
  </DashboardLayout>
</template>

<script setup>
  import { ref, computed, onMounted } from 'vue'
  import { useUserStore } from '@/user/user'
  import axios from 'axios'
  import { useRouter } from 'vue-router'
  import DeviceTable from './EquipmentTable.vue'
  import DeviceFormModal from './EquipmentFormModal.vue'
  import DeviceActionModal from './EquipmentActionModal.vue'

  const userStore = useUserStore()
  const operatorID = userStore.token
  const router = useRouter()

  const viewDeviceDetail = device => {
    router.push({
      name: 'DeviceDetail', //子路由名字
      params: { id: device.equipment_ID } //传递设备ID
    })
  }

  const devices = ref([])
  const loading = ref(true)
  const searchKeyword = ref('')
  const statusFilter = ref('')
  const typeFilter = ref('')
  const showAddModal = ref(false)
  const showActionModal = ref(false)
  const selectedDevice = ref(null)

  // 获取设备列表
  const fetchDevices = async () => {
    loading.value = true
    try {
      axios.get('/api/Equipment/EquipmentList', {
        params: { OperatorID: operatorID }
      })
        .then(response => {
          console.log('响应数据:', response.data);
          devices.value = response.data;
        })
        .catch(error => {
          console.error('请求失败:', error.response?.data || error.message);
        });
    } catch (error) {
      if (error.response) {
        alert(`设备列表加载失败: ${error.response.data.message || error.response.statusText}`);
      } else if (error.request) {
        alert('无法连接服务器，请检查网络或后端服务状态');
      } else {
        alert(`请求出错: ${error.message}`);
      }
    } finally {
      loading.value = false
    }
  }

  // 过滤设备
  const filteredDevices = computed(() => {
    return devices.value.filter(d => {
      const matchKeyword = searchKeyword.value === '' ||
        d.equipment_ID.toString().includes(searchKeyword.value) ||
        d.equipment_TYPE.includes(searchKeyword.value) ||
        d.equipment_STATUS.includes(searchKeyword.value)
      const matchStatus = statusFilter.value === '' || d.equipment_STATUS === statusFilter.value
      const matchType = typeFilter.value === '' || d.equipment_TYPE === typeFilter.value
      return matchKeyword && matchStatus && matchType
    })
  })

  //操作设备
  const operateDevice = device => { selectedDevice.value = device; showActionModal.value = true }

  //删除设备
  const deleteDevice = async (device) => {
    if (!confirm(`确认删除设备 ${device.equipment_ID}?`)) return
    try {
      await axios.delete('/api/Equipment/DeleteEquipment', { params: { equipmentID: device.equipment_ID, OperatorID: operatorID } })
      alert('删除成功')
      fetchDevices()
    } catch (error) {
      if (error.response && error.response.status === 500) {
        alert(`删除设备失败：${error.response.data || '服务器内部错误'}`)
      } else {
        alert('删除设备失败：网络或配置错误')
      }
    }
  }

  //确认添加设备
  const handleDeviceAdded = (newDevice) => { devices.value.push(newDevice); fetchDevices(); showAddModal.value = false }
  //确认完成操作
  const handleActionCompleted = () => { fetchDevices() }
  //重置筛选
  const resetFilters = () => { searchKeyword.value = ''; statusFilter.value = ''; typeFilter.value = '' }


  onMounted(() => {
    console.log('[DeviceList] onMounted, operatorID:', operatorID)
    fetchDevices()
  })
</script>


<style scoped>
  .device-list-container {
    padding: 20px;
  }

  /* 页面标题和操作按钮 */
  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
  }

    .page-header h2 {
      margin: 0;
      color: #2c3e50;
      font-size: 28px; /* 标题字体大一些 */
    }

  /* 按钮样式 */
  .btn-primary,
  .btn-secondary {
    padding: 10px 16px;
    font-size: 16px; /* 字体大一些 */
    height: 42px;
    border: none;
    border-radius: 6px;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }

  .btn-primary {
    background-color: #3498db;
    color: white;
  }

    .btn-primary:hover {
      background-color: #2980b9;
    }

  .btn-secondary {
    background-color: #6c757d;
    color: white;
  }

    .btn-secondary:hover {
      background-color: #5a6268;
    }

  /* 表格工具栏 */
  .table-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    flex-wrap: wrap;
    gap: 20px; /* 间距加大 */
    background: white;
    padding: 15px;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  }

  /* 搜索框 */
  .search-box {
    display: flex;
    align-items: center;
    flex: 1;
  }

    .search-box input {
      width: 250px;
      padding: 10px 14px;
      font-size: 16px;
      height: 42px;
      border: 1px solid #ddd;
      border-radius: 6px;
      box-sizing: border-box;
      transition: all 0.2s;
    }

      .search-box input:focus {
        outline: none;
        border-color: #3498db;
        box-shadow: 0 0 0 2px rgba(52, 152, 219, 0.2);
      }

  /* 筛选组 */
  .filter-group {
    display: flex;
    align-items: center;
    gap: 15px; /* 间距加大 */
  }

    .filter-group select {
      padding: 10px 14px;
      font-size: 16px;
      height: 42px;
      border: 1px solid #ddd;
      border-radius: 6px;
      box-sizing: border-box;
      transition: all 0.2s;
    }

      .filter-group select:focus {
        outline: none;
        border-color: #3498db;
        box-shadow: 0 0 0 2px rgba(52, 152, 219, 0.2);
      }

  /* 响应式设计 */
  @media (max-width: 768px) {
    .table-toolbar {
      flex-direction: column;
      align-items: stretch;
    }

    .search-box {
      width: 100%;
    }

      .search-box input {
        width: 100%;
      }

    .filter-group {
      flex-wrap: wrap;
      justify-content: flex-start;
    }

    .page-header {
      flex-direction: column;
      align-items: flex-start;
      gap: 15px;
    }
  }
</style>

