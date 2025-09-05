<template>
  <div class="device-orders">

    <!-- 顶部标题和新建工单按钮 -->
    <div class="orders-header">
      <h3>设备工单管理</h3>
      <button v-if="props.deviceStatus === '故障'"
              class="btn-create-order"
              @click="handleCreateButtonClick">
        <i class="fas fa-plus"></i> 新建工单
      </button>
    </div>

    <!-- 筛选区域 -->
    <div class="filters">
      <div class="filter-group">
        <label>筛选状态:</label>
        <select v-model="filters.inProgressOnly" @change="fetchOrders">
          <option :value="false">全部工单</option>
          <option :value="true">仅进行中</option>
        </select>
      </div>
    </div>

    <!-- 加载中提示 -->
    <div v-if="loading" class="loading-orders">
      <div class="spinner-small"></div>
      <span>加载工单列表中...</span>
    </div>

    <!-- 无工单提示 -->
    <div v-else-if="orders.length === 0" class="no-orders">
      <i class="fas fa-clipboard-list"></i>
      <p>暂无工单记录</p>
    </div>

    <!-- 工单列表 -->
    <div v-else class="orders-list">
      <div class="order-item" v-for="order in orders" :key="order.REPAIR_START">
        <div class="order-header">
          <span class="order-id">工单 #{{ formatOrderId(order.REPAIR_START) }}</span>
          <span :class="['order-status', getStatusClass(order)]">
            {{ getStatusText(order) }}
          </span>
        </div>

        <div class="order-details">
          <p><strong>维修人员ID:</strong> {{ order.STAFF_ID }}</p>
          <p><strong>开始时间:</strong> {{ formatDateTime(order.REPAIR_START) }}</p>

          <!-- 已完成才显示结束时间、花费和结果 -->
          <template v-if="isOrderCompleted(order)">
            <p><strong>结束时间:</strong> {{ formatDateTime(order.REPAIR_END) }}</p>
            <p><strong>维修花费:</strong> ¥{{ Math.abs(order.REPAIR_COST) }}</p>
            <p>
              <strong>维修结果:</strong>
              <span v-if="order.REPAIR_COST > 0">成功</span>
              <span v-else>失败</span>
            </p>
          </template>

          <!-- 未完成工单 -->
          <template v-else>
            <p><strong>维修花费:</strong> 未计费</p>
          </template>
        </div>

        <div class="order-actions">
          <!-- 未完成工单: 完成按钮 -->
          <button v-if="!isOrderCompleted(order)"
                  class="btn-complete"
                  @click="openCompleteModal(order)">
            <i class="fas fa-check-circle"></i> 完成工单
          </button>

          <!-- 已完成工单: 确认按钮 -->
          <button v-else
                  class="btn-confirm"
                  @click="confirmOrder(order)">
            <i class="fas fa-check-double"></i> 确认工单
          </button>
        </div>
      </div>
    </div>

    <!-- 创建工单模态框 -->
    <CreateOrderModal v-if="showCreateModal"
                      :deviceId="deviceId"
                      :operatorID="operatorID"
                      @close="showCreateModal = false"
                      @order-created="handleOrderCreated" />

    <!-- 完成工单模态框 -->
    <div v-if="showCompleteModalRef" class="modal-overlay" @click.self="showCompleteModalRef = false">
      <div class="modal-content">
        <div class="modal-header">
          <h3>完成工单</h3>
          <button class="btn-close" @click="showCompleteModalRef = false">
            <i class="fas fa-times"></i>
          </button>
        </div>

        <div class="modal-body">
          <div class="form-group">
            <label>维修结果 <span class="required">*</span></label>
            <select v-model="completeData.success" class="form-control">
              <option :value="true">成功</option>
              <option :value="false">失败</option>
            </select>
          </div>

          <div class="form-group">
            <label>维修花费 <span class="required">*</span></label>
            <input type="number"
                   v-model.number="completeData.cost"
                   class="form-control"
                   placeholder="请输入维修花费"
                   min="0"
                   step="0.01"
                   required />
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn-cancel" @click="showCompleteModalRef = false">取消</button>
          <button class="btn-confirm" @click="completeOrder" :disabled="!completeData.cost">
            提交
          </button>
        </div>
      </div>
    </div>

  </div>
</template>


<script setup>
  import { ref, onMounted, defineProps, defineEmits } from 'vue'
  import axios from 'axios'
  import { useUserStore } from '@/user/user'
  import CreateOrderModal from './CreateOrderModal.vue'

const props = defineProps({
  deviceId: { type: String, required: true },
  operatorID: { type: String, required: true },
  deviceStatus: { type: String, required: true }  
})


  // 通知父组件: order-created, order-confirmed
 const emit = defineEmits(['order-updated', 'order-confirmed'])

const handleOrderCreated = () => {
  fetchOrders()          
  emit('order-updated')  
}
  const userStore = useUserStore()

  const orders = ref([])
  const loading = ref(true)
  const showCreateModal = ref(false)
  const showCompleteModalRef = ref(false)
  const selectedOrder = ref(null)
  const completeData = ref({ success: true, cost: 0 })
  const filters = ref({ inProgressOnly: false })

  // ---- 新建工单按钮 ----
  const handleCreateButtonClick = () => {
    showCreateModal.value = true
  }

  // ---- 打开完成工单模态框 ----
  const openCompleteModal = (order) => {
    selectedOrder.value = order
    completeData.value = { success: true, cost: 0 }
    showCompleteModalRef.value = true
  }

  // ---- 完成工单 ----
  const completeOrder = async () => {
    try {
      const response = await axios.put('/api/Equipment/CompleteRepair', {
        EquipmentId: parseInt(props.deviceId),
        StaffId: selectedOrder.value.STAFF_ID,
        RepairStart: selectedOrder.value.REPAIR_START,
        Cost: completeData.value.cost,
        Success: completeData.value.success
      })
      if (response.data) {
        alert(response.data)
        showCompleteModalRef.value = false
        fetchOrders()
      }
    } catch (error) {
      console.error('维修失败:', error)
      alert(error.response?.data || '完成工单失败，请重试')
    }
  }

  const fetchOrders = async () => {
    loading.value = true
    try {
      const response = await axios.get('/api/Equipment/RepairList', {
        params: {
          OperatorID: props.operatorID,
          EquipmentID: parseInt(props.deviceId),
          inProgressOnly: filters.value.inProgressOnly
        }
      })
      if (Array.isArray(response.data)) {
        orders.value = response.data
      } else if (Array.isArray(response.data.data)) {
        orders.value = response.data.data
      } else {
        orders.value = []
      }

    } catch (err) {
      console.error('[fetchOrders] 获取工单失败:', err)
      alert('获取工单列表失败')
    } finally {
      loading.value = false
    }
  }

  // ---- 工单状态检查 ----
  const isOrderCompleted = (order) =>
    order.REPAIR_END && order.REPAIR_END !== '0001-01-01T00:00:00'

  // ---- 确认工单 ----
  const confirmOrder = async (order) => {
    if (!confirm('确定要确认此工单吗？')) return
    try {
      await axios.post('/api/Equipment/confirm-repair', {
        OperatorID: props.operatorID,
        EquipmentId: parseInt(props.deviceId),
        StaffId: order.STAFF_ID,
        RepairStart: order.REPAIR_START
      })
      fetchOrders() 
     emit('order-updated') 
    } catch (err) {
      console.error(err)
      const msg = err.response?.data || err.message || '确认工单失败'
      alert(`确认工单失败: ${msg}`)
    }
  }

  // ---- 工单格式化显示 ----
  const formatOrderId = (dateString) =>
    new Date(dateString).getTime().toString().slice(-6)

  const formatDateTime = (dateString) => {
    if (!dateString || dateString === '0001-01-01T00:00:00') return '未完成'
    return new Date(dateString).toLocaleString('zh-CN')
  }

  const getStatusText = (order) =>
    !isOrderCompleted(order)
      ? '处理中'
      : order.REPAIR_COST > 0
        ? '已完成'
        : '已失败'

  const getStatusClass = (order) =>
    !isOrderCompleted(order)
      ? 'status-pending'
      : order.REPAIR_COST > 0
        ? 'status-completed'
        : 'status-failed'

  // ---- 生命周期 ----
  onMounted(() => {
    fetchOrders()
  })
</script>





<style scoped>
  /* 筛选区域样式优化 */
  .filters {
    display: flex;
    gap: 16px;
    margin-bottom: 20px;
    padding: 16px;
    background-color: #f8f9fa;
    border-radius: 8px;
  }

  .filter-group {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

    .filter-group label {
      font-weight: 600;
      font-size: 14px;
      color: #2c3e50;
    }

    /* 优化后的 select 样式 */
    .filter-group select {
      padding: 8px 12px;
      border: 1px solid #ddd; /* 默认边框色 */
      border-radius: 4px;
      background-color: white;
      font-size: 14px;
      color: #2c3e50;
      transition: border-color 0.2s, box-shadow 0.2s;
      outline: none; /* 移除默认 focus 边框 */
    }

      /* 聚焦高亮，但不刺眼 */
      .filter-group select:focus {
        border-color: #3498db; /* 蓝色边框，和按钮统一 */
        box-shadow: 0 0 0 2px rgba(52, 152, 219, 0.2); /* 淡蓝光晕 */
      }


  .order-actions {
    display: flex;
    gap: 10px;
    margin-top: 12px;
    padding-top: 12px;
    border-top: 1px solid #eaeaea;
    flex-wrap: wrap;
  }

  .btn-complete {
    padding: 6px 12px;
    background-color: #2ecc71;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 14px;
  }

  .btn-confirm {
    padding: 6px 12px;
    background-color: #3498db;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 14px;
  }

  .btn-details {
    padding: 6px 12px;
    background-color: #7f8c8d;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 14px;
  }

  .status-waiting {
    background-color: rgba(241, 196, 15, 0.1);
    color: #f39c12;
  }

  .status-failed {
    background-color: rgba(231, 76, 60, 0.1);
    color: #c0392b;
  }

  /* 模态框样式与CreateOrderModal相同，可以提取到公共样式文件中 */
  .modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(0, 0, 0, 0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
  }

  .modal-content {
    background: white;
    border-radius: 8px;
    width: 90%;
    max-width: 500px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  }

  .modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 16px 20px;
    border-bottom: 1px solid #eaeaea;
  }

    .modal-header h3 {
      margin: 0;
      color: #2c3e50;
    }

  .btn-close {
    background: none;
    border: none;
    font-size: 18px;
    cursor: pointer;
    color: #7f8c8d;
  }

  .modal-body {
    padding: 20px;
  }

  .form-group {
    margin-bottom: 16px;
  }

    .form-group label {
      display: block;
      margin-bottom: 6px;
      font-weight: 600;
      color: #2c3e50;
    }

  .required {
    color: #e74c3c;
  }

  .form-control {
    width: 100%;
    padding: 8px 12px;
    border: 1px solid #ddd;
    border-radius: 4px;
    font-size: 14px;
  }

  .modal-footer {
    display: flex;
    justify-content: flex-end;
    gap: 12px;
    padding: 16px 20px;
    border-top: 1px solid #eaeaea;
  }

  .btn-cancel {
    padding: 8px 16px;
    background-color: #f8f9fa;
    color: #6c757d;
    border: 1px solid #ddd;
    border-radius: 4px;
    cursor: pointer;
  }

  .btn-confirm {
    padding: 8px 16px;
    background-color: #3498db;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
  }

    .btn-confirm:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

  .device-orders {
    padding: 16px;
  }

  .orders-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
  }

    .orders-header h3 {
      margin: 0;
      color: #2c3e50;
    }

  .btn-create-order {
    padding: 8px 16px;
    background-color: #3498db;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 6px;
  }

    .btn-create-order:hover {
      background-color: #2980b9;
    }

  .loading-orders {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 40px 20px;
    justify-content: center;
    color: #6c757d;
  }

  .spinner-small {
    width: 20px;
    height: 20px;
    border: 2px solid #f3f3f3;
    border-top: 2px solid #3498db;
    border-radius: 50%;
    animation: spin 1s linear infinite;
  }

  .no-orders {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 12px;
    padding: 40px 20px;
    color: #6c757d;
  }

    .no-orders i {
      font-size: 32px;
    }

  .orders-list {
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  .order-item {
    border: 1px solid #eaeaea;
    border-radius: 8px;
    padding: 16px;
    background-color: #f8f9fa;
  }

  .order-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12px;
    padding-bottom: 12px;
    border-bottom: 1px solid #eaeaea;
  }

  .order-id {
    font-weight: 600;
    color: #2c3e50;
  }

  .order-status {
    padding: 4px 8px;
    border-radius: 4px;
    font-size: 12px;
    font-weight: 500;
  }

  .status-pending {
    background-color: rgba(241, 196, 15, 0.1);
    color: #f39c12;
  }

  .status-completed {
    background-color: rgba(46, 204, 113, 0.1);
    color: #27ae60;
  }

  .order-details p {
    margin: 6px 0;
    color: #555;
  }

  .order-actions {
    display: flex;
    gap: 10px;
    margin-top: 12px;
    padding-top: 12px;
    border-top: 1px solid #eaeaea;
  }

  .btn-submit {
    padding: 6px 12px;
    background-color: #3498db;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 14px;
  }

    .btn-submit:hover {
      background-color: #2980b9;
    }

  .btn-complete {
    padding: 6px 12px;
    background-color: #2ecc71;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 14px;
  }

    .btn-complete:hover {
      background-color: #27ae60;
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
    .orders-header {
      flex-direction: column;
      align-items: flex-start;
      gap: 12px;
    }

    .order-header {
      flex-direction: column;
      align-items: flex-start;
      gap: 8px;
    }

    .order-actions {
      flex-direction: column;
    }
  }
</style>
