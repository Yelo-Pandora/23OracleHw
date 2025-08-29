<template>
  <div class="collaboration-report">
    <h2>合作方统计报表</h2>

    <div class="report-filters">
      <div class="filter-group">
        <label>开始日期:</label>
        <input type="date" v-model="filters.startDate" :max="filters.endDate || maxDate">
      </div>

      <div class="filter-group">
        <label>结束日期:</label>
        <input type="date" v-model="filters.endDate" :min="filters.startDate" :max="maxDate">
      </div>

      <div class="filter-group">
        <label>合作方名称:</label>
        <input
          type="text"
          v-model="filters.collaborationName"
          placeholder="请输入合作方名称"
        />
      </div>

      <button @click="generateReport" class="btn-generate">生成报表</button>
      <button @click="exportReport" class="btn-export" :disabled="!reportData.length">导出报表</button>
    </div>

    <div v-if="loading" class="loading">生成报表中...</div>

    <div v-else-if="reportData.length === 0" class="no-data">
      <p>暂无数据，请选择日期范围后生成报表</p>
    </div>

    <div v-else class="report-results">
      <h3>统计结果</h3>

      <div class="summary-cards">
        <div class="summary-card">
          <div class="card-title">合作方总数</div>
          <div class="card-value">{{ summary.totalCollaborations }}</div>
        </div>

        <div class="summary-card">
          <div class="card-title">活动总数</div>
          <div class="card-value">{{ summary.totalEvents }}</div>
        </div>

        <div class="summary-card">
          <div class="card-title">总投资金额</div>
          <div class="card-value">¥{{ summary.totalInvestment.toLocaleString() }}</div>
        </div>

        <div class="summary-card">
          <div class="card-title">平均活动收益</div>
          <div class="card-value">¥{{ summary.avgRevenue.toLocaleString() }}</div>
        </div>
      </div>

      <table class="report-table">
        <thead>
          <tr>
            <th>合作方ID</th>
            <th>活动次数</th>
            <th>总投资金额</th>
            <th>平均活动收益</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in reportData" :key="item.CollaborationId">
            <td>{{ item.CollaborationId }}</td>
            <td>{{ item.EventCount }}</td>
            <td>¥{{ item.TotalInvestment.toLocaleString() }}</td>
            <td>¥{{ item.AvgRevenue.toLocaleString() }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref, computed, onMounted } from 'vue';
import { useUserStore } from '@/user/user';
import { useRouter } from 'vue-router';
import axios from 'axios';

const userStore = useUserStore();
const router = useRouter();

const filters = reactive({
  startDate: '',
  endDate: '',
  industry: ''
});

const reportData = ref([]);
const loading = ref(false);
const maxDate = new Date().toISOString().split('T')[0]; // 当前日期

// 检查登录状态
const checkAuth = () => {
  if (!userStore.token) {
    router.push('/login');
    return false;
  }
  return true;
};

const summary = computed(() => {
  if (reportData.value.length === 0) {
    return {
      totalCollaborations: 0,
      totalEvents: 0,
      totalInvestment: 0,
      avgRevenue: 0
    };
  }

  return {
    totalCollaborations: reportData.value.length,
    totalEvents: reportData.value.reduce((sum, item) => sum + item.EventCount, 0),
    totalInvestment: reportData.value.reduce((sum, item) => sum + item.TotalInvestment, 0),
    avgRevenue: reportData.value.reduce((sum, item) => sum + item.AvgRevenue, 0) / reportData.value.length
  };
});

const generateReport = async () => {
  if (!checkAuth()) return;

  // 验证日期
  if (!filters.startDate || !filters.endDate) {
    alert('请选择开始日期和结束日期');
    return;
  }

  if (filters.startDate > filters.endDate) {
    alert('开始日期不能晚于结束日期');
    return;
  }

  if (filters.endDate > maxDate) {
    alert('结束日期不能晚于当前日期');
    return;
  }

  loading.value = true;

  try {
    const params = {
      operatorAccountId: userStore.token,
      startDate: filters.startDate,
      endDate: filters.endDate
    };

    if (filters.industry) {
      params.industry = filters.industry;
    }

    const response = await axios.get('/api/Collaboration/report', { params });
    reportData.value = response.data;
  } catch (error) {
    console.error('生成报表失败:', error);
    if (error.response && error.response.status === 401) {
      alert('登录已过期，请重新登录');
      userStore.logout();
      router.push('/login');
    } else {
      alert('生成报表失败，请稍后重试');
    }
  } finally {
    loading.value = false;
  }
};

const exportReport = () => {
  // 这里实现导出功能，可以导出为Excel或PDF
  // 简单实现：导出为CSV
  const csvContent = [
    ['合作方ID', '活动次数', '总投资金额', '平均活动收益'],
    ...reportData.value.map(item => [
      item.CollaborationId,
      item.EventCount,
      item.TotalInvestment,
      item.AvgRevenue
    ])
  ].map(row => row.join(',')).join('\n');

  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  const url = URL.createObjectURL(blob);

  link.setAttribute('href', url);
  link.setAttribute('download', `合作方统计报表_${filters.startDate}_至_${filters.endDate}.csv`);
  link.style.visibility = 'hidden';

  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
};

// 组件挂载时检查登录状态
onMounted(() => {
  checkAuth();
});
</script>

<style scoped>
.collaboration-report {
  max-width: 1000px;
  margin: 0 auto;
}

.report-filters {
  display: flex;
  flex-wrap: wrap;
  gap: 15px;
  margin-bottom: 30px;
  padding: 20px;
  background-color: #f8f9fa;
  border-radius: 8px;
}

.filter-group {
  display: flex;
  flex-direction: column;
}

.filter-group label {
  margin-bottom: 5px;
  font-weight: bold;
}

.filter-group input,
.filter-group select {
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.btn-generate, .btn-export {
  padding: 8px 15px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  align-self: flex-end;
}

.btn-generate {
  background-color: #007bff;
  color: white;
}

.btn-export {
  background-color: #28a745;
  color: white;
}

.btn-export:disabled {
  background-color: #6c757d;
  cursor: not-allowed;
}

.summary-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.summary-card {
  padding: 20px;
  background-color: #f8f9fa;
  border-radius: 8px;
  text-align: center;
  box-shadow: 0 2px 5px rgba(0,0,0,0.1);
}

.card-title {
  font-size: 14px;
  color: #6c757d;
  margin-bottom: 10px;
}

.card-value {
  font-size: 24px;
  font-weight: bold;
  color: #007bff;
}

.report-table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 20px;
}

.report-table th,
.report-table td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #ddd;
}

.report-table th {
  background-color: #f8f9fa;
  font-weight: bold;
}

.loading, .no-data {
  text-align: center;
  padding: 40px;
  font-size: 18px;
  color: #6c757d;
}
</style>
