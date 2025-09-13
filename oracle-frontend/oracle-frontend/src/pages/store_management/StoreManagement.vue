<template>
  <DashboardLayout>
    <div class="page-container">
      <div class="page-header">
        <h1>店铺管理</h1>
        <p>您的店铺管理中心，请选择一项功能以继续。</p>
      </div>

      <div class="action-grid">
  <router-link v-if="role === '商户'" class="action-card" to="/store-management/store-detail">
          <div class="card-icon">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" /></svg>
          </div>
          <div class="card-content">
            <h3>店铺详情</h3>
            <p>查看并管理您的店铺信息</p>
          </div>
        </router-link>
          <router-link v-if="role === '商户'" class="action-card" to="/store-management/get-store">
            <div class="card-icon">
              <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7h18M5 7v10a2 2 0 002 2h10a2 2 0 002-2V7M9 11h6M9 15h6" /></svg>
            </div>
            <div class="card-content">
              <h3>申请租赁店面</h3>
              <p>选择空置店面并提交租赁申请，提交后店面将被标记为已租用。</p>
            </div>
          </router-link>
        <router-link class="action-card" :to="{ name: 'MyRentBills' }">
          <div class="card-icon">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" /></svg>
          </div>
          <div class="card-content">
            <h3>我的租金</h3>
            <p>查看并处理您的租金账单</p>
          </div>
        </router-link>
        <router-link v-if="role === '商户'" class="action-card" to="/store-management/store-status-request">
          <div class="card-icon">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
          </div>
          <div class="card-content">
            <h3>店面状态申请</h3>
            <p>提交店铺状态变更申请</p>
          </div>
        </router-link>
        <router-link class="action-card" :to="{ name: 'MyRentStatisticsReport' }">
          <div class="card-icon">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 3.055A9.001 9.001 0 1020.945 13H11V3.055z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.488 9H15V3.512A9.025 9.025 0 0120.488 9z" /></svg>
          </div>
          <div class="card-content">
            <h3>我的租金统计</h3>
            <p>查看您的租金统计信息</p>
          </div>
        </router-link>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '@/components/BoardLayout.vue'
import { computed } from 'vue'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()
const role = computed(() => userStore.role || '游客')
</script>

<style scoped>
:root {
  --primary-color: #1abc9c;
  --card-bg: #ffffff;
  --card-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  --card-hover-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  --text-primary: #333;
  --text-secondary: #6c757d;
  --border-radius: 12px;
}

.page-container {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.page-header h1 {
  font-size: 24px;
  font-weight: 600;
  margin-bottom: 4px;
}

.page-header p {
  font-size: 14px;
  color: var(--text-secondary);
}

.action-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 24px;
}

.action-card {
  display: flex;
  align-items: center;
  padding: 20px;
  background-color: var(--card-bg);
  border-radius: var(--border-radius);
  box-shadow: var(--card-shadow);
  text-decoration: none;
  color: inherit;
  transition: transform 0.2s ease-out, box-shadow 0.2s ease-out;
  border-left: 5px solid transparent;
}

.action-card:hover {
  transform: translateY(-5px);
  box-shadow: var(--card-hover-shadow);
  border-left-color: var(--primary-color);
}

.card-icon {
  flex-shrink: 0;
  margin-right: 20px;
  color: var(--primary-color);
}

.card-icon svg {
  width: 32px;
  height: 32px;
}

.card-content h3 {
  margin: 0 0 8px 0;
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

.card-content p {
  margin: 0;
  font-size: 13px;
  color: var(--text-secondary);
  line-height: 1.5;
}
</style>
