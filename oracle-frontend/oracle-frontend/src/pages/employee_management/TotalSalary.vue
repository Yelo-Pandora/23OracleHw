<template>
  <DashboardLayout>
    <div class="total_salary">
      <div class="content">
        <table>
          <thead>
            <tr>
              <th class="table_header">年份</th>
              <th class="table_header">月份</th>
              <th class="table_header">总工资支出</th>
              <th class="table_header">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr class="table_row" v-for="item in monthSalaryCosts" :key="item.MONTH_TIME">
              <td class="table_cell c1">{{ item.MONTH_TIME.split('-')[0] }}</td>
              <td class="table_cell c2">{{ item.MONTH_TIME.split('-')[1] }}</td>
              <td class="table_cell c1">{{ item.TOTAL_COST }}</td>
              <td class="table_cell c2">
                <button class="action-btn detail-btn" @click="viewDetail(item)">查看详情</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
    <SalaryDetailModal :show="showDetailModal"
                       :year="currentYear"
                       :month="currentMonth"
                       :salaryList="currentSalaryList"
                       :operatorAccount="userStore.userInfo.account"
                       @close="showDetailModal = false" />
    <!-- <SalaryDetailModal
      :show="showDetailModal"
      :year="currentYear"
      :month="currentMonth"
      :operatorAccount="userStore.userInfo.account"
      @close="showDetailModal = false"
      @salaryUpdated="refreshSalaryData"
    /> -->
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '@/components/BoardLayout.vue';
import axios from 'axios';
import SalaryDetailModal from './SalaryDetailModal.vue';
import { ref, onMounted } from 'vue';
import { useUserStore } from '@/user/user';

const monthSalaryCosts = ref([]);
const showDetailModal = ref(false);
const currentYear = ref('');
const currentMonth = ref('');
const userStore = useUserStore();

async function fetchMonthSalaryCosts() {
  try {
    const res = await axios.get('/api/Staff/AllMonthSalaryCost');
    monthSalaryCosts.value = res.data;
  } catch (error) {
    console.error('Error fetching month salary cost:', error);
  }
}

function viewDetail(item) {
  currentYear.value = item.MONTH_TIME.split('-')[0];
  currentMonth.value = item.MONTH_TIME.split('-')[1];
  showDetailModal.value = true;
}

const refreshSalaryData = async () => {
  await fetchMonthSalaryCosts();
};

onMounted(async () => {
  await fetchMonthSalaryCosts();
});
</script>

<style scoped>
.content {
  padding: 16px;
}
.content table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 1;
  border-radius: 16px;
  overflow: hidden;
}
.content th,
.content td {
  padding: 12px 16px;
  border-bottom: 1px solid #eee;
  text-align: center;
}
.content th {
  background: #9cd1f6;
}
.content th:first-child { border-top-left-radius: 16px; }
.content th:last-child { border-top-right-radius: 16px; }
.content tr:last-child td:first-child { border-bottom-left-radius: 16px; }
.content tr:last-child td:last-child { border-bottom-right-radius: 16px; }
.content tr:hover { background: #cafefc; }
.content tr .c1 { background: #d8edf2; }
.content tr .c2 { background: #bdf0fc; }

.action-btn {
  margin: 0 4px;
  padding: 6px 18px;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s, color 0.2s, transform 0.18s;
}
.action-btn:hover {
  transform: scale(1.08);
}
.action-btn:active {
  transform: scale(0.96);
}
.detail-btn {
  background: #f7b731;
  color: #fff;
}
.detail-btn:hover {
  background: #f7c676;
}
</style>
