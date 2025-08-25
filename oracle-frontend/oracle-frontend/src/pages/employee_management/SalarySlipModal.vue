<template>
    <div class="temp-salary-window" v-if="show">
        <div class="temp-salary-content">
            <div class="temp-salary-title">员工{{ employeeInfo?.id }}工资条</div>
            <div class="temp-salary-header">
                <label>年份：</label>
                <input class="salary-year-input" v-model="selectedYear" maxlength="4" placeholder="2025" />
                <label class="salary-month-label">月份区间：</label>
                <select class="salary-month-select" v-model="startMonth">
                    <option v-for="m in monthOptions" :key="m" :value="m">{{ m }}</option>
                </select>
                <span class="salary-month-sep">-</span>
                <select class="salary-month-select" v-model="endMonth">
                    <option v-for="m in monthOptions" :key="m" :value="m">{{ m }}</option>
                </select>
            </div>
            <div class="temp-salary-divider"></div>
            <div class="temp-salary-table-wrap">
                <table class="temp-salary-table">
                    <thead>
                        <tr>
                            <th>员工账号</th>
                            <th>员工昵称</th>
                            <th>年份</th>
                            <th>月份</th>
                            <th>员工底薪</th>
                            <th>奖金</th>
                            <th>罚金</th>
                            <th>出勤次数</th>
                            <th>总工资</th>
                            <th>操作</th>
                        </tr>
                    </thead>
                    <tbody>

                        <tr v-for="item in filteredSalarySlip" :key="item.date">
                            <td>{{ employeeInfo?.account }}</td>
                            <td>{{ employeeInfo?.username }}</td>
                            <td>{{ getYear(item.date) }}</td>
                            <td>{{ getMonth(item.date) }}</td>
                            <td>{{ employeeInfo?.salary }}</td>
                            <td>{{ item.bonus }}</td>
                            <td>{{ item.fine }}</td>
                            <td>{{ item.attendence }}</td>
                            <td>{{ calcTotalSalary(employeeInfo?.salary, item.bonus, item.fine) }}</td>
                            <td><button class="salary-edit-btn">编辑</button></td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div>
                <button class="temp-salary-close" @click="$emit('close')">关闭</button>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';

const props = defineProps({
    show: Boolean,
    employeeInfo: Object, // {id, account, username, salary}
    salarySlip: Array // 该员工所有工资条
});

const now = new Date();

function getYear(date) {
    if (!date) return '';
    return String(date).split('-')[0];
}
function getMonth(date) {
    if (!date) return '';
    return String(date).split('-')[1];
}
const monthOptions = Array.from({length:12}, (_,i)=>String(i+1).padStart(2,'0'));

// 默认当前年份
const selectedYear = ref(String(now.getFullYear()));
const startMonth = ref('01');
const endMonth = ref('12');

watch(() => props.salarySlip, (val) => {
    // 若工资条数据变化，自动刷新年份选项
    const years = Array.from(new Set((val||[]).map(s => getYear(s.date)))).sort();
    if (!years.includes(selectedYear.value)) selectedYear.value = years[years.length-1] || String(now.getFullYear());
}, {immediate:true});

const filteredSalarySlip = computed(() => {
    // 只显示选中年份和月份区间的工资条
    const y = selectedYear.value;
    const sm = startMonth.value;
    const em = endMonth.value;
    return (props.salarySlip||[]).filter(item => {
        const itemYear = getYear(item.date);
        const itemMonth = getMonth(item.date);
        if (itemYear !== y) return false;
        return itemMonth >= sm && itemMonth <= em;
    });
});


function calcTotalSalary(base, bonus, fine) {
    const b = Number(base)||0, bo=Number(bonus)||0, f=Number(fine)||0;
    return b + bo - f;
}
</script>

<style scoped>
.temp-salary-window {
    position: fixed;
    top: 0; 
    left: 0;
    width: 100vw; height: 100vh;
    background: rgba(0,0,0,0.18);
    z-index: 9999;
    display: flex;
    align-items: center;
    justify-content: center;
}
.temp-salary-content {
    background: #fff;
    border-radius: 16px;
    border: 2px solid #e0e0e0;
    box-shadow: 0 8px 32px rgba(0,0,0,0.18);
    width: 70vw; height: 50vh;
    min-width: 600px; min-height: 260px;
    max-width: 1200px; max-height: 800px;
    padding: 32px 36px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: flex-start;
}
.temp-salary-title {
    width: 100%;
    text-align: center;
    font-size: 20px;
    font-weight: bold;
    margin-bottom: 10px;
}
.temp-salary-header {
    width: 100%;
    display: flex;
    align-items: center;
    margin-bottom: 8px;
    gap: 8px;
}
.temp-salary-divider {
    width: 100%; height: 1.5px;
    background: #e0e0e0;
    margin: 8px 0 16px 0;
}
.temp-salary-table-wrap {
    flex: 1;
    margin: 0; padding: 0;
    max-height: 38vh; min-height: 80px;
    width: 100%;
    overflow-y: auto;
    position: relative;
}
.temp-salary-table {
    width: 100%;
    border-collapse: separate;
    border-spacing: 1;
    border-radius: 12px;
    overflow: hidden;
    table-layout: fixed;
}
.temp-salary-table th,
.temp-salary-table td {
    padding: 12px 10px;
    border-bottom: 1px solid #eee;
    text-align: center;
    word-break: break-all;
    white-space: pre-line;
    max-width: 120px;
    min-width: 110px;
    height: 48px;
    box-sizing: border-box;
    font-size: 15px;
    overflow-wrap: break-word;
    vertical-align: middle;
    background: none;
    transition: background 0.2s;
}
.temp-salary-table td {
    background: #f8fbfd;
}
.temp-salary-table td:nth-child(even) {
    background: #eaf3fa;
}
.temp-salary-table th:nth-child(even) {
    background: #b6e0fa;
}
.temp-salary-table th {
    background: #9cd1f6;
    font-weight: bold;
    position: sticky;
    top: 0;
    z-index: 2;
}
.temp-salary-table tbody tr {
    transition: background 0.2s;
}
.temp-salary-table tbody tr:hover {
    background: #e0f3ff !important;
}
.temp-salary-table th {
    background: #9cd1f6;
    font-weight: bold;
    position: sticky;
    top: 0;
    z-index: 2;
}
.temp-salary-close {
    margin-top: 18px;
    padding: 8px 32px;
    border: none;
    border-radius: 8px;
    background: #499ffb;
    color: #fff;
    font-size: 16px;
    cursor: pointer;
    font-weight: bold;
    align-self: center;
}
.temp-salary-close:hover { background: #357ae8; }

/* 年份和月份选择美化 */
.salary-year-input {
    width: 70px;
    padding: 6px 8px;
    border: 1px solid #ccc;
    border-radius: 6px;
    font-size: 15px;
    margin-right: 10px;
    outline: none;
    text-align: center;
    transition: border 0.2s;
}
.salary-year-input:focus {
    border: 1.5px solid #499ffb;
}
.salary-month-label {
    margin-left: 16px;
    font-weight: 500;
}
.salary-month-select {
    padding: 6px 10px;
    border-radius: 6px;
    border: 1px solid #ccc;
    font-size: 15px;
    margin: 0 4px;
    outline: none;
    transition: border 0.2s;
}
.salary-month-select:focus {
    border: 1.5px solid #499ffb;
}
.salary-month-sep {
    margin: 0 6px;
    font-size: 16px;
    color: #666;
}
.salary-edit-btn {
    padding: 4px 16px;
    border: none;
    border-radius: 6px;
    background: #ffb74d;
    color: #fff;
    font-size: 14px;
    cursor: pointer;
    font-weight: 500;
    transition: background 0.2s;
}
.salary-edit-btn:hover {
    background: #ff9800;
}
</style>
