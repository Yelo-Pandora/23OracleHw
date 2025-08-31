<template>
  <DashboardLayout>
    <div class="account-content-container">
      <!-- 区域1: 所有员工可见 (修改个人信息) -->
      <div class="common-section">
        <h2 class="section-title">个人信息管理</h2>
        <div class="info-card">
          <p>这里是用于修改您自己账户信息的功能区。</p>
          <p><strong>当前用户:</strong> {{ userStore.userInfo?.Username || 'N/A' }}</p>
          <p><strong>您的角色:</strong> {{ userStore.role || 'N/A' }}</p>
          <!-- 在这里可以放置修改密码、联系方式等的表单 -->
          <button class="action-button">修改密码</button>
        </div>
      </div>

      <!-- 区域2: 仅管理员可见 (管理所有账户) -->
      <div v-if="isAdmin" class="admin-section">
        <hr class="section-divider">
        <h2 class="section-title">系统账户列表</h2>

        <!-- 员工账户信息表格 -->
        <h3 class="table-title">员工账户</h3>
        <div class="table-wrapper">
          <table class="account-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>账户</th>
                <th>姓名</th>
                <th>性别</th>
                <th>所属部门</th>
                <th>职位</th>
                <th>底薪</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="staffAccounts.length === 0">
                <td colspan="7">暂无员工数据</td>
              </tr>
              <tr v-for="account in staffAccounts" :key="account.StaffInfo.StaffId">
                <td>{{ account.StaffInfo.StaffId }}</td>
                <td>{{ account.Account }}</td>
                <td>{{ account.StaffInfo.StaffName }}</td>
                <td>{{ account.StaffInfo.Gender }}</td>
                <td>{{ account.StaffInfo.Department }}</td>
                <td>{{ account.StaffInfo.Position }}</td>
                <td>{{ account.StaffInfo.BaseSalary }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- 商户账户信息表格 -->
        <h3 class="table-title">商户账户</h3>
        <div class="table-wrapper">
          <table class="account-table">
            <thead>
              <tr>
                <th>商户ID</th>
                <th>账户</th>
                <th>用户名</th>
                <th>商户名称</th>
                <th>租户姓名</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="tenantAccounts.length === 0">
                <td colspan="5">暂无商户数据</td>
              </tr>
              <tr v-for="account in tenantAccounts" :key="account.StoreInfo.StoreId">
                <td>{{ account.StoreInfo.StoreId }}</td>
                <td>{{ account.Account }}</td>
                <td>{{ account.Username }}</td>
                <td>{{ account.StoreInfo.StoreName }}</td>
                <td>{{ account.StoreInfo.TenantName }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

    </div>
  </DashboardLayout>
</template>

<script setup>
  import { ref, onMounted, computed } from 'vue';
  import DashboardLayout from '@/components/BoardLayout.vue';
  // 1. 导入用户状态仓库
  import { useUserStore } from '@/user/user';

  // 2. 获取 userStore 实例
  const userStore = useUserStore();

  // 3. 创建一个计算属性来判断当前用户是否为管理员
  //    这让模板中的逻辑更清晰，并且是响应式的
  const isAdmin = computed(() => userStore.userInfo.authority === 1);

  // --- 以下是管理员表格所需的数据和逻辑 ---
  const staffAccounts = ref([]);
  const tenantAccounts = ref([]);

  // 模拟的API数据...
  const mockApiData = [
    { "Account": "admin", "Username": "admin", "Identity": "员工", "Authority": 1, "StaffInfo": { "StaffId": 1, "StaffName": "admin", "Department": "财务部", "Gender": "男", "Position": "经理", "BaseSalary": 90000 }, "StoreInfo": null },
    { "Account": "staff002", "Username": "张三", "Identity": "员工", "Authority": 0, "StaffInfo": { "StaffId": 2, "StaffName": "张三", "Department": "市场部", "Gender": "男", "Position": "职员", "BaseSalary": 12000 }, "StoreInfo": null },
    { "Account": "stringss", "Username": "stringss", "Identity": "商户", "Authority": 1, "StaffInfo": null, "StoreInfo": { "StoreId": 1, "StoreName": "品牌A", "TenantName": "tx" } },
    { "Account": "tenant002", "Username": "李四", "Identity": "商户", "Authority": 0, "StaffInfo": null, "StoreInfo": { "StoreId": 2, "StoreName": "品牌B", "TenantName": "李四" } },
    { "Account": "guest", "Username": "guest", "Identity": "Guest", "Authority": 0, "StaffInfo": null, "StoreInfo": null }
  ];

  const fetchAndProcessAccounts = () => {
    const allAccounts = mockApiData;
    staffAccounts.value = allAccounts.filter(acc => acc.Identity === '员工' && acc.StaffInfo);
    tenantAccounts.value = allAccounts.filter(acc => acc.Identity === '商户' && acc.StoreInfo);
  };

  // 4. 在组件挂载时，检查用户权限
  onMounted(() => {
    // 只有当用户是管理员时，才调用函数获取并处理所有账户的数据
    if (isAdmin.value) {
      console.log("当前用户是管理员，正在加载账户列表...");
      fetchAndProcessAccounts();
    } else {
      console.log("当前用户是普通员工，不加载账户列表。");
    }
  });
</script>

<style scoped>
  /* 通用样式 */
  .account-content-container {
    padding: 1rem;
  }

  .section-title {
    font-size: 1.5rem;
    font-weight: 600;
    color: #333;
    margin-bottom: 1rem;
  }

  .table-title {
    font-size: 1.2rem;
    font-weight: 500;
    margin-top: 2rem;
    margin-bottom: 1rem;
  }

  /* 个人信息区域样式 */
  .common-section .info-card {
    background-color: #fff;
    padding: 20px;
    border-radius: 8px;
    box-shadow: 0 2px 12px 0 rgba(0,0,0,0.06);
  }

  .common-section p {
    margin: 10px 0;
  }

  .action-button {
    background-color: #4A90E2;
    color: white;
    border: none;
    padding: 10px 15px;
    border-radius: 5px;
    cursor: pointer;
    margin-top: 10px;
  }

    .action-button:hover {
      background-color: #357ABD;
    }

  /* 管理员区域样式 */
  .section-divider {
    margin-top: 2rem;
    margin-bottom: 2rem;
    border: 0;
    border-top: 1px solid #dee2e6;
  }

  .table-wrapper {
    background-color: #fff;
    border-radius: 8px;
    box-shadow: 0 2px 12px 0 rgba(0,0,0,0.08);
    overflow: hidden;
  }

  .account-table {
    width: 100%;
    border-collapse: collapse;
    text-align: left;
  }

    .account-table th, .account-table td {
      padding: 12px 15px;
      border-bottom: 1px solid #dee2e6;
      vertical-align: middle;
    }

    .account-table thead th {
      background-color: #f5f7fa;
      color: #909399;
      font-weight: 600;
      font-size: 0.9em;
      text-transform: uppercase;
    }

    .account-table tbody tr:hover {
      background-color: #f1f3f5;
    }

    .account-table tbody tr:last-of-type td {
      border-bottom: none;
    }
</style>
