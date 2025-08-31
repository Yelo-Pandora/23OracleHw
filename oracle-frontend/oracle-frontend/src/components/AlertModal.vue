<template>
  <div class="alert-overlay">
    <div class="alert-modal">
      <div class="alert-body">{{ message }}</div>
      <div class="alert-actions">
        <button class="btn-ok" @click="close">确定</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { defineEmits, defineProps } from 'vue';

defineProps({ message: { type: String, required: true } });
const emit = defineEmits(['close']);
const close = () => emit('close');
</script>

<style scoped>
.alert-overlay {
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
.alert-modal {
  background: #fff;
  padding: 18px;
  border-radius: 8px;
  width: 360px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.2);
}
.alert-body {
  margin-bottom: 18px;
  font-size: 15px;
}
.alert-actions {
  display: flex;
  justify-content: flex-end;
}
.btn-ok {
  background-color: #007BFF;
  color: #fff;
  border: none;
  padding: 8px 14px;
  border-radius: 4px;
  cursor: pointer;
}
</style>
