<template>
  <DashboardLayout>
    <div class="report-container">
      <div class="page-header">
        <h1>商户租金统计报表</h1>
        <p>分析商户租金收缴情况、欠款明细及历史趋势。</p>
      </div>

      <!-- 控制区域 -->
      <div class="controls-card">
        <div class="form-row">
          <div class="form-group">
            <label for="start-period-input">开始时间</label>
            <input type="month" id="start-period-input" v-model="startPeriod" />
          </div>
          <div class="form-group">
            <label for="end-period-input">结束时间</label>
            <input type="month" id="end-period-input" v-model="endPeriod" />
          </div>
          <div class="form-group">
            <label for="dimension-select">统计维度</label>
            <select id="dimension-select" v-model="selectedDimension">
              <option value="all">综合分析</option>
              <option value="time">时间维度</option>
              <option value="area">区域维度</option>
            </select>
          </div>
        </div>
        <div class="button-group">
          <button class="btn-primary" @click="fetchRentStatisticsReport" :disabled="loading">
            {{ loading ? '生成中...' : '生成报表' }}
          </button>
          <button class="btn-secondary" @click="showLogs = !showLogs">
            {{ showLogs ? '隐藏日志' : '显示日志' }}
          </button>
          <button class="btn-success" @click="exportReportToPDF" :disabled="!reportData">
            导出PDF
          </button>
        </div>
      </div>

      <div v-if="showLogs" class="logs-card">
        <h4>调试日志</h4>
        <div class="log-list">
          <div v-for="(l, i) in logs" :key="i" class="log-line">{{ l }}</div>
        </div>
      </div>

      <div v-if="loading" class="status-card">正在加载报表数据...</div>
      <div v-if="error" class="status-card error">{{ error }}</div>

      <!-- 报表结果展示 -->
      <div v-if="reportData" id="reportToExport" class="report-content-card">
        <h3 class="report-main-title">{{ reportData.title }}</h3>

        <!-- 数据可视化 -->
        <div class="visualization-section" v-if="selectedDimension === 'all' && reportData.operationalMetrics">
          <div class="chart-controls">
            <h4>数据可视化</h4>
            <select v-model="selectedChartType" @change="renderCharts">
              <option value="pie">租金状态分布 (饼图)</option>
              <option value="bar">收缴情况对比 (柱状图)</option>
              <option value="line">收缴趋势 (折线图)</option>
              <option value="radar">财务指标雷达图</option>
              <option value="polarArea">运营指标概览 (极坐标图)</option>
              <option value="scatter">租金与逾期关系 (散点图)</option>
            </select>
          </div>
          <div class="chart-container">
            <canvas id="rentChart"></canvas>
          </div>
        </div>

        <!-- 综合分析 -->
        <div v-if="selectedDimension === 'all' && reportData.executiveSummary">
          <div class="stats-grid">
            <div class="stat-item"><h3>{{ reportData.executiveSummary.totalStores }}</h3><p>商铺总数</p></div>
            <div class="stat-item"><h3>¥{{ reportData.executiveSummary.totalRevenue.toLocaleString() }}</h3><p>租金总收入</p></div>
            <div class="stat-item"><h3>{{ reportData.executiveSummary.collectionRate }}%</h3><p>收缴率</p></div>
            <div class="stat-item"><h3>{{ reportData.executiveSummary.riskLevel }}</h3><p>风险等级</p></div>
          </div>
          <div class="report-section">
            <h4>财务汇总</h4>
            <p><strong>应收租金:</strong> ¥{{ reportData.financialSummary.totalAmount.toLocaleString() }}</p>
            <p><strong>已收金额:</strong> ¥{{ reportData.financialSummary.collectedAmount.toLocaleString() }}</p>
          </div>
          <div class="report-section">
            <h4>运营指标</h4>
            <p><strong>总账单数:</strong> {{ reportData.operationalMetrics.totalBills }}</p>
            <p><strong>逾期账单:</strong> {{ reportData.operationalMetrics.overdueBills }}</p>
          </div>
          <div class="report-section" v-if="reportData.recommendations">
            <h4>洞察建议</h4>
            <ul><li v-for="(rec, index) in reportData.recommendations" :key="index">{{ rec }}</li></ul>
          </div>
        </div>

        <!-- 时间维度 -->
        <div v-if="selectedDimension === 'time' && reportData.summary">
          <div class="report-section">
            <h4>收缴总览</h4>
            <p><strong>总账单:</strong> {{ reportData.summary.totalBills }}</p>
            <p><strong>已缴账单:</strong> {{ reportData.summary.paidBills }}</p>
            <p><strong>逾期账单:</strong> {{ reportData.summary.overdueBills }}</p>
            <p><strong>收缴率:</strong> {{ reportData.summary.collectionRate }}%</p>
          </div>
          <div class="report-section" v-if="reportData.insights">
            <h4>数据洞察</h4>
            <ul><li v-for="(insight, index) in reportData.insights" :key="index">{{ insight }}</li></ul>
          </div>
        </div>

        <!-- 区域维度 -->
        <div v-if="selectedDimension === 'area' && reportData.areaDetails">
          <div class="report-section">
            <h4>区域总览</h4>
            <p><strong>总区域数:</strong> {{ reportData.summary.totalAreas }}</p>
            <p><strong>已租用区域:</strong> {{ reportData.summary.occupiedAreas }}</p>
            <p><strong>平均租金:</strong> ¥{{ reportData.summary.avgRentPerArea.toLocaleString() }}</p>
          </div>
          <div class="report-section table-container">
            <h4>各区域详情</h4>
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
import { ref, nextTick } from 'vue';
import axios from 'axios';
import { useUserStore } from '@/stores/user';
import { Chart, registerables } from 'chart.js';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';
import DashboardLayout from '@/components/BoardLayout.vue';

Chart.register(...registerables);

const userStore = useUserStore();
const API_BASE_URL = '/api/Store';

const startPeriod = ref('2024-01');
const endPeriod = ref('2024-12');
const selectedDimension = ref('all');
const loading = ref(false);
const error = ref(null);
const reportData = ref(null);
const logs = ref([]);
const showLogs = ref(false);
const pushLog = (level, ...args) => {
  try {
    const text = args.map(a => {
      try { return typeof a === 'string' ? a : JSON.stringify(a); } catch (e) { return String(a); }
    }).join(' ');
    const prefix = `[${new Date().toLocaleTimeString()}] ${level.toUpperCase()}:`;
    logs.value.unshift(prefix + ' ' + text);
    // keep logs length reasonable
    if (logs.value.length > 200) logs.value.pop();
  } catch (e) {
    // ignore logging errors
  }
};
let rentChart = null;
const selectedChartType = ref('pie');

const renderCharts = () => {
  if (rentChart) {
    rentChart.destroy();
  }
  if (selectedDimension.value !== 'all' || !reportData.value) return;

  const ctx = document.getElementById('rentChart').getContext('2d');
  const financial = reportData.value.financialSummary;
  const operational = reportData.value.operationalMetrics;

  let chartConfig;
  switch (selectedChartType.value) {
    case 'pie':
      chartConfig = {
        type: 'pie',
        data: {
          labels: ['已缴账单', '逾期账单', '待缴账单'],
          datasets: [{ data: [operational.paidBills, operational.overdueBills, operational.pendingBills], backgroundColor: ['#4CAF50', '#F44336', '#FFC107'] }]
        },
        options: { plugins: { title: { display: true, text: '租金账单状态分布' } } }
      };
      break;
    case 'bar':
      chartConfig = {
        type: 'bar',
        data: {
          labels: ['应收', '已收', '待收'],
          datasets: [{ label: '金额', data: [financial.totalAmount, financial.collectedAmount, financial.outstandingAmount], backgroundColor: ['#2196F3', '#4CAF50', '#F44336'] }]
        },
        options: { plugins: { title: { display: true, text: '收缴情况对比' } } }
      };
      break;
    case 'line':
       chartConfig = {
        type: 'line',
        data: {
          labels: ['总账单', '已缴账单', '逾期账单', '待缴账单'],
          datasets: [{ label: '数量', data: [operational.totalBills, operational.paidBills, operational.overdueBills, operational.pendingBills], fill: false, borderColor: '#FF5722' }]
        },
        options: { plugins: { title: { display: true, text: '收缴趋势' } } }
      };
      break;
    case 'radar':
      chartConfig = {
        type: 'radar',
        data: {
          labels: ['总收入', '收缴率(%)', '风险等级(%)', '按时缴纳率(%)'],
          datasets: [{ label: '综合指标', data: [financial.totalAmount / 1000, financial.collectionRate, (operational.overdueBills / operational.totalBills) * 100, operational.onTimePaymentRate], backgroundColor: 'rgba(156, 39, 176, 0.2)', borderColor: '#9C27B0' }]
        },
        options: { plugins: { title: { display: true, text: '财务指标雷达图' } } }
      };
      break;
    case 'polarArea':
      chartConfig = {
        type: 'polarArea',
        data: {
          labels: ['总账单', '已缴账单', '逾期账单'],
          datasets: [{ data: [operational.totalBills, operational.paidBills, operational.overdueBills], backgroundColor: ['#00BCD4', '#8BC34A', '#E91E63'] }]
        },
        options: { plugins: { title: { display: true, text: '运营指标概览' } } }
      };
      break;
    case 'scatter':
      chartConfig = {
        type: 'scatter',
        data: {
          datasets: [{ label: '租金与逾期关系', data: [{ x: financial.avgRentPerStore, y: operational.overdueBills }], backgroundColor: '#607D8B' }]
        },
        options: { plugins: { title: { display: true, text: '租金与逾期关系' } }, scales: { x: { title: { display: true, text: '平均租金' } }, y: { title: { display: true, text: '逾期账单数' } } } }
      };
      break;
  }
  
  if (chartConfig) {
    rentChart = new Chart(ctx, chartConfig);
  }
};

const exportReportToPDF = async () => {
  const reportElement = document.getElementById('reportToExport');
  if (!reportElement) return;

  const canvas = await html2canvas(reportElement, {
    scale: 2, 
    useCORS: true,
  });

  const imgData = canvas.toDataURL('image/png');
  const pdf = new jsPDF('p', 'mm', 'a4');
  const pdfWidth = pdf.internal.pageSize.getWidth();
  const pdfHeight = (canvas.height * pdfWidth) / canvas.width;
  
  pdf.addImage(imgData, 'PNG', 0, 0, pdfWidth, pdfHeight);
  pdf.save(`商户租金统计报表-${startPeriod.value}.pdf`);
};

const fetchRentStatisticsReport = async () => {
  logs.value = [];
  const startVal = startPeriod.value;
  const endVal = endPeriod.value;

  pushLog('info', `生成报表: 维度=${selectedDimension.value}, 开始=${startVal}, 结束=${endVal}`);
  if (!userStore.userInfo || !userStore.userInfo.account) {
    error.value = '无法获取用户信息，请重新登录。';
    return;
  }

  const parsedStart = new Date(startVal + '-01');
  const parsedEnd = new Date(endVal + '-01');
  if (isNaN(parsedStart.getTime()) || isNaN(parsedEnd.getTime())) {
    error.value = '无效的日期格式。';
    return;
  }
  if (parsedStart > parsedEnd) {
    error.value = '开始时间不能晚于结束时间。';
    return;
  }

  loading.value = true;
  error.value = null;
  reportData.value = null;

  try {
    const operatorAccount = userStore.userInfo.account;
    const startPeriodFormatted = startVal.replace('-', '');
    const endPeriodFormatted = endVal.replace('-', '');

    if (selectedDimension.value === 'all') {
      // For 'all', fetch trend, basic stats, and combine them
      const trendPromise = axios.get(`${API_BASE_URL}/RentTrendAnalysis`, {
        params: { startPeriod: startPeriodFormatted, endPeriod: endPeriodFormatted, operatorAccount }
      });
      const basicStatsPromise = axios.get(`${API_BASE_URL}/BasicStatistics`);
      
      const [trendResp, basicStatsResp] = await Promise.all([trendPromise, basicStatsPromise]);

      pushLog('debug', `[RentReport] raw trendData: ${JSON.stringify(trendResp.data)}`);
      pushLog('debug', `[RentReport] raw basicStats: ${JSON.stringify(basicStatsResp.data)}`);

      const trendArr = Array.isArray(trendResp.data.trendData) ? trendResp.data.trendData : [];
      const totalStores = basicStatsResp.data.totalStores || 0;
      const totalRevenue = trendArr.reduce((sum, item) => sum + (item.CollectedAmount || 0), 0);
      const totalBills = trendArr.reduce((sum, item) => sum + (item.TotalBills || 0), 0);
      const paidBills = trendArr.reduce((sum, item) => sum + (item.PaidBills || 0), 0);
      const overdueBills = trendArr.reduce((sum, item) => sum + (item.OverdueBills || 0), 0);
      const collectionRate = totalBills > 0 ? ((paidBills / totalBills) * 100).toFixed(2) : "0.00";
      
      reportData.value = {
        title: `${startVal} - ${endVal} 商户租金综合统计报表`,
        executiveSummary: {
          totalStores: totalStores,
          totalRevenue: totalRevenue,
          collectionRate: collectionRate,
          riskLevel: totalBills > 0 ? `${Math.round((overdueBills / totalBills) * 100)}%` : '0%'
        },
        financialSummary: {
          totalAmount: trendArr.reduce((sum, item) => sum + (item.TotalAmount || 0), 0),
          collectedAmount: totalRevenue,
          outstandingAmount: trendArr.reduce((sum, item) => sum + (item.TotalAmount || 0), 0) - totalRevenue,
          collectionRate: collectionRate,
          avgRentPerStore: totalStores > 0 ? Number((totalRevenue / totalStores).toFixed(2)) : 0
        },
        operationalMetrics: {
          totalBills: totalBills,
          paidBills: paidBills,
          overdueBills: overdueBills,
          pendingBills: totalBills - paidBills - overdueBills,
          onTimePaymentRate: collectionRate
        },
        timeAnalysis: { trend: trendArr },
        recommendations: trendResp.data.recommendations || []
      };

    } else if (selectedDimension.value === 'time') {
      // For 'time', fetch only trend data
      const resp = await axios.get(`${API_BASE_URL}/RentTrendAnalysis`, {
        params: { startPeriod: startPeriodFormatted, endPeriod: endPeriodFormatted, operatorAccount }
      });
      pushLog('debug', `[RentReport] raw trendData: ${JSON.stringify(resp.data)}`);
      const trendArr = Array.isArray(resp.data.trendData) ? resp.data.trendData : [];
      const totalBills = trendArr.reduce((sum, item) => sum + (item.TotalBills || 0), 0);
      const paidBills = trendArr.reduce((sum, item) => sum + (item.PaidBills || 0), 0);
      const collectionRate = totalBills > 0 ? ((paidBills / totalBills) * 100).toFixed(2) : "0.00";

      reportData.value = {
        summary: {
          totalBills: totalBills,
          paidBills: paidBills,
          overdueBills: trendArr.reduce((sum, item) => sum + (item.OverdueBills || 0), 0),
          collectionRate: collectionRate
        },
        insights: [resp.data.message || ''],
        trend: trendArr,
      };

    } else if (selectedDimension.value === 'area') {
      // For 'area', call the specific report endpoint
      const resp = await axios.get(`${API_BASE_URL}/RentStatisticsReport`, {
        params: { startPeriod: startPeriodFormatted, endPeriod: endPeriodFormatted, dimension: 'area', operatorAccount }
      });
      pushLog('debug', `[RentReport] raw areaData: ${JSON.stringify(resp.data)}`);
      if (resp.data && resp.data.report) {
        reportData.value = resp.data.report;
      } else {
        throw new Error('区域报表数据格式不正确');
      }
    }

    await nextTick();
    if (selectedDimension.value === 'all') {
      renderCharts();
    }

  } catch (err) {
    console.error('Failed to fetch rent statistics report:', err);
    pushLog('error', 'Failed to fetch rent statistics report:', err && err.message ? err.message : String(err));
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
:root {
  --primary-color: #1abc9c;
  --secondary-color: #7f8c8d;
  --success-color: #2ecc71;
  --danger-color: #e74c3c;
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

.controls-card, .logs-card, .status-card, .report-content-card {
  background-color: var(--card-bg);
  border-radius: var(--border-radius);
  box-shadow: var(--card-shadow);
  padding: 24px;
}

.form-row {
  display: flex;
  flex-wrap: wrap;
  gap: 20px;
  margin-bottom: 20px;
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

.form-group input, .form-group select {
  padding: 10px 12px;
  border: 1px solid var(--input-border-color);
  border-radius: 8px;
  min-width: 220px;
}

.button-group {
  display: flex;
  gap: 12px;
}

.btn-primary, .btn-secondary, .btn-success {
  padding: 10px 20px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
  transition: background-color 0.2s;
}

.btn-primary { background-color: #1abc9c; color: white; }
.btn-primary:hover:not(:disabled) { background-color: #16a085; transform: translateY(-2px); }
.btn-primary:disabled { background-color: #a3e9a4; cursor: not-allowed; }

.btn-secondary { background-color: #ecf0f1; color: #34495e; }
.btn-secondary:hover:not(:disabled) { background-color: #bdc3c7; transform: translateY(-2px); }

.btn-success { background-color: #2ecc71; color: white; }
.btn-success:hover:not(:disabled) { background-color: #27ae60; transform: translateY(-2px); }
.btn-success:disabled { background-color: #a3e9a4; cursor: not-allowed; }

.status-card { text-align: center; font-size: 16px; }
.status-card.error { color: var(--danger-color); background-color: #fbeae5; }

.logs-card h4 { margin-top: 0; }
.log-list { max-height: 200px; overflow-y: auto; background: #f9f9f9; padding: 10px; border-radius: 8px; }

.report-main-title { font-size: 20px; margin-bottom: 16px; }

.visualization-section { margin-bottom: 24px; }
.chart-controls { display: flex; align-items: center; gap: 16px; margin-bottom: 16px; }
.chart-container { position: relative; height: 400px; width: 100%; margin-top: 16px; }

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
}
.stat-item h3 { margin: 0 0 8px 0; font-size: 22px; color: var(--primary-color); }
.stat-item p { margin: 0; color: var(--secondary-color); }

.report-section {
  border-top: 1px solid #eee;
  padding-top: 20px;
  margin-top: 20px;
}
.report-section h4 { font-size: 18px; margin-bottom: 12px; }

.table-container { overflow-x: auto; }
.report-table { width: 100%; border-collapse: collapse; }
.report-table th, .report-table td { border: 1px solid #ddd; padding: 12px; text-align: left; }
.report-table th { background-color: #f7f7f7; font-weight: 600; }
</style>
