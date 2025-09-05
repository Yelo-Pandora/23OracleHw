<template>
  <div class="page-wrapper">
    <div class="background-container"></div>

    <header class="system-header">
      <img src="@/assets/登录.svg" alt="系统Logo" class="logo">
      <div class="title">商业综合体管理系统</div>
    </header>
    <!-- 切换组件 -->
    <main class="content-center">
      <LoginBox v-if="currentView === 'login'"
                @switchToRegister="currentView = 'register'"
                @switchToForgotPassword="currentView = 'forgotPassword'" />
      <RegisterBox v-else-if="currentView === 'register'"
                   @switchToLogin="currentView = 'login'" />
      <ForgotPasswordBox v-else-if="currentView === 'forgotPassword'"
                         @switchToLogin="currentView = 'login'" />
    </main>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import LoginBox from './LoginBox.vue';
import RegisterBox from './RegisterBox.vue';
import ForgotPasswordBox from './ForgotBox.vue';

  const currentView = ref('login'); // 控制显示 LoginBox 还是 RegisterBox
</script>

<style>
  /* 这里的样式是全局的，因为它们定义了 App 的基础布局 */
  .page-wrapper {
    height: 100%;
    position: relative;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
  }

  .background-container {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-size: cover;
    background-position: center;
    filter: blur(3px);
    transform: scale(1.05);
    z-index: -1;
  }

  .system-header {
    position: absolute;
    top: 30px;
    left: 40px;
    display: flex;
    align-items: center;
    z-index: 1;
  }

    .system-header .logo {
      width: 40px;
      height: 40px;
      margin-right: 15px;
    }

    .system-header .title {
      font-size: 24px;
      color: #fff;
      font-weight: bold;
      text-shadow: 0 2px 4px rgba(0, 0, 0, 0.5);
    }

  .content-center {
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
  }
</style>
