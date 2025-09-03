<template>
  <DashboardLayout>
    <div class="page-container">
      <div class="page-header">
        <h1>租金管理</h1>
        <p>生成、查询并管理商户的月度租金账单。</p>
      </div>

      <!-- 功能区 -->
      <div class="action-grid">
        <!-- 生成账单 -->
        <div class="form-card">
          <h3>生成月度账单</h3>
          <p>为指定月份（格式 YYYYMM）的所有商户生成租金账单。</p>
          <div class="form-group">
            <label for="billing-period">账单月份</label>
            <input type="text" id="billing-period" v-model="billingPeriod" placeholder="例如: 202412" />
          </div>
          <button class="btn-primary" @click="generateBills" :disabled="generateLoading">{{ generateLoading ? '正在生成...' : '确认生成' }}</button>
          <div v-if="generateSuccess" class="form-message success">{{ generateSuccess }}</div>
          <div v-if="generateError" class="form-message error">{{ generateError }}</div>
        </div>

        <!-- 查询账单 -->
        <div class="form-card">
          <h3>查询租金账单</h3>
          <p>根据店铺ID、月份或状态筛选租金账单。</p>
          <div class="form-group">
            <label for="query-store-id">店铺ID</label>
            <input type="number" id="query-store-id" v-model.number="query.storeId" placeholder="可选" />
          </div>
          <div class="form-group">
            <label for="query-period">账单月份</label>
            <input type="text" id="query-period" v-model="query.billPeriod" placeholder="可选, YYYYMM" />
          </div>
          <div class="form-group">
            <label for="query-status">账单状态</label>
            <select id="query-status" v-model="query.billStatus">
              <option value="">全部</option>
              <option value="待缴纳">待缴纳</option>
              <option value="已缴纳">已缴纳</option>
              <option value="已确认">已确认</option>
              <option value="逾期">逾期</option>
            </select>
          </div>
          <button class="btn-primary" @click="getBills" :disabled="listLoading">{{ listLoading ? '正在查询...' : '查询账单' }}</button>
        </div>
      </div>

      <!-- 账单列表 -->
      <div class="content-card">
        <h3>账单列表</h3>
        <div v-if="listLoading" class="status-card">正在加载...</div>
        <div v-if="listError" class="status-card error">{{ listError }}</div>
        <div v-if="!listLoading && bills.length === 0" class="status-card">没有找到符合条件的账单。</div>
        <div v-if="bills.length > 0" class="table-container">
          <table class="bill-table">
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
                <td><span class="status-tag" :class="`status-${bill.BillStatus}`">{{ bill.BillStatus }}</span></td>
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
:root {
  --primary-color: #1abc9c;
  --card-bg: #ffffff;
  --card-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  --border-radius: 12px;
  --input-border-color: #dee2e6;
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

.action-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
  gap: 24px;
}

.form-card, .content-card {
  background-color: var(--card-bg);
  border-radius: var(--border-radius);
  box-shadow: var(--card-shadow);
  padding: 24px;
}

.form-card h3, .content-card h3 {
  font-size: 18px;
  margin-top: 0;
  margin-bottom: 8px;
}

.form-card p {
  font-size: 14px;
  color: #666;
  margin-bottom: 16px;
  min-height: 40px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 16px;
}

.form-group label {
  font-weight: 500;
  font-size: 14px;
}

.form-group input, .form-group select {
  padding: 10px 12px;
  border: 1px solid var(--input-border-color);
  border-radius: 8px;
}

.btn-primary {
  padding: 10px 20px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
  background-color: var(--primary-color);
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

.status-card {
  text-align: center;
  padding: 40px 20px;
  font-size: 16px;
  color: #666;
  background-color: #f9f9f9;
  border-radius: 8px;
}
.status-card.error {
  color: #e74c3c;
  background-color: #fbeae5;
}

.form-message {
  margin-top: 16px;
  padding: 12px;
  border-radius: 8px;
  font-size: 14px;
}
.form-message.error { color: #e74c3c; background-color: #fbeae5; }
.form-message.success { color: #27ae60; background-color: #e8f8f5; }

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
.status-待缴纳 { background-color: #f39c12; }
.status-已缴纳 { background-color: #2ecc71; }
.status-已确认 { background-color: #3498db; }
.status-逾期 { background-color: #e74c3c; }
</style>
