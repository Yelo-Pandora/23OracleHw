<template>
  <DashboardLayout>
    <div class="account-content-container">
      <!-- 区域1: 所有员工/商户可见，管理自身账户 -->
      <div class="common-section">
        <h2 class="section-title">个人信息管理</h2>
        <div class="info-card">
          <div class="details-container">

            <!-- 左侧为账号信息 -->
            <div class="column basic-info">
              <h4>基础信息</h4>
              <p><strong>账号:</strong> {{ userStore.userInfo?.account || 'N/A' }}</p>
              <p><strong>用户名:</strong> {{ userStore.userInfo?.username || 'N/A' }}</p>
              <p><strong>身份:</strong> {{ userStore.role || 'N/A' }}</p>
            </div>

            <!-- 右侧为详细信息，员工/商户 -->
            <div class="column profile-info">
              <!-- 加载状态 -->
              <div v-if="isLoadingProfile">
                <p>正在加载档案...</p>
              </div>
              <div v-else>
                <!-- 关联的员工信息 -->
                <div v-if="currentUserStaffInfo" class="role-details">
                  <h4>员工档案</h4>
                  <p><strong>员工姓名:</strong> {{ currentUserStaffInfo.StaffName }}</p>
                  <p><strong>所属部门:</strong> {{ currentUserStaffInfo.Department }}</p>
                  <p><strong>职位:</strong> {{ currentUserStaffInfo.Position }}</p>
                </div>
                <!-- 关联的商户信息 -->
                <div v-if="currentUserStoreInfo" class="role-details">
                  <h4>商户档案</h4>
                  <p><strong>商户名称:</strong> {{ currentUserStoreInfo.storeName }}</p>
                  <p><strong>租户姓名:</strong> {{ currentUserStoreInfo.tenantName }}</p>
                </div>
                <!-- 如果没有信息 -->
                <p v-if="!currentUserStaffInfo && !currentUserStoreInfo">
                  暂无更多详细档案信息。
                </p>
              </div>
            </div>
          </div>

          <!-- 1. 创建一个操作按钮的容器 -->
          <div class="actions-container">
            <!-- 2. 修改按钮文字，并绑定新的点击事件 -->
            <button class="action-button" @click="modifyInfo">修改用户名</button>

            <!-- 3. 使用 <template> 和 v-if 来包裹所有仅管理员可见的按钮 -->
            <template v-if="isAdmin">
              <button class="action-button" @click="addAccount">添加账号</button>
              <button class="action-button" @click="alterAccount">编辑账号</button>
              <button class="action-button" @click="linkAccount">关联账号</button>
              <button class="action-button" @click="grantTempPermission">临时权限</button>
              <button class="action-button action-button--danger" @click="deleteAccount">删除账号</button>
            </template>
          </div>
        </div>
      </div>

      <!-- 区域2: 仅管理员可见，管理所有账户 -->
      <div v-if="isAdmin" class="admin-section">
        <hr class="section-divider">
        <h2 class="section-title">系统账户列表</h2>

        <!-- 筛选和搜索 -->
        <div class="filter-controls">
          <select v-model="searchFilter" class="filter-select">
            <option value="Account">按账户搜索</option>
            <option value="Username">按用户名搜索</option>
            <option value="StaffName">按员工姓名搜索</option>
            <option value="Department">按部门搜索</option>
            <option value="StoreName">按商户名称搜索</option>
          </select>
          <input type="text"
                 v-model="searchQuery"
                 placeholder="输入关键词..."
                 class="search-input" />
        </div>

        <!-- 员工账户信息表格 -->
        <h3 class="table-title">员工账户</h3>
        <div class="table-wrapper">
          <table class="account-table">
            <thead>
              <tr>
                <!-- 复选框 -->
                <th class="checkbox-col">
                  <input type="checkbox"
                         :checked="isAllStaffSelected"
                         @change="toggleAllStaffSelection" />
                </th>
                <th>员工ID</th>
                <th>账户</th>
                <th>用户名</th>
                <th>姓名</th>
                <th>性别</th>
                <th>所属部门</th>
                <th>职位</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="filteredStaffAccounts.length === 0">
                <td colspan="7">暂无员工数据</td>
              </tr>
              <tr v-for="account in filteredStaffAccounts" :key="account.StaffInfo.StaffId">
                <td>
                  <input type="checkbox"
                         :value="account.StaffInfo.StaffId"
                         v-model="selectedStaffIds" />
                </td>
                <td>{{ account.StaffInfo.StaffId }}</td>
                <td>{{ account.Account }}</td>
                <td>{{ account.Username }}</td>
                <td>{{ account.StaffInfo.StaffName }}</td>
                <td>{{ account.StaffInfo.StaffSex || '未知' }}</td>
                <td>{{ account.StaffInfo.Department }}</td>
                <td>{{ account.StaffInfo.Position }}</td>
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
                <th class="checkbox-col">
                  <input type="checkbox"
                         :checked="isAllTenantSelected"
                         @change="toggleAllTenantSelection" />
                </th>
                <th>商户ID</th>
                <th>账户</th>
                <th>用户名</th>
                <th>商户名称</th>
                <th>租户姓名</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="filteredTenantAccounts.length === 0">
                <td colspan="5">暂无商户数据</td>
              </tr>
              <tr v-for="account in filteredTenantAccounts" :key="account.StoreInfo.StoreId">
                <td>
                  <input type="checkbox"
                         :value="account.StoreInfo.StoreId"
                         v-model="selectedTenantIds" />
                </td>
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
  import { computed, onMounted } from 'vue';
  import DashboardLayout from '@/components/BoardLayout.vue';
  import { useUserStore } from '@/user/user';

  // 按需导入需要的函数
  import {
    useCurrentUserProfile,
    useAccountList,
    useAccountSelection,
    useAccountActions
  } from './AccountInteract.js';

  // --- 基础设置 ---
  const userStore = useUserStore();
  const isAdmin = computed(() => userStore.userInfo?.authority === 1);

  // --- 模块化调用 ---

  // 模块 A: 获取个人信息
  const {
    currentUserStaffInfo,
    currentUserStoreInfo,
    isLoadingProfile,
    fetchCurrentUserDetails
  } = useCurrentUserProfile(userStore);

  // 模块 B: 获取、筛选和搜索账户列表
  const {
    searchFilter,
    searchQuery,
    fetchAndProcessAccounts,
    filteredStaffAccounts,
    filteredTenantAccounts
  } = useAccountList();

  // 模块 C: 处理列表勾选 (它依赖于模块B的结果)
  const {
    selectedStaffIds,
    selectedTenantIds,
    isAllStaffSelected,
    toggleAllStaffSelection,
    isAllTenantSelected,
    toggleAllTenantSelection
  } = useAccountSelection(filteredStaffAccounts, filteredTenantAccounts);

  // 模块 D: 处理按钮操作 (它依赖于模块C的结果)
  const {
    modifyInfo,
    addAccount,
    deleteAccount,
    linkAccount,
    grantTempPermission
  } = useAccountActions(userStore, selectedStaffIds, selectedTenantIds);


  // --- 生命周期钩子 ---
  onMounted(() => {
    // 调用模块A的函数
    fetchCurrentUserDetails();

    // 如果是管理员，调用模块B的函数
    if (isAdmin.value) {
      fetchAndProcessAccounts();
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
    transition: background-color 0.2s;
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

  .details-container {
    display: flex;
    gap: 40px;
  }

  .column {
    flex: 1;
    min-width: 0;
  }

  .basic-info {
    border-right: 1px solid #e9ecef;
    padding-right: 40px;
  }

  .column h4 {
    margin-top: 0;
    margin-bottom: 15px;
    font-size: 1rem;
    font-weight: 600;
    color: #343a40;
  }

  .actions-container {
    display: flex;
    flex-wrap: wrap; /* 如果屏幕过窄，按钮可以换行 */
    gap: 10px; /* 按钮之间的间距 */
    margin-top: 20px;
    padding-top: 20px;
    border-top: 1px solid #e9ecef; /* 在信息和按钮之间加一条分隔线 */
  }

  /* 为“删除”等危险操作按钮添加的特殊样式 */
  .action-button--danger {
    background-color: #e74c3c; /* 红色 */
  }

    .action-button--danger:hover {
      background-color: #c0392b; /* 深红色 */
    }

  .list-header {
    display: flex;
    justify-content: space-between; /* 两端对齐 */
    align-items: center; /* 垂直居中 */
    margin-bottom: 1rem;
  }

    /* 把标题的下边距去掉，因为 list-header 已经有了 */
    .list-header .section-title {
      margin-bottom: 0;
    }

  /* 筛选和搜索控件的容器 */
  .filter-controls {
    display: flex;
    gap: 10px;
  }

  /* 下拉筛选框和搜索输入框的通用样式 */
  .filter-select,
  .search-input {
    padding: 8px 12px;
    border: 1px solid #ccc;
    border-radius: 5px;
    font-size: 0.9rem;
  }

  .search-input {
    width: 200px; /* 给搜索框一个固定宽度 */
  }

  /* 复选框列的样式 */
  .checkbox-col {
    width: 40px; /* 给复选框列一个固定的窄宽度 */
    text-align: center;
  }
</style>
