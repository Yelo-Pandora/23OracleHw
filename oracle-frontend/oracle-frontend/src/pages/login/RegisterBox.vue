<template>
  <div class="register-container">
    <h2>注册账号</h2>
    
    <form @submit.prevent="handleRegister">
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
          type="text" 
          class="input-field" 
          placeholder="请输入您的姓名/商户名称"
          v-model="username" 
          required>
      </div>
      <div class="input-group">
        <input 
          :type="passwordFieldType"
          class="input-field" 
          placeholder="请输入密码"
          v-model="password" 
          required>
      </div>
      <div class="input-group">
        <input 
          :type="passwordFieldType"
          class="input-field" 
          placeholder="请再次输入密码"
          v-model="confirmPassword" 
          required>
      </div>
      <div class="input-group">
        <select class="input-field" id="select" v-model="identity" required>
          <option value="" disabled>请选择身份</option>
          <option value="员工">员工</option>
          <option value="商户">商户</option>
        </select>
      </div>
      <button type="submit" class="register-btn">注 册</button>
    </form>

    <div class="links-container">
      <a href="#" @click.prevent="switchToLogin">已有账号？去登录</a>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, defineEmits } from 'vue';
import axios from 'axios';

const emit = defineEmits(['switchToLogin']);

const account = ref('');
const username = ref('');
const password = ref('');
const confirmPassword = ref('');
const identity = ref(''); // '员工' 或 '商户'
const isPasswordVisible = ref(false); // 可以根据需要添加密码显示/隐藏功能

const passwordFieldType = computed(() => {
  return isPasswordVisible.value ? 'text' : 'password';
});

async function handleRegister() {
  if (!account.value || !username.value || !password.value || !confirmPassword.value || !identity.value) {
    alert('请填写所有注册信息！');
    return;
  }
  if (password.value !== confirmPassword.value) {
    alert('两次输入的密码不一致！');
    return;
  }

  try {
    // 预留与后端API交互的接口
    const response = await axios.post('/api/Accounts/register', {
      account: account.value,
      username: username.value,
      password: password.value,
      identity: identity.value,
    });

    if (response.data.message) { // 假设后端返回 success 字段
      alert('注册成功！请登录。');
      console.log('RegisterBox: 注册成功，触发 switchToLogin 事件');
      emit('switchToLogin'); // 注册成功后切换回登录界面
    } else {
      alert(response.data.message || '注册失败，请稍后再试。');
    }
  } catch (err) {
    alert('注册请求失败，账号已存在。');
    console.error('注册错误:', err);
  }
}

function switchToLogin() {
  emit('switchToLogin');
}
</script>

<style scoped>
/* 复用公共样式 */
.register-container {
  width: 400px;
  padding: 40px;
  background: rgba(25, 25, 25, 0.65);
  backdrop-filter: blur(10px);
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
  color: #fff;
  text-align: center;
}

.register-container h2 {
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
    transition: border-color 0.3s, background-color 0.3s;
    /* 针对 select 元素的额外样式 */
    -webkit-appearance: none;
    -moz-appearance: none;
    appearance: none;
    /*background-image: url('data:image/svg+xml;charset=US-ASCII,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20width%3D%22292.4%22%20height%3D%22292.4%22%3E%3Cpath%20fill%3D%22%23cccccc%22%20d%3D%22M287%2069.4a17.6%2017.6%200%200%200-13.2-5.4H18.2c-7.9%200-14.3%206.4-14.3%2014.3s6.4%2014.3%2014.3%2014.3h255.6c4.7%200%208.5-3.8%208.5-8.5s-3.8-8.5-8.5-8.5z%22%2F%3E%3Cpath%20fill%3D%22%23cccccc%22%20d%3D%22M146.2%20223.6c-4.7%200-8.5-3.8-8.5-8.5V85.7c0-4.7%203.8-8.5%208.5-8.5s8.5%203.8%208.5%208.5v129.4c0%204.7-3.8%208.5-8.5%208.5z%22%2F%3E%3C%2Fsvg%3E');*/ /* 自定义下拉箭头 */
/*    background-repeat: no-repeat;
    background-position: right 15px center;
    background-size: 12px;
    padding-right: 40px;*/
  }

  #select {
    background-image: url('@/assets/下拉.svg');
    background-repeat: no-repeat;
    background-position: right 15px center;
    background-size: 16px;
    padding-right: 40px;
  }


  .input-field::placeholder {
    color: #a0a0a0;
  }

  /* 针对 select 元素中的 option 标签的样式 */
  .input-field option {
    background-color: #333; /* 选项的背景色，使其在下拉时可见 */
    color: #fff; /* 选项的文本颜色 */
  }

    .input-field option:disabled {
      color: #a0a0a0; /* 禁用选项的颜色 */
    }

  /* 当 select 处于未选择状态时，显示 placeholder 颜色 */
  .input-field:not(:valid) {
    color: #a0a0a0; /* 当没有选择有效值时，文本显示为placeholder颜色 */
  }
  /* 当 select 选择了一个有效值时，文本颜色恢复为白色 */
  .input-field:valid {
    color: #fff;
  }

.register-btn {
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

.register-btn:hover {
  background-color: #218838;
}

.register-btn:active {
  transform: scale(0.98);
}

.links-container {
  margin-top: 15px;
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
