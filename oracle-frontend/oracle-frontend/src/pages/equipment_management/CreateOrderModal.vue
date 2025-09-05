<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <!-- 模态框头部 -->
      <div class="modal-header">
        <h3>新建工单</h3>
        <button class="btn-close" @click="$emit('close')">
          <i class="fas fa-times"></i>
        </button>
      </div>

      <!-- 模态框主体 -->
      <div class="modal-body">
        <div class="form-group">
          <label>故障描述 <span class="required">*</span></label>
          <textarea v-model="faultDescription"
                    class="form-control"
                    placeholder="请详细描述设备故障情况"
                    rows="4"
                    required></textarea>
        </div>
      </div>

      <!-- 模态框底部 -->
      <div class="modal-footer">
        <button class="btn-cancel" @click="$emit('close')">取消</button>
        <button class="btn-confirm" @click="createOrder" :disabled="!faultDescription">
          创建工单
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, defineProps, defineEmits } from 'vue'
  import axios from 'axios'

  const props = defineProps({
    deviceId: { type: String, required: true },
    operatorID: { type: String, required: true }
  })

  const emit = defineEmits(['close', 'order-created'])

  const faultDescription = ref('')
  const loading = ref(false)

  const createOrder = async () => {
    try {
      loading.value = true

      const payload = {
        OperatorID: props.operatorID,
        EquipmentId: parseInt(props.deviceId),
        FaultDescription: faultDescription.value
      }
      console.log('创建工单发送的数据:', payload)

      const response = await axios.post('/api/Equipment/CreateOrder', payload)

      if (response.data && response.data.message) {
        alert(response.data.message)
        emit('order-created')
        emit('close')
      }
    } catch (error) {
      console.error('创建工单失败:', error)
      if (error.response && error.response.data) {
        alert(`创建工单失败: ${error.response.data}`)
      } else {
        alert('创建工单失败，请重试')
      }
    } finally {
      loading.value = false
    }
  }
</script>

<style scoped>
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
    display: flex;
    flex-direction: column;
    overflow: hidden; /* 防止子元素溢出 */
  }

  /* 模态框头部 */
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
    max-width: 100%;
    padding: 8px 12px;
    border: 1px solid #ddd;
    border-radius: 4px;
    font-size: 14px;
    resize: vertical;
    box-sizing: border-box;
    background-color: #fff;
    color: #2c3e50;
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

  @media (max-width: 480px) {
    .modal-content {
      width: 95%;
    }

    .modal-body {
      padding: 16px;
    }

    .form-control {
      font-size: 13px;
    }
  }
</style>
