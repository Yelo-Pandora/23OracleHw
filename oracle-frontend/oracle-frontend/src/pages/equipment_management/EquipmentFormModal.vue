<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal">

      <div class="modal-header">
        <h3>添加设备</h3>
        <button class="modal-close" @click="$emit('close')">&times;</button>
      </div>

      <!-- 表单 -->
      <div class="modal-body">
        <form @submit.prevent="submitForm">
          <div class="form-group">
            <label>设备ID <span style="color:red">*</span></label>
            <input type="number" v-model="formData.equipment_ID" required />
          </div>

          <div class="form-group">
            <label>设备类型 <span style="color:red">*</span></label>
            <select v-model="formData. equipment_TYPE" required>
              <option value="">请选择设备类型</option>
              <option value="空调">空调</option>
              <option value="照明">照明</option>
              <option value="电梯">电梯</option>
            </select>
          </div>

          <div class="form-group">
            <label>设备状态 <span style="color:red">*</span></label>
            <select v-model="formData.equipment_STATUS" required>
              <option value="">请选择设备状态</option>
              <option value="运行中">运行中</option>
              <option value="待机">待机</option>
              <option value="离线">离线</option>
            </select>
          </div>

          <div class="form-group">
            <label>设备接口</label>
            <input type="text" v-model="formData.PORT" placeholder="例如: COM3" />
          </div>

          <div class="form-group">
            <label>设备花费</label>
            <input type="number" v-model.number="formData.equipment_COST" placeholder="请输入设备花费" />
          </div>

          <div class="form-group">
            <label>购入时间 <span style="color:red">*</span></label>
            <input type="datetime-local" v-model="formData.BUY_TIME" required />
          </div>
        </form>
      </div>

      <!-- 底部按钮 -->
      <div class="modal-footer">
        <button class="btn-secondary" @click="$emit('close')">取消</button>
        <button class="btn-primary" @click="submitForm" :disabled="submitting">
          {{ submitting ? '提交中...' : '添加设备' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { reactive, ref } from 'vue'
  import axios from 'axios'

  const props = defineProps({
    operatorID: { type: String, required: true }
  })

  const emit = defineEmits(['close', 'saved'])
  const submitting = ref(false)

  const formData = reactive({
    equipment_ID: '',
    equipment_TYPE: '',
    equipment_STATUS: '',
    PORT: '',
    equipment_COST: null,
    BUY_TIME: ''
  })

  const submitForm = async () => {
    if (!props.operatorID) {
      alert('操作人员信息缺失，无法添加设备');
      submitting.value = false;
      return;
    }
    if (!formData.equipment_ID || !formData.equipment_TYPE || !formData.equipment_STATUS || !formData.BUY_TIME) {
      alert('请填写必填项')
      return
    }

    submitting.value = true

    try {
      const response = await axios.post(
        `/api/Equipment/AddEquipment?OperatorID=${props.operatorID}`,
        formData
      )
      const savedDevice = response.data
      emit('saved', savedDevice)
      emit('close')
    } catch (err) {
      alert(`请求出错: ${err.message}`);
    } finally {
      submitting.value = false
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
    background: rgba(0,0,0,0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    padding: 10px;
  }

  .modal {
    background: #fff;
    border-radius: 8px;
    width: 100%;
    max-width: 500px; 
    max-height: 90vh;
    overflow-y: auto;
    box-shadow: 0 4px 15px rgba(0,0,0,0.2);
    font-size: 16px; 
  }

  .modal-header, .modal-footer {
    padding: 15px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .modal-header {
    border-bottom: 1px solid #eee;
  }

  .modal-footer {
    border-top: 1px solid #eee;
  }

  .modal-body {
    padding: 20px;
  }

  .form-group {
    margin-bottom: 15px;
    display: flex;
    flex-direction: column;
  }

    .form-group label {
      font-weight: 500;
      margin-bottom: 6px;
    }

    .form-group input,
    .form-group select {
      width: 100%;
      padding: 10px 12px;
      font-size: 16px; /* 字体大一些 */
      border: 1px solid #ccc;
      border-radius: 6px;
      box-sizing: border-box;
      transition: all 0.2s;
    }

      .form-group input:focus,
      .form-group select:focus {
        outline: none;
        border-color: #3498db;
        box-shadow: 0 0 0 2px rgba(52, 152, 219, 0.2);
      }

  /* 底部按钮 */
  .btn-primary,
  .btn-secondary {
    padding: 10px 16px;
    font-size: 16px;
    border-radius: 6px;
    cursor: pointer;
    min-width: 100px;
    text-align: center;
  }

  .btn-primary {
    background: #3498db;
    color: #fff;
    border: none;
  }

    .btn-primary:disabled {
      opacity: 0.7;
      cursor: not-allowed;
    }

    .btn-primary:hover:not(:disabled) {
      background: #2980b9;
    }

  .btn-secondary {
    background: #6c757d;
    color: #fff;
    border: none;
  }

    .btn-secondary:hover {
      background: #5a6268;
    }

  .modal-close {
    background: none;
    border: none;
    font-size: 22px;
    cursor: pointer;
    color: #6c757d;
  }
</style>
