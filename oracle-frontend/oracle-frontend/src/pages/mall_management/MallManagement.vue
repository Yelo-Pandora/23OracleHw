<template>
  <div class="mall-management-container">
    <div class="page-header">
      <h1>商场管理</h1>
      <p>商场与店铺管理中心，请选择一项功能以继续。</p>
    </div>

    <div class="action-grid">
      <router-link v-if="role === '商户'" class="action-card" :to="'/mall-management/store-status-request'">
        <div class="card-icon">
          <!-- Placeholder for a real icon -->
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
        </div>
        <div class="card-content">
          <h3>店面状态申请</h3>
          <p>提交店铺状态变更申请</p>
        </div>
      </router-link>

      <router-link v-if="role === '商户'" class="action-card" :to="'/mall-management/store-management'">
        <div class="card-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" /></svg>
        </div>
        <div class="card-content">
          <h3>我的店铺</h3>
          <p>查看并管理您的店铺信息</p>
        </div>
      </router-link>

      <router-link v-if="role === '员工'" class="action-card" :to="'/mall-management/store-status-approval'">
        <div class="card-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
        </div>
        <div class="card-content">
          <h3>店面状态审批</h3>
          <p>审批商户提交的状态申请</p>
        </div>
      </router-link>

      <router-link v-if="role === '员工'" class="action-card" :to="'/mall-management/create-merchant'">
        <div class="card-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
        </div>
        <div class="card-content">
          <h3>新增店面</h3>
          <p>在商场中添加新店面</p>
        </div>
      </router-link>

      <router-link v-if="role === '员工'" class="action-card" :to="{ name: 'RentCollection' }">
        <div class="card-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z" /></svg>
        </div>
        <div class="card-content">
          <h3>租金管理</h3>
          <p>生成、查询并确认租金账单</p>
        </div>
      </router-link>

      <router-link v-if="role === '员工'" class="action-card" :to="'/mall-management/merchant-statistics-report'">
        <div class="card-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" /></svg>
        </div>
        <div class="card-content">
          <h3>商户统计报表</h3>
          <p>查看商户、区域和类型统计</p>
        </div>
      </router-link>

      <!-- 为员工提供快速访问店铺详情的入口（将店铺详情导航移到商场管理页） -->
      <router-link v-if="role === '员工'" class="action-card" to="/store-management/store-detail">
        <div class="card-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" /></svg>
        </div>
        <div class="card-content">
          <h3>店铺详情</h3>
          <p>查看任意店铺的详细信息</p>
        </div>
      </router-link>

      <router-link v-if="role === '员工'" class="action-card" :to="{ name: 'MerchantRentStatisticsReport' }">
        <div class="card-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 3.055A9.001 9.001 0 1020.945 13H11V3.055z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.488 9H15V3.512A9.025 9.025 0 0120.488 9z" /></svg>
        </div>
        <div class="card-content">
          <h3>商户租金统计报表</h3>
          <p>分析商户租金收缴情况</p>
        </div>
      </router-link>
    </div>

    <section class="child-view-container">
      <router-view />
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted, watch, computed } from 'vue'
import axios from 'axios'
import { useUserStore } from '@/stores/user'

// expose role for template quick links
const userStore = useUserStore()
const role = computed(() => userStore.role || '游客')
</script>

<style scoped>
/* 使用 var() 函数来引用在 BoardLayout.vue 中定义的 CSS 变量 */
:root {
  --primary-color: #1abc9c;
  --card-bg: #ffffff;
  --card-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  --card-hover-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  --text-primary: #333;
  --text-secondary: #6c757d;
  --border-radius: 12px;
}

.mall-management-container {
  /* 保持默认间距，避免空的 CSS 规则导致 lint 错误 */
  padding: 0 0 0 0;
}

.page-header {
  margin-bottom: 24px;
}

.page-header h1 {
  font-size: 24px;
  font-weight: 600;
  color: var(--text-primary);
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

/* 子视图容器，确保与整体布局融合 */
.child-view-container {
  margin-top: 32px;
  /* 移除独立背景和阴影，使其无缝集成 */
}
</style>
