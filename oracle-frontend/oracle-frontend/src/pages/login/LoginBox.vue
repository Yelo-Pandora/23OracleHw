<!-- src/pages/login/LoginBox.vue -->
<template>
  <div class="login-container">
    <div class="role-switcher">
      <button 
        class="role-btn" 
        :class="{ active: role === '员工' }"
        @click="switchRole('员工')">
        员工
      </button>
      <button 
        class="role-btn" 
        :class="{ active: role === '商户' }"
        @click="switchRole('商户')">
        商户
      </button>
    </div>

    <h2>欢迎登录</h2>
    
    <form @submit.prevent="handleLogin">
      <div class="input-group">
        <input 
          type="text" 
          class="input-field" 
          :placeholder="usernamePlaceholder"
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
      <button type="submit" class="login-btn">登 录</button>
    </form>

    <div class="links-container">
      <a href="#register">注册账号</a>
      <a href="#forgot">忘记密码?</a>
    </div>

    <hr class="divider">

    <div class="guest-login">
      <a href="#" @click.prevent="handleGuestLogin">>>> 游客登录</a>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed } from 'vue';
  import axios from 'axios';
  import { useRouter } from 'vue-router'
  import { useUserStore } from '@/user/user'

  //响应式状态
  const role = ref('员工'); // '员工' 或 '商户' 或 '游客'
  const username = ref('');
  const password = ref('');
  const isPasswordVisible = ref(false);
  const router = useRouter();
  const userStore = useUserStore();


  //计算属性
  const usernamePlaceholder = computed(() => {
    return role.value === '员工' ? '请输入员工账号' : '请输入商户账号';
  });

  const passwordFieldType = computed(() => {
    return isPasswordVisible.value ? 'text' : 'password';
  });

  //函数
  function switchRole(newRole) {
    role.value = newRole;
  }

  async function handleLogin() {
    if (!username.value || !password.value) {
      alert('请输入账号和密码！');
      return;
    }

    try {
      const response = await axios.post('/api/Accounts/login', {
        acc: username.value,
        pass: password.value,
        identity: role.value,
      });

      const { ACCOUNT, USERNAME, IDENTITY, AUTHORITY } = response.data;

      const userInfo = {
        account: ACCOUNT,
        username: USERNAME,
        authority: AUTHORITY
      };

      userStore.login(ACCOUNT, IDENTITY, userInfo);

      router.push('/');
    } catch (err) {
      alert('登录失败，请检查用户名或密码');
      console.error(err);
    }
  }

  function handleGuestLogin() {
    userStore.login('guest-token', 'guest', { name: '游客' });
    router.push('/');
  }
</script>

<style scoped>
.login-container {
  width: 400px;
  padding: 40px;
  background: rgba(25, 25, 25, 0.65);
  backdrop-filter: blur(10px);
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
  color: #fff;
  text-align: center;
}

.role-switcher {
  display: flex;
  margin-bottom: 25px;
  border-radius: 8px;
  background: rgba(0, 0, 0, 0.2);
  padding: 4px;
}

.role-btn {
  flex: 1;
  padding: 10px;
  border: none;
  background-color: transparent;
  color: #ccc;
  font-size: 16px;
  cursor: pointer;
  border-radius: 6px;
  transition: all 0.3s ease;
}

.role-btn.active {
  background-color: #007BFF;
  color: #fff;
  font-weight: bold;
}

.login-container h2 {
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
}

.input-field::placeholder {
  color: #a0a0a0;
}

.input-field:focus {
  outline: none;
  background-color: rgba(255, 255, 255, 0.15);
  border-color: #007BFF;
}

.password-toggle {
  position: absolute;
  top: 50%;
  right: 15px;
  transform: translateY(-50%);
  cursor: pointer;
  color: #a0a0a0;
  user-select: none;
}

.login-btn {
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

.login-btn:hover {
  background-color: #0056b3;
}

.login-btn:active {
  transform: scale(0.98);
}

.links-container {
  display: flex;
  justify-content: space-between;
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

.divider {
  margin: 30px 0;
  border: none;
  border-top: 1px solid rgba(255, 255, 255, 0.2);
}

.guest-login a {
  color: #fff;
  text-decoration: none;
  font-size: 16px;
  font-weight: bold;
  transition: color 0.3s, letter-spacing 0.3s;
}

.guest-login a:hover {
  color: #4dabff;
  letter-spacing: 1px;
}
</style>
