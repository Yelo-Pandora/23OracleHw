<template>
  <div class="device-location">
    <h3>设备位置信息</h3>

    <!-- 已绑定位置 -->
    <div v-if="device.area_ID" class="location-bound">
      <div class="current-location">
        <i class="fas fa-map-marker-alt"></i>
        <div class="location-info">
          <h4>当前位置</h4>
          <p>{{ device.area_ID}}</p>
        </div>
      </div>

      <button class="btn-unbind" @click="unbindLocation">
        <i class="fas fa-unlink"></i> 解绑位置
      </button>
    </div>

    <!-- 未绑定位置 -->
    <div v-else class="location-unbound">
      <div class="unbound-message">
        <i class="fas fa-exclamation-circle"></i>
        <p>该设备尚未绑定位置</p>
      </div>

      <div class="bind-form">
        <div class="form-group">
          <label>填写区域ID</label>
          <input type="number" v-model.number="selectedArea" class="form-control" placeholder="请输入区域ID" />
        </div>

        <button class="btn-bind"
                @click="bindLocation"
                :disabled="!selectedArea">
          <i class="fas fa-link"></i> 绑定位置
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, defineProps, defineEmits } from 'vue'
  import axios from 'axios'
  import { useUserStore } from '@/user/user'

  const props = defineProps({
    device: { type: Object, required: true },
  })

  const emit = defineEmits(['location-updated'])

  const selectedArea = ref(null)
  const processing = ref(false)

  const userStore = useUserStore()
  const operatorID = userStore.token

  // 绑定位置
  const bindLocation = async () => {
    if (!selectedArea.value) return

    try {
      processing.value = true
      await axios.post('/api/Equipment/AddEquipmentLocation', null, {
        params: {
          equipmentID: props.device.equipment_ID,
          areaID: selectedArea.value,
          OperatorID: operatorID
        }
      })

      emit('location-updated', selectedArea.value)
      selectedArea.value = null
      alert('位置绑定成功')
    } catch (error) {
      if (error.response && error.response.status === 400) {
        alert(`绑定位置失败：${error.response.data}`)
      } else {
        alert('绑定位置失败：服务器错误')
      }
    } finally {
      processing.value = false
    }
  }

  // 解绑位置
  const unbindLocation = async () => {
    if (!confirm('确定要解绑该设备的位置吗？')) return

    try {
      processing.value = true
      await axios.delete('/api/Equipment/UnbindEquipmentLocation', {
        params: {
          equipmentID: props.device.equipment_ID,
          OperatorID: operatorID
        }
      })

      emit('location-updated', null)
      alert('位置解绑成功')
    } catch (error) {
      console.error('解绑位置失败:', error)
      alert(`解绑位置失败`)
    } finally {
      processing.value = false
    }
  }
</script>

<style scoped>
  .device-location {
    padding: 16px;
  }

    .device-location h3 {
      margin-top: 0;
      color: #2c3e50;
      border-bottom: 1px solid #eaeaea;
      padding-bottom: 12px;
    }

  .location-bound {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 20px;
    background-color: #f8f9fa;
    border-radius: 8px;
    margin-top: 16px;
    max-width: 480px;
  }

  .current-location {
    display: flex;
    align-items: center;
    gap: 12px;
  }

    .current-location i {
      font-size: 24px;
      color: #3498db;
    }

  .location-info h4 {
    margin: 0 0 4px 0;
    color: #2c3e50;
  }

  .location-info p {
    margin: 0;
    color: #27ae60;
    font-weight: 500;
  }

  .btn-unbind {
    padding: 8px 16px;
    background-color: #e74c3c;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 6px;
  }

    .btn-unbind:hover {
      background-color: #c0392b;
    }

  .location-unbound {
    padding: 20px;
    background-color: #f8f9fa; 
    border-radius: 8px;
    max-width: 480px; 
  }

  .unbound-message {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 16px;
  }

  .unbound-message i {
    color: #3498db;
    font-size: 20px;
  }

  .unbound-message p {
    margin: 0;
    color: #2c3e50;
    font-weight: 500;
  }

  .bind-form {
    display: flex;
    gap: 12px;
    align-items: flex-end;
  }

  .form-group {
    flex: 1;
  }

    .form-group label {
      display: block;
      margin-bottom: 6px;
      font-weight: 600;
      color: #2c3e50;
    }

  .form-control {
    width: 100%;
    padding: 8px 12px;
    border: 1px solid #ddd; 
    border-radius: 4px;
    font-size: 14px;
    box-sizing: border-box;
    outline: none;
    transition: border-color 0.2s;
  }

    .form-control:focus {
      border-color: #3498db; 
      box-shadow: 0 0 0 2px rgba(52, 152, 219, 0.2);
    }

  .btn-bind {
    padding: 8px 16px;
    background-color: #3498db;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 6px;
    white-space: nowrap;
  }

    .btn-bind:hover:not(:disabled) {
      background-color: #2980b9;
    }

    .btn-bind:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

  @media (max-width: 768px) {
    .location-bound,
    .location-unbound {
      flex-direction: column;
      gap: 16px;
      align-items: flex-start;
      max-width: 100%;
      margin-left: auto;
      margin-right: auto;
    }

    .bind-form {
      flex-direction: column;
      align-items: stretch;
    }
  }
</style>
