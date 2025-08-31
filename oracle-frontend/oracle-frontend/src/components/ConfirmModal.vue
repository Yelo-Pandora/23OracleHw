<template>
  <div class="confirm-overlay">
    <div class="confirm-modal">
      <div class="confirm-body">{{ message }}</div>
      <div class="confirm-actions">
        <button class="btn-cancel" @click="cancel">取消</button>
        <button class="btn-confirm" @click="confirmClick">确定</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { defineEmits, defineProps } from 'vue';

defineProps({
  message: { type: String, required: true }
});

const emit = defineEmits(['close']);

const confirmClick = () => emit('close', true);
const cancel = () => emit('close', false);
</script>

<style scoped>
.confirm-overlay {
  position: fixed;
  left: 0;
  top: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10000;
}
.confirm-modal {
  background: #fff;
  padding: 20px;
  border-radius: 8px;
  width: 400px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.2);
}
.confirm-body {
  margin-bottom: 20px;
  font-size: 16px;
}
.confirm-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
.btn-cancel {
  background-color: #6c757d;
  color: #fff;
  border: none;
  padding: 8px 14px;
  border-radius: 4px;
  cursor: pointer;
}
.btn-confirm {
  background-color: #dc3545;
  color: #fff;
  border: none;
  padding: 8px 14px;
  border-radius: 4px;
  cursor: pointer;
}
</style>
