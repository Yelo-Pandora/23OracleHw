<template>
  <DashboardLayout>
    <div class="rent-collection-page">
      <h2>租金管理</h2>
      <p class="subtitle">生成、查询并管理商户的月度租金账单。</p>

      <!-- 功能区 -->
      <div class="action-container">
        <!-- 生成账单 -->
        <div class="action-card card">
          <h3>生成月度账单</h3>
          <p>为指定月份（格式 YYYYMM）的所有商户生成租金账单。</p>
          <div class="form-group">
            <label for="billing-period">账单月份:</label>
            <input type="text" id="billing-period" v-model="billingPeriod" placeholder="例如: 202412" />
          </div>
          <button @click="generateBills" :disabled="generateLoading">{{ generateLoading ? '正在生成...' : '确认生成' }}</button>
          <div v-if="generateSuccess" class="success-message">{{ generateSuccess }}</div>
          <div v-if="generateError" class="error-message">{{ generateError }}</div>
        </div>

        <!-- 查询账单 -->
        <div class="action-card card">
          <h3>查询租金账单</h3>
          <p>根据店铺ID、月份或状态筛选租金账单。</p>
          <div class="form-group">
            <label for="query-store-id">店铺ID:</label>
            <input type="number" id="query-store-id" v-model.number="query.storeId" placeholder="可选" />
          </div>
          <div class="form-group">
            <label for="query-period">账单月份:</label>
            <input type="text" id="query-period" v-model="query.billPeriod" placeholder="可选, YYYYMM" />
          </div>
          <div class="form-group">
            <label for="query-status">账单状态:</label>
            <select id="query-status" v-model="query.billStatus">
              <option value="">全部</option>
              <option value="待缴纳">待缴纳</option>
              <option value="已缴纳">已缴纳</option>
              <option value="已确认">已确认</option>
              <option value="逾期">逾期</option>
            </select>
          </div>
          <button @click="getBills" :disabled="listLoading">{{ listLoading ? '正在查询...' : '查询账单' }}</button>
        </div>
      </div>

      <!-- 账单列表 -->
      <div class="list-container card">
        <h3>账单列表</h3>
        <div v-if="listLoading" class="loading-indicator">正在加载...</div>
        <div v-if="listError" class="error-message">{{ listError }}</div>
        <div v-if="!listLoading && bills.length === 0" class="info-message">没有找到符合条件的账单。</div>
        <div v-if="bills.length > 0" class="table-container">
          <table class="report-table">
            <thead>
              <tr>
                <th>店铺名称</th>
                <th>账单月份</th>
                <th>金额</th>
                <th>截止日期</th>
                <th>状态</th>
                <th>支付时间</th>
                <th>财务确认人</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="bill in bills" :key="bill.StoreId + bill.BillPeriod">
                <td>{{ bill.StoreName }} (ID: {{ bill.StoreId }})</td>
                <td>{{ bill.BillPeriod }}</td>
                <td>{{ bill.TotalAmount != null ? '¥' + bill.TotalAmount.toFixed(2) : 'N/A' }}</td>
                <td>{{ new Date(bill.DueDate).toLocaleDateString() }}</td>
                <td><span :class="`status-${bill.BillStatus}`">{{ bill.BillStatus }}</span></td>
                <td>{{ bill.PaymentTime ? new Date(bill.PaymentTime).toLocaleString() : '-' }}</td>
                <td>{{ bill.ConfirmedBy || '-' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue';
import axios from 'axios';
import { useUserStore } from '@/stores/user';
import DashboardLayout from '@/components/BoardLayout.vue';

const userStore = useUserStore();

// 生成账单部分的状态
const billingPeriod = ref('');
const generateLoading = ref(false);
const generateError = ref('');
const generateSuccess = ref('');

// 查询账单部分的状态
const query = reactive({
  storeId: null,
  billPeriod: '',
  billStatus: ''
});

// 列表部分的状态
const bills = ref([]);
const listLoading = ref(false);
const listError = ref('');

const getOperatorAccount = () => {
    return userStore.userInfo?.account || 'admin';
}

// 生成月度账单
const generateBills = async () => {
  if (!billingPeriod.value || !/\d{6}/.test(billingPeriod.value)) {
    generateError.value = '请输入有效的账单月份，格式为 YYYYMM。';
    return;
  }
  generateLoading.value = true;
  generateError.value = '';
  generateSuccess.value = '';
  try {
    const response = await axios.post('/api/Store/GenerateMonthlyRentBills', `"${billingPeriod.value}"`, {
        headers: { 'Content-Type': 'application/json' }
    });
    generateSuccess.value = response.data.message || '账单生成成功！';
    getBills(); // 成功后刷新列表
  } catch (err) {
    generateError.value = err.response?.data?.error || '账单生成失败';
  } finally {
    generateLoading.value = false;
  }
};

// 获取账单列表
const getBills = async () => {
  listLoading.value = true;
  listError.value = '';
  bills.value = [];
  try {
    const payload = {
        operatorAccount: getOperatorAccount(),
        storeId: query.storeId || null,
        billPeriod: query.billPeriod || null,
        billStatus: query.billStatus || null
    };
    const response = await axios.post('/api/Store/GetRentBills', payload);
    bills.value = response.data.bills.map(b => ({ ...b, confirmLoading: false }));
  } catch (err) {
    listError.value = err.response?.data?.error || '获取账单列表失败';
  } finally {
    listLoading.value = false;
  }
};

// 组件加载时自动获取一次列表
onMounted(() => {
  getBills();
});

</script>

<style scoped>
.rent-collection-page {
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

.action-container {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
    gap: 24px;
}

.action-card h3 {
    font-size: 18px;
    margin-top: 0;
    margin-bottom: 8px;
}

.action-card p {
    font-size: 14px;
    color: #666;
    margin-bottom: 16px;
    min-height: 40px;
}

.form-group {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
}

.form-group label {
  font-weight: bold;
  color: #555;
  width: 80px;
}

.form-group input,
.form-group select {
  width: 100%;
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  flex: 1;
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

.confirm-btn {
    background-color: #27ae60;
}
.confirm-btn:hover {
    background-color: #219a52;
}

.loading-indicator, .info-message {
  text-align: center;
  padding: 20px;
  font-size: 16px;
  color: #666;
}

.error-message, .success-message {
  margin-top: 12px;
  padding: 10px;
  border-radius: 4px;
  font-size: 14px;
}

.error-message {
  color: #c00;
  background-color: #fbeae5;
}

.success-message {
  color: #219a52;
  background-color: #e6f7f4;
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
  white-space: nowrap;
}

.report-table th {
  background-color: #f2f2f2;
  font-weight: bold;
}

.report-table tr:nth-child(even) {
  background-color: #f9f9f9;
}

.status-待缴纳 { color: #e67e22; font-weight: bold; }
.status-已缴纳 { color: #27ae60; font-weight: bold; }
.status-已确认 { color: #2980b9; font-weight: bold; }
.status-逾期 { color: #c0392b; font-weight: bold; }

</style>
