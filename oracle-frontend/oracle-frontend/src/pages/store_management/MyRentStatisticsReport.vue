<template>
  <DashboardLayout>
    <div class="page-container">
      <div class="page-header">
        <h1>我的租金统计</h1>
        <p>查看您所有商铺的租金账单和统计信息。</p>
      </div>

      <div v-if="loading" class="status-card">正在加载您的租金数据...</div>
      <div v-if="error" class="status-card error">{{ error }}</div>

      <div v-if="rentData" class="report-content">
        <!-- 租金概览 -->
        <div class="stats-grid">
          <div class="stat-item">
            <h3>{{ rentData.totalBills }}</h3>
            <p>账单总数</p>
          </div>
          <div class="stat-item">
            <h3>¥{{ totalPaid.toLocaleString() }}</h3>
            <p>已付总额</p>
          </div>
          <div class="stat-item">
            <h3>{{ overdueBills }}</h3>
            <p>逾期账单</p>
          </div>
          <div class="stat-item">
            <h3>¥{{ totalDue.toLocaleString() }}</h3>
            <p>待付总额</p>
          </div>
        </div>

        <!-- 账单明细 -->
        <div class="content-card">
          <h4>租金账单明细</h4>
          <div class="table-container">
            <table class="bill-table">
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
                  <td>¥{{ bill.TotalAmount.toLocaleString() }}</td>
                  <td>
                    <span class="status-tag" :class="getStatusClass(bill.billStatus)">
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
    .reduce((sum, b) => sum + b.TotalAmount, 0);
});

const totalDue = computed(() => {
  if (!rentData.value) return 0;
  return rentData.value.bills
    .filter(b => b.billStatus !== '已缴纳')
    .reduce((sum, b) => sum + b.TotalAmount, 0);
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
:root {
  --primary-color: #1abc9c;
  --success-color: #2ecc71;
  --warning-color: #f39c12;
  --danger-color: #e74c3c;
  --card-bg: #ffffff;
  --card-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  --border-radius: 12px;
}

.page-container {
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
  color: #7f8c8d;
}

.status-card {
  text-align: center;
  padding: 40px 20px;
  font-size: 16px;
  color: #666;
  background-color: #f9f9f9;
  border-radius: 8px;
}
.status-card.error {
  color: var(--danger-color);
  background-color: #fbeae5;
}

.report-content {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 20px;
}

.stat-item {
  background: var(--card-bg);
  padding: 20px;
  border-radius: var(--border-radius);
  text-align: center;
  box-shadow: var(--card-shadow);
}
.stat-item h3 { margin: 0 0 8px 0; font-size: 28px; font-weight: 600; color: var(--primary-color); }
.stat-item p { margin: 0; color: #7f8c8d; }

.content-card {
  background-color: var(--card-bg);
  border-radius: var(--border-radius);
  box-shadow: var(--card-shadow);
  padding: 24px;
}
.content-card h4 { font-size: 18px; margin-bottom: 16px; }

.table-container { overflow-x: auto; }
.bill-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}
.bill-table th, .bill-table td {
  border-bottom: 1px solid #eee;
  padding: 16px;
  text-align: left;
  vertical-align: middle;
}
.bill-table th {
  background-color: #f8f9fa;
  font-weight: 600;
  color: #555;
}

.status-tag {
  padding: 4px 10px;
  border-radius: 12px;
  font-weight: 500;
  font-size: 12px;
  color: white;
}
.status-paid { background-color: var(--success-color); }
.status-pending { background-color: var(--warning-color); }
.status-overdue { background-color: var(--danger-color); }
</style>
