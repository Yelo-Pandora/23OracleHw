<template>
  <DashboardLayout>
    <div class="report-container">
      <div class="page-header">
        <h1>商户信息统计报表</h1>
        <p>按不同维度生成商户、店铺及区域的统计信息。</p>
      </div>

      <div class="controls-card">
        <div class="form-group">
          <label for="dimension-select">选择统计维度</label>
          <select id="dimension-select" v-model="selectedDimension">
            <option value="all">完整报表</option>
            <option value="type">按租户类型</option>
            <option value="area">按店面区域</option>
            <option value="status">按店面状态</option>
          </select>
        </div>
        <button class="btn-primary" @click="fetchReport" :disabled="loading">
          {{ loading ? '正在生成...' : '生成报表' }}
        </button>
      </div>

      <div v-if="loading" class="status-card">正在加载报表数据...</div>
      <div v-if="error" class="status-card error">{{ error }}</div>

      <!-- 基础统计卡片 -->
      <div v-if="basicStats" class="stats-grid">
        <div class="stat-item">
          <h3>{{ basicStats.totalStores }}</h3>
          <p>总店铺数</p>
        </div>
        <div class="stat-item">
          <h3>{{ basicStats.activeStores }}</h3>
          <p>正常营业</p>
        </div>
        <div class="stat-item">
          <h3>{{ basicStats.vacantAreas }}</h3>
          <p>空置区域</p>
        </div>
        <div class="stat-item">
          <h3>{{ basicStats.occupancyRate }}%</h3>
          <p>入住率</p>
        </div>
      </div>

      <!-- 报表结果展示 -->
      <div v-if="reportData" class="report-content-card">
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
import DashboardLayout from '@/components/BoardLayout.vue';

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
:root {
  --primary-color: #1abc9c;
  --secondary-color: #7f8c8d;
  --card-bg: #ffffff;
  --card-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  --border-radius: 12px;
  --input-border-color: #dee2e6;
}

.report-container {
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
  color: var(--secondary-color);
}

.controls-card, .status-card, .report-content-card {
  background-color: var(--card-bg);
  border-radius: var(--border-radius);
  box-shadow: var(--card-shadow);
  padding: 24px;
}

.controls-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 20px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.form-group label {
  font-weight: 500;
  font-size: 14px;
}

.form-group select {
  padding: 10px 12px;
  border: 1px solid var(--input-border-color);
  border-radius: 8px;
  min-width: 250px;
}

.btn-primary {
  padding: 10px 20px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
  background-color: #1abc9c;
  color: white;
  transition: background-color 0.2s, transform 0.2s;
}
.btn-primary:hover:not(:disabled) { 
  background-color: #16a085;
  transform: translateY(-2px);
}
.btn-primary:disabled { 
  background-color: #a3e9a4; 
  cursor: not-allowed; 
}

.status-card { text-align: center; font-size: 16px; }
.status-card.error { color: #e74c3c; background-color: #fbeae5; }

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 20px;
  margin-bottom: 24px;
}

.stat-item {
  background: #f9f9f9;
  padding: 20px;
  border-radius: 8px;
  text-align: center;
  border-left: 4px solid var(--primary-color);
}
.stat-item h3 { margin: 0 0 8px 0; font-size: 28px; font-weight: 600; color: #333; }
.stat-item p { margin: 0; color: var(--secondary-color); }

.report-content-card h3 { font-size: 20px; margin-bottom: 8px; }
.report-meta { font-size: 12px; color: #777; margin-bottom: 20px; }
.report-meta span { margin-right: 16px; }

.report-section {
  border-top: 1px solid #eee;
  padding-top: 20px;
  margin-top: 20px;
}
.report-section:first-child { margin-top: 0; border-top: none; padding-top: 0; }
.report-section h4 { font-size: 18px; margin-bottom: 12px; color: #333; }

.overview-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 12px;
  font-size: 14px;
}
.overview-grid p { margin: 0; padding: 10px; background-color: #f9f9f9; border-radius: 4px; }

.table-container { overflow-x: auto; }
.report-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.report-table th, .report-table td { border: 1px solid #ddd; padding: 12px; text-align: left; }
.report-table th { background-color: #f7f7f7; font-weight: 600; }
.report-table tr:nth-child(even) { background-color: #fcfcfc; }
</style>
