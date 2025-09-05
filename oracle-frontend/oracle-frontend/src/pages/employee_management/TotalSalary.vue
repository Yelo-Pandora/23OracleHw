<template>
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
            <tr class="table_row" v-for="item in totalSalaries" :key="item.year + '-' + item.month">
              <td class="table_cell c1">{{ item.year }}</td>
              <td class="table_cell c2">{{ item.month }}</td>
              <td class="table_cell c1">{{ item.total }}</td>
              <td class="table_cell c2">
                <button class="action-btn detail-btn" @click="viewDetail(item)">查看详情</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
    <SalaryDetailModal
      :show="showDetailModal"
      :year="currentYear"
      :month="currentMonth"
      :salaryList="currentSalaryList"
      @close="showDetailModal = false"
    />
</template>

<script setup>
import DashboardLayout from '@/components/BoardLayout.vue';
import axios from 'axios';
import { ref, onMounted } from 'vue';
import { computed } from 'vue';
import SalaryDetailModal from './SalaryDetailModal.vue';

const employees = ref([]);
const salarySlips = ref([]);
const showDetailModal = ref(false);
const currentYear = ref('');
const currentMonth = ref('');
const currentSalaryList = ref([]);

// 计算每月总工资（年、月、总工资），按年降序、月降序
const totalSalaries = computed(() => {
  // 按年月分组
  const map = {};
  for (const slip of salarySlips.value) {
    const year = slip.year;
    const month = slip.month;
    const key = `${year}-${month}`;
    if (!map[key]) {
      map[key] = { year, month, total: 0 };
    }
    // 工资 = salary + bonus - fine
    map[key].total += Number(slip.salary || 0) + Number(slip.bonus || 0) - Number(slip.fine || 0);
  }
  // 转数组并排序
  return Object.values(map).sort((a, b) => {
    if (a.year !== b.year) return b.year - a.year;
    return b.month - a.month;
  });
});

function viewDetail(item) {
  currentYear.value = item.year;
  currentMonth.value = item.month;
  currentSalaryList.value = salarySlips.value.filter(slip => slip.year === item.year && slip.month === item.month)
    .map(slip => {
      const emp = employees.value.find(e => e.id === slip.staffId);
      return {
        name: emp ? emp.name : '',
        staffId: slip.staffId,
        account: emp ? emp.account : '',
        username: emp ? emp.username : '',
        dept: emp ? emp.department : '',
        salary: slip.salary,
        bonus: slip.bonus,
        fine: slip.fine
      };
    });
  showDetailModal.value = true;
}

onMounted(async () => {
    // 获取所有员工
    try{
        const staff = await axios.get('/api/Staff/AllStaffs');
        employees.value = staff.data.map(item => ({
            id: item.STAFF_ID,
            name: item.STAFF_NAME,
            sex: item.STAFF_SEX,
            department: item.STAFF_APARTMENT,
            position: item.STAFF_POSITION,
            salary: item.STAFF_SALARY
        }));
    } catch (error) {
        console.error("Error fetching staff data:", error);
    }

    // 对于每个employee, GetAccountByStaffId
    try {
        for (const emp of employees.value) {
            if (!emp.id) continue;
            const account = await axios.get('/api/Accounts/GetAccById', {
                params: {
                    staffId: emp.id
                }
            });
            const acc = account.data;
            emp.account = acc.ACCOUNT;
            emp.username = acc.USERNAME;
            emp.authority = acc.AUTHORITY;
        }
    } catch (error) {
        console.error("Error fetching account data:", error);
    }

    // SalarySlip
    try {
      const res = await axios.get('/api/Staff/AllsalarySlip');
      salarySlips.value = res.data.map(slip => ({
        staffId: slip.STAFF_ID,
        year: String(slip.MONTH_TIME).split('-')[0],
        month: String(slip.MONTH_TIME).split('-')[1],
        bonus: slip.BONUS,
        fine: slip.FINE,
        attendence: slip.ATD_COUNT
      }));
      // 查找employees获得salary
      for (const slip of salarySlips.value) {
        const emp = employees.value.find(e => e.id === slip.staffId);
        if (emp) {
          slip.salary = emp.salary;
        } else {
          slip.salary = 0;
        }
      }
    } catch (error) {
      console.error("Error fetching salary slip data:", error);
    }
    console.log(salarySlips.value)
})
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
  transition: background 0.2s, color 0.2s;
}
.detail-btn {
  background: #f7b731;
  color: #fff;
}
.detail-btn:hover {
  background: #f7c676;
}
</style>