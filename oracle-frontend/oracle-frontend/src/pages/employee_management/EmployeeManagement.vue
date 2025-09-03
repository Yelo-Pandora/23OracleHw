<template>
    <DashboardLayout>
    <div class="employee-management">
        <div class = "header">
            <div class="hint">
                你现在的身份是 <strong>{{ currentUserRole }}</strong>
            </div>
            <div class="buttons" v-if="userEmployee && Number(userEmployee.authority) <= 2">
                <button class = "btn" @click="AddStaff">添加员工</button>
            </div>
        </div>
        <div class="content">
            <table>
                <thead>
                    <tr>
                        <th class = "table_header">员工账号</th>
                        <th class = "table_header">员工昵称</th>
                        <th class = "table_header">员工ID</th>
                        <th class = "table_header">姓名</th>
                        <th class = "table_header">性别</th>
                        <th class = "table_header">所属部门</th>
                        <th class = "table_header">职位</th>
                        <th class = "table_header">员工权限</th>
                        <th class = "table_header">底薪</th>
                        <th class = "table_header">操作</th>
                    </tr>
                </thead>
                <tbody>
                    <tr class="table_row" v-for="employee in sortedEmployees" :key="employee.id">
                        <td class = "table_cell c1">{{ employee.account }}</td>
                        <td class = "table_cell c2">{{ employee.username }}</td>
                        <td class = "table_cell c1">{{ employee.id }}</td>
                        <td class = "table_cell c2">{{ employee.name }}</td>
                        <td class = "table_cell c1">{{ employee.sex }}</td>
                        <td class = "table_cell c2">{{ employee.department }}</td>
                        <td class = "table_cell c1">{{ employee.position }}</td>
                        <td class = "table_cell c2">{{ employee.authority }}</td>
                        <td class = "table_cell c1">{{ employee.salary }}</td>
                        <td class = "table_cell c2">
                            <button class="action-btn salary-btn" 
                            @click="DisplaySalaryWindow(employee.id)">工资条目</button>
                            <button class="action-btn edit-btn" 
                            @click="EditStaff(employee.id)">编辑员工信息</button>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <SalarySlipModal
            :show="showSalarySlipWindow"
            :employeeInfo="employees.find(emp => emp.id === currentEmployeeId)"
            :salarySlip="salarySlip.filter(slip => slip.staffId === currentEmployeeId)"
            @close="showSalarySlipWindow = false"
        />
        <AddStaffModal
            :show="showAddStaffModal"
            :operatorAccount="userStore.userInfo.account"
            @close="showAddStaffModal = false"
        />
        <EmployeeInfoEditModal
            :show="showEditEmployeeModal"
            :employeeInfo="employees.find(emp => emp.id === currentEmployeeId)"
            :operatorAccount="userStore.userInfo.account"
            :operatorAuthority="userEmployee?.authority"
            @close="showEditEmployeeModal = false"
        />
    </div>
    </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '@/components/BoardLayout.vue';
import SalarySlipModal from './SalarySlipModal.vue';
import AddStaffModal from './AddStaffModal.vue';
import EmployeeInfoEditModal from './EmployeeInfoEditModal.vue';
import axios from 'axios';
import { ref, onMounted } from 'vue';
import { useUserStore } from '@/user/user';
import { computed } from 'vue';

const userStore = useUserStore();
const currentUserRole = userStore.role;

const currentEmployeeId = ref(null);
const employees = ref([]);
const userEmployee = ref(null);

const salarySlip = ref([]);
const showSalarySlipWindow = ref(false);
const showAddStaffModal = ref(false);
const showEditEmployeeModal = ref(false);

const sortedEmployees = computed(() => {
    // 权限过滤
    if (!userEmployee.value || !userEmployee.value.authority) return [];
    const auth = Number(userEmployee.value.authority);
    let filtered = employees.value;
    if (auth === 1) {
        // 全部显示
        console.log("全部显示");
        filtered = employees.value;
    } else if (auth === 2) {
        // 只显示同部门
        console.log("只显示同部门");
        filtered = employees.value.filter(emp => emp.department === userEmployee.value.department);
    } else if (auth > 2) {
        // 只显示自己
        console.log("只显示自己");
        filtered = employees.value.filter(emp => emp.id === userEmployee.value.id);
    }
    return filtered.slice().sort((a, b) => {
        const idA = Number(a.id);
        const idB = Number(b.id);
        if (isNaN(idA) || isNaN(idB)) return String(a.id).localeCompare(String(b.id));
        return idA - idB;
    });
});

// salary
function DisplaySalaryWindow(employeeId) {
    currentEmployeeId.value = employeeId;
    showSalarySlipWindow.value = true;
}


// add staff
function AddStaff() {
    showAddStaffModal.value = true;
}

// edit staff
function EditStaff(employeeId) {
    currentEmployeeId.value = employeeId;
    showEditEmployeeModal.value = true;
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
        const salarySlips = await axios.get('/api/Staff/AllsalarySlip');
        // 赋给employee
        salarySlip.value = salarySlips.data.map(item => ({
            staffId: item.STAFF_ID,
            date: item.MONTH_TIME,
            attendence: item.ATD_COUNT,
            bonus: item.BONUS,
            fine: item.FINE,
        }));
    }
    catch (error) {
        console.error("Error fetching salary slip data:", error);
    }

    // 设置当前登录员工信息（假设userStore.account为当前登录账号）
    userEmployee.value = employees.value.find(emp => emp.account === userStore.userInfo.account) || null;
})

</script>

<style scoped>
@import './EmployeeManagement.css';
</style>



