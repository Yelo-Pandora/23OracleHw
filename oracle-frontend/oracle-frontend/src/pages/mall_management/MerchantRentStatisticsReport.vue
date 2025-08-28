<template>
  <DashboardLayout>
    <div class="rent-statistics-report">
      <h2>商户租金统计报表</h2>
      <p class="subtitle">分析商户租金收缴情况、欠款明细及历史趋势。</p>

      <!-- 控制区域 -->
      <div class="controls-container card">
        <div class="form-group">
          <label for="period-input">统计时间 (YYYYMM):</label>
          <input type="text" id="period-input" v-model="reportPeriod" placeholder="例如: 202412" />
        </div>
        <div class="form-group">
          <label for="dimension-select">统计维度:</label>
          <select id="dimension-select" v-model="selectedDimension">
            <option value="all">综合分析</option>
            <option value="time">时间维度</option>
            <option value="area">区域维度</option>
          </select>
        </div>
        <button @click="fetchRentStatisticsReport" :disabled="loading">
          {{ loading ? '正在生成...' : '生成统计报表' }}
        </button>
      </div>

      <div v-if="loading" class="loading-indicator card">正在加载报表数据...</div>
      <div v-if="error" class="error-message card">{{ error }}</div>

      <!-- 报表结果展示 -->
      <div v-if="reportData" class="report-container card">
        <h3 class="report-main-title">{{ reportData.title }}</h3>

        <!-- 综合分析 (all) -->
        <div v-if="selectedDimension === 'all' && reportData.executiveSummary">
          <div class="stats-overview-container">
            <div class="stats-grid">
              <div class="stats-card card-primary"><h3>{{ reportData.executiveSummary.totalStores }}</h3><small>商铺总数</small></div>
              <div class="stats-card card-success"><h3>¥{{ reportData.executiveSummary.totalRevenue.toLocaleString() }}</h3><small>租金总收入</small></div>
              <div class="stats-card card-warning"><h3>{{ reportData.executiveSummary.collectionRate }}%</h3><small>收缴率</small></div>
              <div class="stats-card card-danger"><h3>{{ reportData.executiveSummary.riskLevel }}</h3><small>风险等级</small></div>
            </div>
          </div>
          <div class="report-section">
            <h4 class="report-title">财务汇总</h4>
            <p><strong>应收租金:</strong> ¥{{ reportData.financialSummary.totalAmount.toLocaleString() }}</p>
            <p><strong>已收金额:</strong> ¥{{ reportData.financialSummary.collectedAmount.toLocaleString() }}</p>
          </div>
          <div class="report-section">
            <h4 class="report-title">运营指标</h4>
            <p><strong>总账单数:</strong> {{ reportData.operationalMetrics.totalBills }}</p>
            <p><strong>逾期账单:</strong> {{ reportData.operationalMetrics.overdueBills }}</p>
          </div>
          <div class="report-section" v-if="reportData.recommendations">
            <h4 class="report-title">洞察建议</h4>
            <ul><li v-for="(rec, index) in reportData.recommendations" :key="index">{{ rec }}</li></ul>
          </div>
        </div>

        <!-- 时间维度 (time) -->
        <div v-if="selectedDimension === 'time' && reportData.summary">
          <div class="report-section">
            <h4 class="report-title">收缴总览</h4>
            <p><strong>总账单:</strong> {{ reportData.summary.totalBills }}</p>
            <p><strong>已缴账单:</strong> {{ reportData.summary.paidBills }}</p>
            <p><strong>逾期账单:</strong> {{ reportData.summary.overdueBills }}</p>
            <p><strong>收缴率:</strong> {{ reportData.summary.collectionRate }}%</p>
          </div>
          <div class="report-section" v-if="reportData.insights">
            <h4 class="report-title">数据洞察</h4>
            <ul><li v-for="(insight, index) in reportData.insights" :key="index">{{ insight }}</li></ul>
          </div>
        </div>

        <!-- 区域维度 (area) -->
        <div v-if="selectedDimension === 'area' && reportData.areaDetails">
          <div class="report-section">
            <h4 class="report-title">区域总览</h4>
            <p><strong>总区域数:</strong> {{ reportData.summary.totalAreas }}</p>
            <p><strong>已租用区域:</strong> {{ reportData.summary.occupiedAreas }}</p>
            <p><strong>平均租金:</strong> ¥{{ reportData.summary.avgRentPerArea.toLocaleString() }}</p>
          </div>
          <div class="report-section table-container">
            <h4 class="report-title">各区域详情</h4>
            <table class="report-table">
              <thead>
                <tr><th>区域ID</th><th>店铺名称</th><th>基础租金</th><th>收缴状态</th><th>平米租金</th></tr>
              </thead>
              <tbody>
                <tr v-for="area in reportData.areaDetails" :key="area.areaId">
                  <td>{{ area.areaId }}</td>
                  <td>{{ area.storeName || '-' }}</td>
                  <td>¥{{ area.baseRent.toLocaleString() }}</td>
                  <td>{{ area.collectionStatus }}</td>
                  <td>¥{{ area.rentPerSqm.toLocaleString() }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup>
import { ref } from 'vue';
import axios from 'axios';
import { useUserStore } from '@/stores/user';
import DashboardLayout from '@/components/BoardLayout.vue';

const userStore = useUserStore();
const API_BASE_URL = '/api/Store';

const reportPeriod = ref('202412');
const selectedDimension = ref('all');
const loading = ref(false);
const error = ref(null);
const reportData = ref(null);

const fetchRentStatisticsReport = async () => {
  if (!userStore.userInfo || !userStore.userInfo.account) {
    error.value = '无法获取用户信息，请重新登录。';
    return;
  }
  if (!reportPeriod.value || reportPeriod.value.length !== 6) {
    error.value = '时间段格式错误，应为YYYYMM格式。';
    return;
  }

  loading.value = true;
  error.value = null;
  reportData.value = null;

  try {
    const params = {
      period: reportPeriod.value,
      dimension: selectedDimension.value,
      operatorAccount: userStore.userInfo.account,
    };
    const response = await axios.get(`${API_BASE_URL}/RentStatisticsReport`, { params });
    if (response.data && response.data.report) {
      reportData.value = response.data.report;
    } else {
      throw new Error("返回的报表数据格式不正确或为空");
    }
  } catch (err) {
    console.error('Failed to fetch rent statistics report:', err);
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
</script>

<style scoped>
.rent-statistics-report {
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
input, select {
  width: 200px;
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}
button {
  background-color: #e74c3c;
  color: white;
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
}
button:hover {
  background-color: #c0392b;
}
button:disabled {
  background-color: #f5b7b1;
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
.stats-overview-container {
  margin-bottom: 24px;
}
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 15px;
}
.stats-card {
  text-align: center;
  padding: 20px;
  border-radius: 8px;
  color: white;
  font-weight: bold;
}
.stats-card h3 {
  margin: 0;
  font-size: 24px;
}
.stats-card small {
  font-size: 12px;
  opacity: 0.9;
}
.card-primary { background: linear-gradient(135deg, #3498db, #2980b9); }
.card-success { background: linear-gradient(135deg, #27ae60, #229954); }
.card-danger { background: linear-gradient(135deg, #e74c3c, #c0392b); }
.card-warning { background: linear-gradient(135deg, #f39c12, #e67e22); }
.report-main-title {
    font-size: 20px;
    color: #333;
    margin-bottom: 16px;
}
.report-section {
  border-bottom: 1px solid #eee;
  padding-bottom: 15px;
  margin-bottom: 15px;
}
.report-section:last-child {
  border-bottom: none;
}
.report-title {
  color: #e74c3c;
  font-size: 18px;
  font-weight: bold;
  margin-bottom: 10px;
}
.table-container {
    overflow-x: auto;
}
.report-table {
    width: 100%;
    border-collapse: collapse;
}
.report-table th, .report-table td {
    border: 1px solid #ddd;
    padding: 8px;
    text-align: left;
}
.report-table th {
    background-color: #f7f7f7;
}
</style>
