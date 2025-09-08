<!-- components/AccountLinkModal.vue -->
<template>
  <!-- 模态框的显示由 v-if="visible" 控制 -->
  <div v-if="visible" class="modal-overlay" @click.self="close">
    <div class="modal-content">
      <h3>关联账号: {{ accountToLink?.Account || 'N/A' }}</h3>

      <div class="form-group">
        <label for="link-type">关联类型:</label>
        <select id="link-type" v-model="selectedType">
          <option value="员工">员工</option>
          <option value="商户">商户</option>
        </select>
      </div>

      <!-- 员工选择列表 -->
      <div class="form-group" v-if="selectedType === '员工'">
        <label for="staff-select">选择员工:</label>
        <select id="staff-select" v-model="selectedId">
          <option :value="null" disabled>-- 请选择 --</option>
          <option v-for="staff in staffs" :key="staff.STAFF_ID" :value="staff.STAFF_ID">
            {{ staff.STAFF_NAME }} (ID: {{ staff.STAFF_ID }})
          </option>
        </select>
      </div>

      <!-- 商户选择列表 -->
      <div class="form-group" v-if="selectedType === '商户'">
        <label for="store-select">选择商户:</label>
        <select id="store-select" v-model="selectedId">
          <option :value="null" disabled>-- 请选择 --</option>
          <option v-for="store in stores" :key="store.STORE_ID" :value="store.STORE_ID">
            {{ store.STORE_NAME }} (ID: {{ store.STORE_ID }})
          </option>
        </select>
      </div>

      <div class="modal-actions">
        <!-- 点击“确认”时，触发 confirm 事件并传递数据 -->
        <button @click="handleConfirm" class="action-button">确认关联</button>
        <!-- 点击“取消”时，触发 close 事件 -->
        <button @click="close" class="action-button action-button--secondary">取消</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue';

// 1. 定义 props 来接收父组件传递的数据
const props = defineProps({
  visible: { type: Boolean, required: true },
  accountToLink: { type: Object, default: null },
  staffs: { type: Array, required: true },
  stores: { type: Array, required: true },
});

// 2. 定义 emits 来声明该组件可以触发的自定义事件
const emit = defineEmits(['close', 'confirm']);

// 3. 内部状态
const selectedType = ref('员工');
const selectedId = ref(null);

// 4. 监听关联类型的变化，并重置选中的ID
watch(() => selectedType.value, () => {
  selectedId.value = null;
});

// 5. 事件处理函数
const close = () => {
  emit('close');
};

const handleConfirm = () => {
  if (!selectedId.value) {
    alert('请选择一个要关联的员工或商户。');
    return;
  }
  // 触发 confirm 事件，并把选中的数据传递给父组件
  emit('confirm', {
    type: selectedType.value,
    id: selectedId.value,
  });
};
</script>

<style scoped>
  /* 将之前模态框的样式从 AccountContent.vue 剪切到这里 */
  .modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.5);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 1000;
  }

  .modal-content {
    background-color: #fff;
    padding: 2rem;
    border-radius: 8px;
    width: 90%;
    max-width: 500px;
    box-shadow: 0 5px 15px rgba(0,0,0,0.3);
  }

    .modal-content h3 {
      margin-top: 0;
    }

  .form-group {
    margin-bottom: 1rem;
  }

    .form-group label {
      display: block;
      margin-bottom: 0.5rem;
    }

    .form-group select {
      width: 100%;
      padding: 0.5rem;
      border: 1px solid #ccc;
      border-radius: 4px;
    }

  .modal-actions {
    margin-top: 2rem;
    display: flex;
    justify-content: flex-end;
    gap: 1rem;
  }

  .action-button--secondary {
    background-color: #6c757d;
  }

    .action-button--secondary:hover {
      background-color: #5a6268;
    }
  /* ... (可以从 AccountContent.vue 复制 .action-button 的基本样式) ... */
  .action-button {
    background-color: #4A90E2;
    color: white;
    border: none;
    padding: 10px 15px;
    border-radius: 5px;
    cursor: pointer;
  }

    .action-button:hover {
      background-color: #357ABD;
    }
</style>
