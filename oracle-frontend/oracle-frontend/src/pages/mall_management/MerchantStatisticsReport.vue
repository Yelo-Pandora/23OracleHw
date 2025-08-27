<template>
  <DashboardLayout>
    <div class="merchant-statistics-report">
      <h2>商户信息统计报表</h2>
      <p class="subtitle">按不同维度生成商户、店铺及区域的统计信息。</p>

      <div class="controls-container card">
        <div class="form-group">
          <label for="dimension-select">选择统计维度:</label>
          <select id="dimension-select" v-model="selectedDimension">
            <option value="all">完整报表</option>
            <option value="type">按租户类型</option>
            <option value="area">按店面区域</option>
            <option value="status">按店面状态</option>
          </select>
        </div>
        <button @click="fetchReport" :disabled="loading">
          {{ loading ? '正在生成...' : '生成报表' }}
        </button>
      </div>

      <div v-if="loading" class="loading-indicator card">正在加载报表数据...</div>
      <div v-if="error" class="error-message card">{{ error }}</div>

      <!-- 基础统计卡片 -->
      <div v-if="basicStats" class="stats-cards-container">
          <h3>基础数据一览</h3>
          <div class="action-grid">
              <div class="stat-card card">
                  <div class="stat-number">{{ basicStats.totalStores }}</div>
                  <div class="stat-label">总店铺数</div>
              </div>
              <div class="stat-card card">
                  <div class="stat-number">{{ basicStats.activeStores }}</div>
                  <div class="stat-label">正常营业</div>
              </div>
              <div class="stat-card card">
                  <div class="stat-number">{{ basicStats.vacantAreas }}</div>
                  <div class="stat-label">空置区域</div>
              </div>
              <div class="stat-card card">
                  <div class="stat-number">{{ basicStats.occupancyRate }}%</div>
                  <div class="stat-label">入住率</div>
              </div>
          </div>
      </div>

      <!-- 报表结果展示 -->
      <div v-if="reportData" class="report-container card">
        <h3>{{ reportData.reportTitle }}</h3>
        <div class="report-meta">
          <span>生成时间: {{ new Date(reportData.generateTime).toLocaleString() }}</span>
          <span>操作员: {{ reportData.operatorAccount }}</span>
        </div>

        <!-- 报表内容 -->
        <div v-if="reportData.dimension === 'all' && reportData.data" class="report-content">
            <div class="report-section">
                <h4>报表总览</h4>
                <div class="overview-grid">
                    <p><strong>总店铺数:</strong> {{ reportData.data.overview.totalStores }}</p>
                    <p><strong>正常营业:</strong> {{ reportData.data.overview.activeStores }}</p>
                    <p><strong>总区域数:</strong> {{ reportData.data.overview.totalAreas }}</p>
                    <p><strong>空置区域:</strong> {{ reportData.data.overview.vacantAreas }}</p>
                    <p><strong>入住率:</strong> {{ reportData.data.overview.occupancyRate }}%</p>
                    <p><strong>总收入:</strong> ¥{{ reportData.data.overview.totalRevenue.toFixed(2) }}</p>
                </div>
            </div>
            <div class="report-section" v-if="reportData.data.byType">
                <h4>{{ reportData.data.byType.title }}</h4>
                <table class="report-table">
                    <thead><tr><th>租户类型</th><th>店铺数量</th><th>占比</th><th>总租金</th><th>平均租金</th></tr></thead>
                    <tbody><tr v-for="item in reportData.data.byType.details" :key="item.storeType"><td>{{ item.storeType }}</td><td>{{ item.storeCount }}</td><td>{{ item.percentage }}%</td><td>¥{{ item.totalRent.toFixed(2) }}</td><td>¥{{ item.averageRent.toFixed(2) }}</td></tr></tbody>
                </table>
            </div>
            <div class="report-section" v-if="reportData.data.byStatus">
                <h4>{{ reportData.data.byStatus.title }}</h4>
                <table class="report-table">
                    <thead><tr><th>店铺状态</th><th>店铺数量</th><th>占比</th><th>总租金</th><th>平均租金</th></tr></thead>
                    <tbody><tr v-for="item in reportData.data.byStatus.details" :key="item.storeStatus"><td>{{ item.storeStatus }}</td><td>{{ item.storeCount }}</td><td>{{ item.percentage }}%</td><td>¥{{ item.totalRent.toFixed(2) }}</td><td>¥{{ item.averageRent.toFixed(2) }}</td></tr></tbody>
                </table>
            </div>
            <div class="report-section" v-if="reportData.data.byArea">
                <h4>{{ reportData.data.byArea.title }}</h4>
                <div class="table-container">
                    <table class="report-table">
                        <thead><tr><th>区域ID</th><th>面积(㎡)</th><th>基础租金</th><th>状态</th><th>店铺名称</th><th>租户</th><th>租户类型</th><th>租期</th></tr></thead>
                        <tbody><tr v-for="item in reportData.data.byArea.details" :key="item.areaId"><td>{{ item.areaId }}</td><td>{{ item.areaSize }}</td><td>¥{{ item.baseRent.toFixed(2) }}</td><td>{{ item.rentStatus }}</td><td>{{ item.storeName || '-' }}</td><td>{{ item.tenantName || '-' }}</td><td>{{ item.storeType || '-' }}</td><td>{{ item.rentStart ? `${item.rentStart} ~ ${item.rentEnd}` : '-' }}</td></tr></tbody>
                    </table>
                </div>
            </div>
        </div>
        <div v-else-if="reportData.data" class="report-content">
            <h4>{{ reportData.data.title }}</h4>
            <table class="report-table" v-if="reportData.dimension === 'type'">
                <thead><tr><th>租户类型</th><th>店铺数量</th><th>占比</th><th>总租金</th><th>平均租金</th></tr></thead>
                <tbody><tr v-for="item in reportData.data.details" :key="item.storeType"><td>{{ item.storeType }}</td><td>{{ item.storeCount }}</td><td>{{ item.percentage }}%</td><td>¥{{ item.totalRent.toFixed(2) }}</td><td>¥{{ item.averageRent.toFixed(2) }}</td></tr></tbody>
            </table>
            <table class="report-table" v-if="reportData.dimension === 'status'">
                <thead><tr><th>店铺状态</th><th>店铺数量</th><th>占比</th><th>总租金</th><th>平均租金</th></tr></thead>
                <tbody><tr v-for="item in reportData.data.details" :key="item.storeStatus"><td>{{ item.storeStatus }}</td><td>{{ item.storeCount }}</td><td>{{ item.percentage }}%</td><td>¥{{ item.totalRent.toFixed(2) }}</td><td>¥{{ item.averageRent.toFixed(2) }}</td></tr></tbody>
            </table>
            <div class="table-container" v-if="reportData.dimension === 'area'">
                <table class="report-table">
                    <thead><tr><th>区域ID</th><th>面积(㎡)</th><th>基础租金</th><th>状态</th><th>店铺名称</th><th>租户</th><th>租户类型</th><th>租期</th></tr></thead>
                    <tbody><tr v-for="item in reportData.data.byArea.details" :key="item.areaId"><td>{{ item.areaId }}</td><td>{{ item.areaSize }}</td><td>¥{{ item.baseRent.toFixed(2) }}</td><td>{{ item.rentStatus }}</td><td>{{ item.storeName || '-' }}</td><td>{{ item.tenantName || '-' }}</td><td>{{ item.storeType || '-' }}</td><td>{{ item.rentStart ? `${item.rentStart} ~ ${item.rentEnd}` : '-' }}</td></tr></tbody>
                </table>
            </div>
        </div>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import axios from 'axios';
import { useUserStore } from '@/stores/user';
import DashboardLayout from '@/components/BoardLayout.vue'; // 引入布局组件

const userStore = useUserStore();

const selectedDimension = ref('all');
const loading = ref(false);
const error = ref(null);
const reportData = ref(null);
const basicStats = ref(null);

const API_BASE_URL = '/api/Store';

const fetchBasicStats = async () => {
  try {
    const response = await axios.get(`${API_BASE_URL}/BasicStatistics`);
    basicStats.value = response.data;
  } catch (err) {
    console.error('Failed to fetch basic statistics:', err);
    error.value = '无法加载基础统计数据，请确保后端服务正在运行。';
  }
};

const fetchReport = async () => {
  if (!userStore.userInfo || !userStore.userInfo.account) {
    error.value = '无法获取用户信息，请重新登录。';
    return;
  }

  loading.value = true;
  error.value = null;
  reportData.value = null;

  try {
    const params = {
      operatorAccount: userStore.userInfo.account,
      dimension: selectedDimension.value,
    };
    const response = await axios.get(`${API_BASE_URL}/MerchantStatisticsReport`, { params });
    if (response.data && response.data.data) {
        reportData.value = response.data;
    } else {
        throw new Error("返回的数据格式不正确或数据为空");
    }
  } catch (err) {
    console.error('Failed to fetch statistics report:', err);
    if (err.response) {
      error.value = `报表生成失败: ${err.response.data.error || err.response.statusText}`;
    } else if (err.request) {
      error.value = '无法连接到服务器，请检查API是否正在运行。';
    } else {
      error.value = `请求失败: ${err.message}`;
    }
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  fetchBasicStats();
});
</script>

<style scoped>
/* 借鉴自 CreateMerchant.vue 和 MallManagement.vue */
.merchant-statistics-report {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.card {
  background: #ffffff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.06);
  padding: 24px;
}

h2 {
  font-size: 22px;
  color: #333;
  margin: 0;
  margin-bottom: 4px;
}

.subtitle {
  color: #666;
  font-size: 14px;
  margin: 0 0 16px 0;
}

.controls-container {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
}

.form-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

label {
  font-weight: bold;
  color: #555;
}

select {
  width: 200px;
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}

button {
  background-color: #3498db;
  color: white;
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.2s;
}

button:hover {
  background-color: #2980b9;
}

button:disabled {
  background-color: #a9cce3;
  cursor: not-allowed;
}

.loading-indicator, .error-message {
  text-align: center;
  padding: 20px;
  font-size: 16px;
}

.error-message {
  color: #c00;
  background-color: #fbeae5;
  border: 1px solid #c00;
}

.stats-cards-container h3, .report-container h3 {
  font-size: 18px;
  margin-bottom: 16px;
}

.action-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
}

.stat-card {
  padding: 20px;
  text-align: center;
}

.stat-number {
  font-size: 28px;
  font-weight: bold;
  color: #2c3e50;
  margin-bottom: 5px;
}

.stat-label {
  font-size: 14px;
  color: #666;
}

.report-meta {
  font-size: 12px;
  color: #777;
  margin-bottom: 20px;
}
.report-meta span {
  margin-right: 16px;
}

.report-section {
  margin-top: 24px;
}

.report-section:first-child {
    margin-top: 0;
}

.report-section h4 {
  font-size: 16px;
  color: #27ae60;
  border-bottom: 1px solid #e0e0e0;
  padding-bottom: 8px;
  margin-bottom: 16px;
}

.overview-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 12px;
    font-size: 14px;
}
.overview-grid p {
    margin: 0;
    padding: 10px;
    background-color: #f9f9f9;
    border-radius: 4px;
}

.table-container {
    overflow-x: auto;
}

.report-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.report-table th, .report-table td {
  border: 1px solid #ddd;
  padding: 10px;
  text-align: left;
}

.report-table th {
  background-color: #f2f2f2;
  font-weight: bold;
}

.report-table tr:nth-child(even) {
  background-color: #f9f9f9;
}
</style>