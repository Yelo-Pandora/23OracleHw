<template>
  <DashboardLayout>
    <div class="my-rent-report">
      <h2>我的租金统计</h2>
      <p class="subtitle">查看您所有商铺的租金账单和统计信息。</p>

      <div v-if="loading" class="loading-indicator card">正在加载您的租金数据...</div>
      <div v-if="error" class="error-message card">{{ error }}</div>

      <div v-if="rentData" class="report-container">
        <!-- 租金概览 -->
        <div class="stats-overview-container">
          <div class="stats-grid">
            <div class="stats-card card-primary">
              <h3>{{ rentData.totalBills }}</h3>
              <small>账单总数</small>
            </div>
            <div class="stats-card card-success">
              <h3>¥{{ totalPaid.toLocaleString() }}</h3>
              <small>已付总额</small>
            </div>
            <div class="stats-card card-danger">
              <h3>{{ overdueBills }}</h3>
              <small>逾期账单</small>
            </div>
            <div class="stats-card card-warning">
              <h3>¥{{ totalDue.toLocaleString() }}</h3>
              <small>待付总额</small>
            </div>
          </div>
        </div>

        <!-- 账单明细 -->
        <div class="rent-details card">
          <h4 class="report-title">租金账单明细</h4>
          <div class="table-container">
            <table class="report-table">
              <thead>
                <tr>
                  <th>账单周期</th>
                  <th>商铺名称</th>
                  <th>总金额</th>
                  <th>账单状态</th>
                  <th>到期日</th>
                  <th>支付日期</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="bill in rentData.bills" :key="bill.billId">
                  <td>{{ bill.billPeriod }}</td>
                  <td>{{ bill.storeName }}</td>
                  <td>¥{{ bill.totalAmount.toLocaleString() }}</td>
                  <td>
                    <span :class="['status-badge', getStatusClass(bill.billStatus)]">
                      {{ bill.billStatus }}
                    </span>
                  </td>
                  <td>{{ new Date(bill.dueDate).toLocaleDateString() }}</td>
                  <td>{{ bill.paymentDate ? new Date(bill.paymentDate).toLocaleDateString() : '-' }}</td>
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
import { ref, onMounted, computed } from 'vue';
import axios from 'axios';
import { useUserStore } from '@/stores/user';
import DashboardLayout from '@/components/BoardLayout.vue';

const userStore = useUserStore();
const API_BASE_URL = '/api/Store';

const loading = ref(false);
const error = ref(null);
const rentData = ref(null);

const fetchMyRentBills = async () => {
  if (!userStore.userInfo || !userStore.userInfo.account) {
    error.value = '无法获取用户信息，请重新登录。';
    return;
  }

  loading.value = true;
  error.value = null;
  rentData.value = null;

  try {
    const params = {
      merchantAccount: userStore.userInfo.account,
    };
    const response = await axios.get(`${API_BASE_URL}/GetMyRentBills`, { params });
    if (response.data && response.data.bills) {
      rentData.value = response.data;
    } else {
      throw new Error("返回的租金数据格式不正确或为空");
    }
  } catch (err) {
    console.error('Failed to fetch my rent bills:', err);
    if (err.response) {
      error.value = `获取租金数据失败: ${err.response.data.error || err.response.statusText}`;
    } else if (err.request) {
      error.value = '无法连接到服务器，请检查API是否正在运行。';
    } else {
      error.value = `请求失败: ${err.message}`;
    }
  } finally {
    loading.value = false;
  }
};

const totalPaid = computed(() => {
  if (!rentData.value) return 0;
  return rentData.value.bills
    .filter(b => b.billStatus === '已缴纳')
    .reduce((sum, b) => sum + b.totalAmount, 0);
});

const totalDue = computed(() => {
  if (!rentData.value) return 0;
  return rentData.value.bills
    .filter(b => b.billStatus !== '已缴纳')
    .reduce((sum, b) => sum + b.totalAmount, 0);
});

const overdueBills = computed(() => {
  if (!rentData.value) return 0;
  return rentData.value.bills.filter(b => b.billStatus === '逾期').length;
});

const getStatusClass = (status) => {
  switch (status) {
    case '已缴纳': return 'status-paid';
    case '待缴纳': return 'status-pending';
    case '逾期': return 'status-overdue';
    default: return '';
  }
};

onMounted(() => {
  fetchMyRentBills();
});
</script>

<style scoped>
.my-rent-report {
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
.report-title {
  color: #333;
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
.status-badge {
  padding: 3px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: bold;
  color: white;
}
.status-paid { background-color: #27ae60; }
.status-pending { background-color: #f39c12; }
.status-overdue { background-color: #e74c3c; }
</style>
