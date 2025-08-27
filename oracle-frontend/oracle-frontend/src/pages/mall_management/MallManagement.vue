<template>
  <div class="mall-management">
    <h2>商场管理</h2>
    <p>商场/店铺管理模块（已将“新增店面区域”表单集成于此）。</p>

    <div class="mall-actions">
      <div class="action-grid">
        <router-link v-if="role === '商户'" class="card" :to="'/mall-management/store-status-request'">
          <h3>店面状态申请</h3>
          <p>提交店铺状态变更申请（商户）</p>
        </router-link>

        <router-link v-if="role === '商户'" class="card" :to="'/mall-management/store-management'">
          <h3>我的店铺</h3>
          <p>查看并管理您的店铺信息</p>
        </router-link>

        <router-link v-if="role === '员工'" class="card" :to="'/mall-management/store-status-approval'">
          <h3>店面状态审批</h3>
          <p>审批商户提交的店面状态申请（员工）</p>
        </router-link>

        <router-link v-if="role === '员工'" class="card" :to="'/mall-management/create-merchant'">
          <h3>新增店面</h3>
          <p>在商场中添加新店面（员工）</p>
        </router-link>

        <router-link v-if="role === '员工'" class="card" :to="'/mall-management/merchant-statistics-report'">
          <h3>商户统计报表</h3>
          <p>查看商户、区域和类型的统计信息</p>
        </router-link>
    </div>

    <!-- 移除原来的新增区域表单，MallManagement 仅作为导航/汇总页面 -->
    <section class="child-view">
      <router-view />
    </section>
  </div>
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
.mall-management { padding: 16px }
.mall-actions { margin-top: 16px }

.action-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
  margin: 16px 0;
}
.card {
  display: block;
  padding: 16px;
  background: #ffffff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.06);
  text-decoration: none;
  color: inherit;
  transition: transform 0.12s ease, box-shadow 0.12s ease;
}
.card h3 { margin: 0 0 6px 0; font-size: 16px }
.card p { margin: 0; color: #666; font-size: 13px }
.card:hover {
  transform: translateY(-4px);
  box-shadow: 0 6px 18px rgba(0,0,0,0.1);
}

.child-view {
  margin-top: 24px;
  padding: 16px;
  background: #f9f9f9;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}
</style>
