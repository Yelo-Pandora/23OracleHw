<template>
    <div class="create-merchant">
    <h2>新增店面</h2>
    <form @submit.prevent="submitForm">
      <div class="form-group">
        <label for="storeName">店面名称</label>
        <input id="storeName" v-model="storeName" type="text" required />
      </div>
      <div class="form-group">
        <label for="storeLocation">店面位置</label>
        <input id="storeLocation" v-model="storeLocation" type="text" required />
      </div>
      <div class="form-group">
        <label for="storeCategory">店面类别</label>
        <select id="storeCategory" v-model="storeCategory" required>
          <option value="">请选择类别</option>
          <option value="餐饮">餐饮</option>
          <option value="零售">零售</option>
          <option value="服务">服务</option>
        </select>
      </div>
      <button type="submit">提交</button>
    </form>
    <div v-if="successMessage" class="success-message">{{ successMessage }}</div>
    <div v-if="errorMessage" class="error-message">{{ errorMessage }}</div>
.    </div>
</template>

<script setup>
import { ref } from 'vue';
import axios from 'axios';
import DashboardLayout from '@/components/BoardLayout.vue'

const storeName = ref('');
const storeLocation = ref('');
const storeCategory = ref('');
const successMessage = ref('');
const errorMessage = ref('');

const submitForm = async () => {
  successMessage.value = '';
  errorMessage.value = '';
  try {
    const response = await axios.post('/api/stores', {
      name: storeName.value,
      location: storeLocation.value,
      category: storeCategory.value,
    });
    successMessage.value = '店面新增成功！';
    storeName.value = '';
    storeLocation.value = '';
    storeCategory.value = '';
  } catch (error) {
    errorMessage.value = '店面新增失败，请稍后重试。';
  }
};
</script>

<style scoped>
.create-merchant {
  max-width: 600px;
  margin: 0 auto;
}
.form-group {
  margin-bottom: 1rem;
}
label {
  display: block;
  margin-bottom: 0.5rem;
}
input, select {
  width: 100%;
  padding: 0.5rem;
  margin-bottom: 0.5rem;
}
button {
  padding: 0.5rem 1rem;
  background-color: #007bff;
  color: white;
  border: none;
  cursor: pointer;
}
button:hover {
  background-color: #0056b3;
}
.success-message {
  color: green;
  margin-top: 1rem;
}
.error-message {
  color: red;
  margin-top: 1rem;
}
</style>
