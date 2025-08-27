<template>
  <DashboardLayout>
    <div class="create-merchant">
      <h1>新增店面</h1>

      <form @submit.prevent="submitForm" class="form">
        <div class="form-group">
          <label for="storeName">店铺名称</label>
          <input id="storeName" v-model="form.storeName" type="text" required />
        </div>

        <div class="form-group">
          <label for="storeType">店铺类型</label>
          <input id="storeType" v-model="form.storeType" type="text" required />
        </div>

        <div class="form-group">
          <label for="storeStatus">店铺状态</label>
          <select id="storeStatus" v-model="form.storeStatus" required>
            <option value="正常营业">正常营业</option>
            <option value="歇业中">歇业中</option>
            <option value="翻新中">翻新中</option>
            <option value="维修中">维修中</option>
            <option value="暂停营业">暂停营业</option>
          </select>
        </div>

        <div class="form-group">
          <label for="contactInfo">联系方式</label>
          <input id="contactInfo" v-model="form.contactInfo" type="text" required />
        </div>

        <div class="form-group">
          <label for="description">店铺简介</label>
          <textarea id="description" v-model="form.description" required></textarea>
        </div>

        <div class="actions">
          <button type="submit">提交</button>
        </div>
      </form>

      <div v-if="success" class="success">{{ success }}</div>
      <div v-if="error" class="error">{{ error }}</div>
    </div>
  </DashboardLayout>
</template>

<script setup>
import { ref } from 'vue';
import axios from 'axios';
import DashboardLayout from '@/components/BoardLayout.vue';

const form = ref({
  storeName: '',
  storeType: '',
  storeStatus: '正常营业',
  contactInfo: '',
  description: ''
});

const success = ref('');
const error = ref('');

async function submitForm() {
  success.value = '';
  error.value = '';
  try {
    const response = await axios.post('/api/Store/Create', form.value);
    success.value = response.data.message || '店面创建成功';
  } catch (e) {
    error.value = e.response?.data?.error || '店面创建失败';
  }
}
</script>

<style scoped>
.create-merchant {
  padding: 16px;
}
.form {
  max-width: 600px;
  margin: 0 auto;
  background: #fff;
  padding: 16px;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}
.form-group {
  margin-bottom: 16px;
}
label {
  display: block;
  margin-bottom: 8px;
  font-weight: bold;
}
input, select, textarea {
  width: 100%;
  padding: 8px;
  box-sizing: border-box;
  border: 1px solid #ccc;
  border-radius: 4px;
}
textarea {
  min-height: 80px;
}
.actions {
  text-align: right;
}
button {
  padding: 8px 16px;
  background-color: #409eff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
button:hover {
  background-color: #66b1ff;
}
.success {
  color: green;
  margin-top: 16px;
}
.error {
  color: red;
  margin-top: 16px;
}
</style>
