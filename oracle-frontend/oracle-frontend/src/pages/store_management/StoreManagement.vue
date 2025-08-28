<template>
  <DashboardLayout>
    <div class="store-management">
      <h2>店铺管理导航</h2>
      <p>请选择以下功能以继续：</p>

      <div class="navigation">
        <router-link class="card" to="/store-management/store-detail">
          <h3>我的店铺详情</h3>
          <p>查看并管理您的店铺信息</p>
        </router-link>
        <router-link class="card" :to="{ name: 'MyRentBills' }">
          <h3>我的租金</h3>
          <p>查看并处理您的租金账单</p>
        </router-link>

        <router-link class="card" :to="{ name: 'MyRentStatisticsReport' }">
          <h3>我的租金统计</h3>
          <p>查看您的租金统计信息</p>
        </router-link>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '@/components/BoardLayout.vue'
</script>

<style scoped>
.store-management {
  padding: 16px;
}

.navigation {
  margin-top: 16px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.nav-item {
  display: block;
  padding: 16px;
  background: #ffffff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  text-decoration: none;
  color: inherit;
  transition: transform 0.12s ease, box-shadow 0.12s ease;
}

.nav-item h3 {
  margin: 0 0 6px 0;
  font-size: 16px;
}

.nav-item p {
  margin: 0;
  color: #666;
  font-size: 13px;
}

.nav-item:hover {
  transform: translateY(-4px);
  box-shadow: 0 6px 18px rgba(0, 0, 0, 0.1);
}
</style>
