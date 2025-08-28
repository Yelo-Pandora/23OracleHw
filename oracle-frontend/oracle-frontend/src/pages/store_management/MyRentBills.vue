<template>
  <DashboardLayout>
    <div class="page-container">
      <div class="page-header">
        <h1>我的租金账单</h1>
        <p>查看并支付您的月度租金账单。</p>
      </div>

      <div class="content-card">
        <div v-if="loading" class="status-card">正在加载账单...</div>
        <div v-if="error" class="status-card error">{{ error }}</div>
        <div v-if="!loading && bills.length === 0" class="status-card">您当前没有租金账单。</div>

        <div v-if="bills.length > 0" class="table-container">
          <table class="bill-table">
            <thead>
              <tr>
                <th>账单月份</th>
                <th>店铺名称</th>
                <th>金额</th>
                <th>截止日期</th>
                <th>状态</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="bill in bills" :key="bill.BillPeriod">
                <td>{{ bill.BillPeriod }}</td>
                <td>{{ bill.StoreName }}</td>
                <td>¥{{ bill.TotalAmount.toFixed(2) }}</td>
                <td>{{ new Date(bill.DueDate).toLocaleDateString() }}</td>
                <td><span class="status-tag" :class="`status-${bill.BillStatus}`">{{ bill.BillStatus }}</span></td>
                <td>
                  <button
                    v-if="bill.BillStatus === '待缴纳' || bill.BillStatus === '逾期'"
                    @click="payBill(bill)"
                    class="btn-success"
                    :disabled="bill.payLoading">
                    {{ bill.payLoading ? '支付中...' : '立即支付' }}
                  </button>
                  <span v-else>-</span>
                </td>
              </tr>
            </tbody>
          </table>
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
const bills = ref([]);
const loading = ref(false);
const error = ref('');

const getMerchantAccount = () => {
    return userStore.userInfo?.account;
}

const fetchMyBills = async () => {
  const account = getMerchantAccount();
  if (!account) {
    error.value = '无法获取您的商户信息，请重新登录。';
    return;
  }

  loading.value = true;
  error.value = '';
  try {
    const response = await axios.get('/api/Store/GetMyRentBills', { 
        params: { merchantAccount: account }
    });
    bills.value = response.data.bills.map(b => ({ ...b, payLoading: false }));
  } catch (err) {
    error.value = err.response?.data?.error || '获取账单失败，请稍后重试。';
  } finally {
    loading.value = false;
  }
};

const payBill = async (bill) => {
  bill.payLoading = true;
  try {
    const payload = {
        storeId: bill.StoreId,
        billPeriod: bill.BillPeriod,
        paymentMethod: '线上支付'
    };
    await axios.post('/api/Store/PayRent', payload);
    
    // 支付成功后，更新本地账单状态
    const billInList = bills.value.find(b => b.BillPeriod === bill.BillPeriod);
    if (billInList) {
        billInList.BillStatus = '已缴纳';
    }
    alert('支付成功！');

  } catch (err) {
    alert(`支付失败: ${err.response?.data?.error || '未知错误'}`);
  } finally {
    bill.payLoading = false;
  }
};

onMounted(() => {
  fetchMyBills();
});
</script>

<style scoped>
:root {
  --primary-color: #1abc9c;
  --success-color: #2ecc71;
  --warning-color: #f39c12;
  --danger-color: #e74c3c;
  --info-color: #3498db;
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

.content-card {
  background-color: var(--card-bg);
  border-radius: var(--border-radius);
  box-shadow: var(--card-shadow);
  padding: 24px;
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

.table-container {
  overflow-x: auto;
}

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

.btn-success {
  padding: 8px 16px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 500;
  background-color: #2ecc71;
  color: white;
  transition: background-color 0.2s, transform 0.2s;
}
.btn-success:hover:not(:disabled) { 
  background-color: #27ae60;
  transform: translateY(-2px);
}
.btn-success:disabled { 
  background-color: #a3e9a4; 
  cursor: not-allowed; 
}

.status-tag {
  padding: 4px 10px;
  border-radius: 12px;
  font-weight: 500;
  font-size: 12px;
  color: white;
}
.status-待缴纳 { background-color: var(--warning-color); }
.status-已缴纳 { background-color: var(--success-color); }
.status-已确认 { background-color: var(--info-color); }
.status-逾期 { background-color: var(--danger-color); }
</style>
