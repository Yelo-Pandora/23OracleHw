<template>
  <div class="forgot-password-container">
    <h2>重置密码</h2>
    
    <form @submit.prevent="handleResetPassword">
      <div class="input-group">
        <input 
          type="text" 
          class="input-field" 
          placeholder="请输入员工/商户账号"
          v-model="account" 
          required>
      </div>
      <div class="input-group">
        <input 
          type="password"
          class="input-field" 
          placeholder="请输入新密码"
          v-model="newPassword" 
          required>
      </div>
      <div class="input-group">
        <input 
          type="password"
          class="input-field" 
          placeholder="请再次输入新密码"
          v-model="confirmPassword" 
          required>
      </div>
      <button type="submit" class="reset-btn">确认修改</button>
    </form>

    <div class="links-container">
      <a href="#" @click.prevent="$emit('switchToLogin')">返回登录</a>
    </div>
  </div>
</template>

<script setup>
  import { ref, defineEmits } from 'vue';
  import axios from 'axios';

  const emit = defineEmits(['switchToLogin']);

  const account = ref('');
  const newPassword = ref('');
  const confirmPassword = ref('');

  async function handleResetPassword() {
    if (!account.value || !newPassword.value || !confirmPassword.value) {
      alert('请填写所有信息！');
      return;
    }
    if (newPassword.value !== confirmPassword.value) {
      alert('两次输入的密码不一致！');
      return;
    }

    try {
      // 获取输入的账号信息
      const getInfoUrl = `/api/Accounts/info/${account.value}`;
      const infoResponse = await axios.get(getInfoUrl);

      // 将获取到的账号信息保存下来
      const currentAccountData = infoResponse.data;

      // 更新密码字段
      currentAccountData.PASSWORD = newPassword.value;
      console.log('准备更新的账号数据:', currentAccountData);
      // 构造修改请求和数据
      const patchUrl = `/api/Accounts/alter/${account.value}?operatorAccountId=${account.value}`;
      const patchData = currentAccountData;

      // 修改请求
      const patchResponse = await axios.patch(patchUrl, patchData);

      if (patchResponse.data == '更新成功') {
        alert('密码修改成功！');
        emit('switchToLogin');
      } else {
        alert(patchResponse.data.message || patchResponse.data || '密码修改失败，未知错误。');
      }
    } catch (err) {
      let errorMessage = '请求失败，请检查网络或稍后再试。';
      if (err.response) {
        // 如果具有错误响应，则说明可以连接服务端
        if (err.response.status === 404) {
          // 若GET请求返回404，则说明账号不存在
          errorMessage = '账号不存在，请检查您输入的账号。';
        } else {
          errorMessage = err.response.data || '操作失败，请重试。';
        }
      }
      alert(errorMessage);
      console.error('重置密码错误:', err);
    }
  }
</script>

<style scoped>
/* 这里的样式可以复用 LoginBox 或 RegisterBox 的样式，以保持界面一致性 */
.forgot-password-container {
  width: 400px;
  padding: 40px;
  background: rgba(25, 25, 25, 0.65);
  backdrop-filter: blur(10px);
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
  color: #fff;
  text-align: center;
}

.forgot-password-container h2 {
  margin-bottom: 30px;
  font-weight: 300;
  font-size: 28px;
}

.input-group {
  position: relative;
  margin-bottom: 25px;
}

.input-field {
  width: 100%;
  padding: 14px 20px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 8px;
  color: #fff;
  font-size: 16px;
  box-sizing: border-box;
}

.input-field::placeholder {
  color: #a0a0a0;
}

.reset-btn {
  width: 100%;
  padding: 14px;
  background-color: #007BFF;
  border: none;
  border-radius: 8px;
  color: #fff;
  font-size: 18px;
  font-weight: bold;
  cursor: pointer;
  transition: background-color 0.3s, transform 0.1s;
}

.reset-btn:hover {
  background-color: #0056b3;
}

.links-container {
  margin-top: 20px;
  font-size: 14px;
}

.links-container a {
  color: #ccc;
  text-decoration: none;
  transition: color 0.3s;
}

.links-container a:hover {
  color: #007BFF;
}
</style>
