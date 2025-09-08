<template>
  <DashboardLayout>
    <div class="device-management">
      <!-- 权限不足时的提示 -->
      <!--<div v-if="!hasAccess" class="no-access">
      <h2>无权访问</h2>
      <p>您没有查看设备管理页面的权限。如需访问，请联系管理员。</p>
      <button class="btn-primary" @click="goHome">
        返回首页
      </button>
    </div>-->
      <!-- 有权限时渲染子路由 -->
      <router-view />
    </div>
  </DashboardLayout>
</template>

<script setup>
import { computed } from 'vue'
import { useUserStore } from '@/user/user';
import DashboardLayout from '@/components/BoardLayout.vue';


const userStore = useUserStore();
const currentUserRole = userStore.role;

 const requiredRoles = ['员工'] 
 const hasAccess = computed(() => requiredRoles.includes(currentUserRole))

  // 返回首页
  function goHome() {
    router.push('/')
  }
</script>

<style scoped>
  .device-management {
    padding: 20px;
  }

  .no-access {
    text-align: center;
    padding: 50px;
    background: #fff;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
  }

    .no-access h2 {
      color: #c0392b;
      margin-bottom: 10px;
    }

    .no-access p {
      margin-bottom: 20px;
      color: #555;
    }

  .btn-primary {
    padding: 10px 16px;
    background-color: #3498db;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
  }

    .btn-primary:hover {
      background-color: #2980b9;
    }
</style>
