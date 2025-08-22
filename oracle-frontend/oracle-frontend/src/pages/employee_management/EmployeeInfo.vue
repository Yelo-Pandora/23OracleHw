<!-- src/pages/employee/EmployeeInfo.vue -->
<template>
  <DashboardLayout>
    <div class="content-container">
      <h1>员工信息管理</h1>
      <p>
        这里是员工信息管理页面。如果您能看到这个页面，并且左侧的“员工管理”菜单项处于高亮激活状态，说明菜单和路由跳转功能正常工作
      </p>
    </div>
  </DashboardLayout>
</template>

<script setup>
// 3. 导入基础布局组件
import DashboardLayout from '@/components/BoardLayout.vue';
</script>

<style scoped>
/* 一些简单的样式 */
.content-container {
  padding: 24px;
  background-color: #ffffff;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

h1 {
  margin-top: 0;
  color: #333;
}

p {
  color: #666;
  font-size: 16px;
  line-height: 1.6;
}
</style>
