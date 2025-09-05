<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-header">
        <h3>操作设备 - {{ device.equipment_TYPE }}{{ device.equipment_ID }}</h3>
        <button class="modal-close" @click="$emit('close')">&times;</button>
      </div>

      <div class="modal-body">
        <div class="device-info">
          <p><strong>当前状态:</strong> {{ device.equipment_STATUS }}</p>
        </div>

        <div class="action-section">
          <h4>可用操作</h4>

          <div v-if="loadingActions" class="loading-actions">
            <div class="spinner-small"></div>
            <span>加载操作列表中...</span>
          </div>

          <div v-else-if="actions.length === 1 && actions[0] === '当前状态不可操作'" class="no-actions">
            <p>当前状态下无可用的操作</p>
          </div>

          <div v-else class="action-buttons">
            <button v-for="action in actions"
                    :key="action"
                    class="action-btn"
                    :class="{ 'danger-btn': action.includes('紧急停止') }"
                    @click="executeAction(action)"
                    :disabled="executingAction">
              {{ action }}
            </button>
          </div>
        </div>

        <div v-if="actionResult" class="action-result" :class="actionResult.success ? 'success' : 'error'">
          <i :class="actionResult.success ? 'fas fa-check-circle' : 'fas fa-exclamation-circle'"></i>
          <span>{{ actionResult.message }}</span>
        </div>
      </div>

      <div class="modal-footer">
        <button class="btn-secondary" @click="$emit('close')">关闭</button>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, onMounted, defineProps, defineEmits } from 'vue'
  import axios from 'axios'

  const props = defineProps({
    device: { type: Object, required: true },
    operatorID: { type: String, required: true }
  })

  const emit = defineEmits(['close', 'action-completed'])

  const loadingActions = ref(true)
  const executingAction = ref(false)
  const actions = ref([])
  const actionResult = ref(null)

  // 获取可用操作列表
  const fetchAvailableActions = async () => {
    loadingActions.value = true
    actionResult.value = null
    try {
      const response = await axios.get('/api/Equipment/ActionsList', {
        params: {
          id: props.device.equipment_ID,
          OperatorID: props.operatorID
        }
      })
      actions.value = response.data
    } catch (err) {
      console.error('获取操作列表失败:', err)
      actions.value = []
    } finally {
      loadingActions.value = false
    }
  }

  // 执行操作
  const executeAction = async (action) => {
    executingAction.value = true
    actionResult.value = null
    try {
      const payload = {
        EquipmentID: props.device.equipment_ID,
        OperatorID: props.operatorID,
        Operation: action
      }
      const response = await axios.post('/api/Equipment/operate', payload)

      actionResult.value = {
        success: true,
        message: response.data.result || '操作成功'
      }

      // 操作成功后刷新父组件设备状态
      setTimeout(() => {
        emit('action-completed')
      }, 1000)
    } catch (err) {
      if (err.response && err.response.status === 400) {
        alert(`操作设备失败：${err.response.data}`)
      } else {
        alert('操作设备失败：服务器错误')
      }
    } finally {
      executingAction.value = false
    }
  }

  // 挂载时获取操作列表
  onMounted(() => {
    fetchAvailableActions()
  })
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
  padding: 20px;
}

.modal {
  background: #fff;
  border-radius: 8px;
  width: 100%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 4px 15px rgba(0,0,0,0.2);
}

.modal-header{
  padding: 15px 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}
  .modal-footer {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    padding: 16px 20px;
    border-top: 1px solid #eee;
  }
  .modal-header {
    border-bottom: 1px solid #eee;
  }

.modal-header h3 {
  margin: 0;
  font-size: 18px;
  color: #2c3e50;
}

.modal-close {
  background: none;
  border: none;
  font-size: 20px;
  cursor: pointer;
  color: #6c757d;
}

.modal-body {
  padding: 20px;
}

.device-info {
  margin-bottom: 20px;
  padding-bottom: 15px;
  border-bottom: 1px solid #eee;
}

.device-info p {
  margin: 5px 0;
  font-size: 14px;      
  color: #2c3e50;
}

.action-section h4 {
  margin-bottom: 12px;
  font-size: 16px;
  color: #2c3e50;
}


.action-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.action-btn {
  padding: 10px 16px;
  background-color: #3498db;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: background-color 0.3s;
}

.action-btn:hover:not(:disabled) {
  background-color: #2980b9;
}

.action-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.action-btn.danger-btn {
  background-color: #e74c3c;
}

.action-btn.danger-btn:hover:not(:disabled) {
  background-color: #c0392b;
}

.loading-actions, .no-actions {
  font-size: 14px;
  color: #6c757d;
  display: flex;
  align-items: center;
  gap: 10px;
}

.spinner-small {
  width: 16px;
  height: 16px;
  border: 2px solid #f3f3f3;
  border-top: 2px solid #3498db;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.action-result {
  margin-top: 15px;
  padding: 10px;
  border-radius: 4px;
  font-size: 14px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.action-result.success {
  background-color: rgba(46, 204, 113, 0.1);
  color: #27ae60;
  border: 1px solid rgba(46, 204, 113, 0.2);
}

.action-result.error {
  background-color: rgba(231, 76, 60, 0.1);
  color: #c0392b;
  border: 1px solid rgba(231, 76, 60, 0.2);
}


.btn-secondary {
  padding: 10px 16px;
  font-size: 14px;
  background-color: #6c757d;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.btn-secondary:hover {
  background-color: #5a6268;
}

</style>
