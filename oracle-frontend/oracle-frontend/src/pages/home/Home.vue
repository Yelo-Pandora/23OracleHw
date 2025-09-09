<template>
  <!-- 使用 DashboardLayout 组件作为页面的根元素 -->
  <DashboardLayout>
    <!--
      这里的所有内容，都会被自动插入到 DashboardLayout 组件的 <slot> 位置。
    -->
    <div class="home-content">
      <h1>欢迎回来</h1>
      <p>您当前的身份: <strong>{{ roleLabel }}</strong></p>

      <div class="card-container">
        <!-- 员工专属快捷入口 -->
        <template v-if="isStaff">
          <router-link class="card" to="/account_management">账号管理</router-link>
          <router-link class="card" to="/mall-management">商场管理</router-link>
          <router-link class="card" to="/parking-management">停车场管理</router-link>
          <router-link class="card" to="/event-management">活动管理</router-link>
          <router-link class="card" to="/equipment-management">设备管理</router-link>
          <router-link class="card" to="/employee_management">员工信息管理</router-link>
          <router-link class="card" to="/cashflow_management/total_salary">员工工资支出</router-link>
        </template>

        <!-- 商户专属快捷入口 -->
        <template v-else-if="isMerchant">
          <router-link class="card" to="/store-management">我的店铺</router-link>
          <router-link class="card" to="/parking-query">车位查询</router-link>
          <router-link class="card" to="/event-query">活动查询</router-link>
        </template>

        <!-- 游客视图 -->
        <template v-else>
          <router-link class="card" to="/parking-query">车位查询</router-link>
          <router-link class="card" to="/event-query">活动查询</router-link>
        </template>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup>
  // 1. 导入布局组件
  import DashboardLayout from '@/components/BoardLayout.vue';
  import { useUserStore } from '@/stores/user';
  const userStore = useUserStore();
  import { computed } from 'vue';

  const role = computed(() => userStore.role || '游客');
  const isStaff = computed(() => role.value === '员工');
  const isMerchant = computed(() => role.value === '商户');
  const roleLabel = computed(() => role.value);
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
