<template>
    <DashboardLayout>
    <div class="employee-management">
        <div class = "header">
            <div class="hint">
                你现在的身份是 <strong>{{ currentUserRole }}</strong>
            </div>
            <div class="buttons">
                <button class = "btn" @click="addEmployee">添加员工</button>
                <button class = "btn" @click="editEmployee">编辑员工</button>
                <button class = "btn" @click="deleteEmployee">删除员工</button>
            </div>
        </div>
        <div class="content">
            <table>
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
                        <button class="action-btn salary-btn" @click="goToSalary(employee.id)">工资条目</button>
                        <button class="action-btn tempauth-btn" @click="DisplayTempAuthWindow(true, employee.id)">临时权限</button>
                    </td>
                </tr>
            </table>
        </div>
        <div class="temp-auth-window" v-if="showTempAuthWindow">
            <div class="temp-auth-content">
                <div class="temp-auth-title">
                    {{ showAllTempAuth ? '全部员工临时权限' : `员工${currentEmployeeId}临时权限` }}
                </div>
                <div class="temp-auth-header">
                    <input class="temp-auth-search" v-model="tempAuthSearch" placeholder="搜索..." />
                    <button class="temp-auth-toggle-btn" @click="toggleShowAllTempAuth">
                        {{ showAllTempAuth ? `只显示员工${currentEmployeeId}的权限` : '显示全部' }}
                    </button>
                </div>
                <div class="temp-auth-divider"></div>
                <div class="temp-auth-table-wrap">
                    <table class="temp-auth-table">
                        <thead>
                            <tr>
                                <th>员工账号</th>
                                <th>员工昵称</th>
                                <th>员工ID</th>
                                <th>活动ID</th>
                                <th>员工临时权限</th>
                                <th>操作</th>
                            </tr>
                        </thead>
                        <tbody class="temp-auth-table-body">
                            <tr v-for="item in filteredTempAuthList" :key="item.event_id">
                                <td>{{ item.account }}</td>
                                <td>{{ item.username }}</td>
                                <td>{{ item.staffId }}</td>
                                <td>{{ item.event_id }}</td>
                                <td>{{ item.temp_auth }}</td>
                                <td>
                                    <button class="action-btn temp_auth_edit" @click="editTempAuth(item)">编辑</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
                <div>
                    <button class="temp-auth-close" @click="showTempAuthWindow = false">关闭</button>
                </div>
            </div>
        </div>
    </div>
    </DashboardLayout>
</template>


<script setup>
import DashboardLayout from '@/components/BoardLayout.vue';
import axios from 'axios';
import { ref, onMounted } from 'vue';
import { useUserStore } from '@/user/user';
import { computed } from 'vue';

const userStore = useUserStore();
const currentUserRole = userStore.role;

const employees = ref([]);
const tempAuthList = ref([]);
const tempAuthSearch = ref('');
const showTempAuthWindow = ref(false);
const currentEmployeeId = ref(null);

const sortedEmployees = computed(() => {
    return employees.value.slice().sort((a, b) => {
        const idA = Number(a.id);
        const idB = Number(b.id);
        if (isNaN(idA) || isNaN(idB)) return String(a.id).localeCompare(String(b.id));
        return idA - idB;
    });
});

const showAllTempAuth = ref(false);
const filteredTempAuthList = computed(() => {
    let list = showAllTempAuth.value
        ? tempAuthList.value
        : tempAuthList.value.filter(item => item.staffId === currentEmployeeId.value);
    const keyword = tempAuthSearch.value.trim().toLowerCase();
    if (keyword) {
        list = list.filter(item => {
            return (
                (item.account && item.account.toLowerCase().includes(keyword)) ||
                (item.username && item.username.toLowerCase().includes(keyword)) ||
                (item.staffId && String(item.staffId).includes(keyword)) ||
                (item.event_id && String(item.event_id).includes(keyword)) ||
                (item.temp_auth && String(item.temp_auth).toLowerCase().includes(keyword))
            );
        });
    }
    // 按event_id升序排序
    return list.slice().sort((a, b) => {
        const idA = Number(a.event_id);
        const idB = Number(b.event_id);
        if (isNaN(idA) || isNaN(idB)) return String(a.event_id).localeCompare(String(b.event_id));
        return idA - idB;
    });
});

function toggleShowAllTempAuth() {
    showAllTempAuth.value = !showAllTempAuth.value;
}

// temp auth
function DisplayTempAuthWindow(display, employeeId) {
    showTempAuthWindow.value = display;
    currentEmployeeId.value = employeeId;
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

    // 临时权限
    try {
        const tempAuths = await axios.get('/api/Staff/AllTempAuthorities');
        tempAuthList.value = tempAuths.data.map(item => ({
            account: item.ACCOUNT,
            event_id: item.EVENT_ID,
            temp_auth: item.TEMP_AUTHORITY
        }));
        // staff id和 username
        tempAuthList.value.forEach(item => {
            const emp = employees.value.find(emp => emp.account === item.account);
            if (emp) {
                item.staffId = emp.id;
                item.username = emp.username;
            }
        });
    } catch (error) {
        console.error("Error fetching temporary authorization data:", error);
    }
    console.log(tempAuthList.value);
})

</script>

<style scoped>
@import './EmployeeManagement.css';
</style>



