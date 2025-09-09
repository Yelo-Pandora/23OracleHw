<template>
  <DashboardLayout>
    <div class="tenant-area-view">

      <!-- 页面加载状态 -->
      <div v-if="loading" class="loading-state">正在加载您的店铺信息...</div>
      <div v-else-if="error" class="error-state">{{ error }}</div>

      <!-- 数据加载成功后显示 -->
      <div v-else-if="storeInfo">

        <!-- 页面顶部：显示商户信息 -->
        <header class="view-header">
          <h1>店铺区域管理</h1>
          <div class="store-info-card">
            <div class="info-item">
              <span class="label">店铺名称:</span>
              <span class="value">{{ storeInfo.STORE_NAME }}</span>
            </div>
            <div class="info-item">
              <span class="label">店铺ID:</span>
              <span class="value">{{ storeInfo.STORE_ID }}</span>
            </div>
            <div class="info-item">
              <span class="label">租户姓名:</span>
              <span class="value">{{ storeInfo.TENANT_NAME }}</span>
            </div>
          </div>
          <router-link to="/store-management/get-store" class="manage-store-btn">
            管理店铺
          </router-link>
        </header>

        <!-- 页面主体：显示区域列表 -->
        <main class="area-list-container">
          <h3>我租赁的区域</h3>
          <div v-if="rentedAreas.length === 0" class="no-results">
            您当前没有租赁任何区域。
          </div>
          <div v-else class="table-wrapper">
            <table class="area-table">
              <thead>
                <tr>
                  <th>区域ID</th>
                  <th>是否空置</th>
                  <th>面积 (m²)</th>
                  <th>租赁状态</th>
                  <th>基础租金</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="area in rentedAreas" :key="area.areaId">
                  <td>{{ area.AreaId }}</td>
                  <td>{{ area.IsEmpty === 1 ? '是' : '否' }}</td>
                  <td>{{ area.AreaSize }}</td>
                  <td>{{ area.RentStatus || 'N/A' }}</td>
                  <td>{{ area.BaseRent }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </main>

      </div>

      <!-- 如果 API 返回 404 或其他原因导致 storeInfo 为 null -->
      <div v-else class="no-results">
        未能加载到与您账号关联的店铺信息。
      </div>

    </div>
  </DashboardLayout>
</template>

<script setup>
  import { ref, onMounted } from 'vue';
  import { useUserStore } from '@/user/user';
  import axios from 'axios';
  import DashboardLayout from '@/components/BoardLayout.vue';
  import { RouterLink } from 'vue-router';

  const userStore = useUserStore();

  // 存储从 API 获取的数据
  const storeInfo = ref(null);
  const rentedAreas = ref([]);

  // 页面状态
  const loading = ref(true);
  const error = ref(null);

  const fetchMyAreas = async () => {
    const tenantAccount = userStore.userInfo?.account;
    if (!tenantAccount) {
      error.value = "无法获取您的账号信息，请重新登录。";
      loading.value = false;
      return;
    }

    try {
      const response = await axios.get('/api/Areas/tenant-dashboard', {
        params: { tenantAccount }
      });
      console.log("获取商户区域列表成功:", response.data);
      // 将返回的数据分别赋值给ref
      storeInfo.value = response.data.StoreInfo;
      rentedAreas.value = response.data.RentedAreas;

    } catch (err) {
      console.error("获取商户区域列表失败:", err);
      if (err.response && err.response.status === 404) {
        error.value = err.response.data; // 显示后端返回的 "未找到..." 错误信息
      } else {
        error.value = "加载您的区域列表失败，请稍后再试。";
      }
    } finally {
      loading.value = false;
    }
  };

  onMounted(() => {
    fetchMyAreas();
  });
</script>

<style scoped>
  .tenant-area-view {
    padding: 2rem;
  }

  .view-header {
    margin-bottom: 2rem;
    padding-bottom: 1.5rem;
    border-bottom: 1px solid #e9ecef;
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

    .view-header h1 {
      margin: 0;
      white-space: nowrap;
    }

  .store-info-card {
    background-color: #f8f9fa;
    display: flex;
    flex-wrap: nowrap;
    gap: 2rem;
    color: #495057;
    font-size: 1.2rem;
  }

  .info-item .label {
    font-weight: 600;
    color: #495057;
    margin-right: 0.5rem;
  }

  .area-list-container h3 {
    margin-bottom: 1rem;
    margin-top: 2rem;
  }

  .loading-state, .error-state, .no-results {
    text-align: center;
    padding: 40px 20px;
    font-size: 1.1rem;
    color: #6c757d;
  }

  .error-state {
    color: #dc3545;
  }

  .table-wrapper {
    background-color: #fff;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.08);
    overflow: hidden;
  }

  .area-table {
    width: 100%;
    border-collapse: collapse;
  }

    .area-table th, .area-table td {
      padding: 12px 15px;
      text-align: left;
      border-bottom: 1px solid #ddd;
    }

    .area-table th {
      background-color: #f8f9fa;
    }

  .manage-store-btn {
    background-color: #007bff;
    color: white;
    padding: 0.6rem 1.2rem;
    border-radius: 6px;
    text-decoration: none;
    font-weight: 500;
    transition: background-color 0.2s ease-in-out;
    flex-shrink: 0;
  }

    .manage-store-btn:hover {
      background-color: #0056b3;
    }
  .info-item {
    display: flex;
    align-items: center;
  }
</style>
