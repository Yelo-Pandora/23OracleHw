<template>
  <!-- 使用 DashboardLayout 组件作为页面的根元素 -->
  <DashboardLayout>
    <!--
      这里的所有内容，都会被自动插入到 DashboardLayout 组件的 <slot> 位置。
    -->
    <div class="home-content">
      <h1>欢迎, Admin!</h1>
      <p>这里是您的仪表盘主页。</p>

      <!-- 您可以把图二中的各种卡片、图表组件放在这里 -->
      <div class="card-container">
        <div class="card">信息卡片 1</div>
        <div class="card">信息卡片 2</div>
        <div class="card">信息卡片 3</div>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup>
  // 1. 导入布局组件
  import DashboardLayout from '@/components/BoardLayout.vue';
  import { useUserStore } from '@/user/user';
  const userStore = useUserStore();
</script>

<style scoped>
  /* 只属于首页的样式 */
  .home-content {
    background-color: #fff;
    padding: 20px;
    border-radius: 8px;
    box-shadow: 0 2px 12px 0 rgba(0,0,0,0.1);
  }

  .card-container {
    display: flex;
    gap: 20px;
    margin-top: 20px;
  }

  .card {
    flex: 1;
    padding: 20px;
    border: 1px solid #eee;
    border-radius: 4px;
  }
</style>
