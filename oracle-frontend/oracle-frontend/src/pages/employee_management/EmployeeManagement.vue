
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

                <tr class="table_row" v-for="employee in employees" :key="employee.id">
                    <td class = "table_cell c1">{{ employee.account }}</td>
                    <td class = "table_cell c2">{{ employee.username }}</td>
                    <td class = "table_cell c1">{{ employee.id }}</td>
                    <td class = "table_cell c2">{{ employee.name }}</td>
                    <td class = "table_cell c1">{{ employee.sex }}</td>
                    <td class = "table_cell c2">{{ employee.department }}</td>
                    <td class = "table_cell c1">{{ employee.position }}</td>
                    <td class = "table_cell c2">{{ employee.authority }}</td>
                    <td class = "table_cell c1">{{ employee.salary }}</td>
                </tr>
            </table>
        </div>
        <div class="footer">footer</div>
    </div>
    </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '@/components/BoardLayout.vue';
import { ref, onMounted } from 'vue';
import axios from 'axios';
import { useUserStore } from '@/user/user';

const userStore = useUserStore();
const currentUserRole = userStore.role;

const employees = ref([]);

onMounted(async () => {
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

    const account = await axios.get('/api/Accounts/GetAccountByStaffId', {
        params: {
            staffId: 1
        }
    });
    console.log(account.data)

    // 对于每个employee, GetAccountByStaffId
    // try {
    //     for (const emp of employees.value) {
    //         if (!emp.id) continue;
    //         const account = await axios.get('/api/Accounts/GetAccountByStaffId', {
    //             params: {
    //                 staffId: emp.id
    //             }
    //         });
    //         if (account.data && account.data.length > 0) {
    //             const acc = account.data[0];
    //             emp.account = acc.ACCOUNT;
    //             emp.username = acc.USERNAME;
    //             emp.authority = acc.AUTHORITY;
    //         }
    //     }
    // } catch (error) {
    //     console.error("Error fetching account data:", error);
    // }
})

</script>

<style scoped>
.header {
    background-color: #f5f5f5;
    padding: 12px 16px;
    border-bottom: 1px solid #eee;
}

.header .hint {
    font-size: 14px;
    color: #666;
}

.header .buttons {
    margin-top: 16px;
}

.header .buttons .btn {
    margin-right: 32px;
    width: 100px;
    height: 42px;
    border: none;
    background-color: #499ffb;
    color: white;
    border-radius: 8px;
    cursor: pointer;
    font-weight: bold;
}

.footer {
    background-color: #f5f5f5;
    padding: 12px 16px;
    border-top: 1px solid #eee;
}

.content {
    padding: 16px;
}

.content table {
    width: 100%;
    border-collapse: separate;
}

.content th,
.content td {
    padding: 12px 16px;
    border-bottom: 1px solid #eee;
}

.content th {
    background-color: #9cd1f6;
    text-align: left;
}

.content tr:hover {
    background-color: #cafefc;
}

.content tr .c1 {
    background-color: #d8edf2;
}

.content tr .c2 {
    background-color: #bdf0fc;
}

</style>