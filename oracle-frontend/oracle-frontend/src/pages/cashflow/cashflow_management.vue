<template>
  <DashboardLayout>
    <div class="cashflow-dashboard">
      <!-- 这里会切换 DashboardHome.vue 或 CashFlowDetail.vue -->
      <router-view />
    </div>
  </DashboardLayout>
</template>

<script setup>
  import DashboardLayout from '@/components/BoardLayout.vue'
</script>

<style scoped>
  .cashflow-dashboard {
    width: 100%;
    height: 100%;
  }
</style>
