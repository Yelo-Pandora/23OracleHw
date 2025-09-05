<!-- src/App.vue -->
<template>
  <div class="page-wrapper">
    <div class="background-container"></div>
    
    <header class="system-header">
        <div class="logo"></div>
        <div class="title">商业综合体管理系统</div>
    </header>

    <main class="content-center">
      <LoginBox />
    </main>
  </div>
</template>

<script setup>
import LoginBox from './LoginBox.vue';
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
      background-color: #007BFF;
      border-radius: 50%;
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
