<template>
  <DashboardLayout>
    <div class="my-rent-bills-page">
      <h2>我的租金账单</h2>
      <p class="subtitle">查看并支付您的月度租金账单。</p>

      <div class="list-container card">
        <h3>账单列表</h3>
        <div v-if="loading" class="loading-indicator">正在加载账单...</div>
        <div v-if="error" class="error-message">{{ error }}</div>
        <div v-if="!loading && bills.length === 0" class="info-message">您当前没有租金账单。</div>
        
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
                <td><span :class="`status-${bill.BillStatus}`">{{ bill.BillStatus }}</span></td>
                <td>
                  <button 
                    v-if="bill.BillStatus === '待缴纳' || bill.BillStatus === '逾期'" 
                    @click="payBill(bill)" 
                    class="pay-btn"
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
.my-rent-bills-page {
  padding: 16px;
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
  margin-bottom: 24px;
}

.list-container h3 {
  font-size: 18px;
  margin-top: 0;
  margin-bottom: 16px;
}

.loading-indicator, .info-message, .error-message {
  text-align: center;
  padding: 20px;
  font-size: 16px;
  color: #666;
}

.error-message {
  color: #c00;
  background-color: #fbeae5;
  border-radius: 4px;
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
  padding: 12px 16px;
  text-align: left;
}

.bill-table th {
  background-color: #f8f9fa;
  font-weight: 600;
  color: #555;
}

.pay-btn {
  background-color: #27ae60;
  color: white;
  padding: 8px 16px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.2s;
}

.pay-btn:hover {
  background-color: #219a52;
}

.pay-btn:disabled {
  background-color: #a3d9b8;
  cursor: not-allowed;
}

.status-待缴纳 { color: #e67e22; font-weight: bold; }
.status-已缴纳 { color: #27ae60; font-weight: bold; }
.status-已确认 { color: #2980b9; font-weight: bold; }
.status-逾期 { color: #c0392b; font-weight: bold; }
</style>
