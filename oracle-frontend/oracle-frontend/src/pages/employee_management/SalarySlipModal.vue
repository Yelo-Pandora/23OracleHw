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
                                <td>
                                    <template v-if="editRow === item.date">
                                        <input v-model.number="editForm.salary" type="number" min="0" class="salary-edit-input" />
                                    </template>
                                    <template v-else>{{ employeeInfo?.salary }}</template>
                                </td>
                                <td>
                                    <template v-if="editRow === item.date">
                                        <input v-model.number="editForm.bonus" type="number" min="0" class="salary-edit-input" />
                                    </template>
                                    <template v-else>{{ item.bonus }}</template>
                                </td>
                                <td>
                                    <template v-if="editRow === item.date">
                                        <input v-model.number="editForm.fine" type="number" min="0" class="salary-edit-input" />
                                    </template>
                                    <template v-else>{{ item.fine }}</template>
                                </td>
                                <td>
                                    {{ item.attendence }}
                                </td>
                                <td>{{ calcTotalSalary(editRow === item.date ? editForm.salary : employeeInfo?.salary, editRow === item.date ? editForm.bonus : item.bonus, editRow === item.date ? editForm.fine : item.fine) }}</td>
                                <td>
                                    <button
                                        class="salary-edit-btn"
                                        :disabled="!canEditRow()"
                                        :style="!canEditRow() ? 'background:#ccc;cursor:not-allowed;' : ''"
                                        @click="onEdit(item)"
                                    >
                                        {{ editRow === item.date ? '保存' : '编辑' }}
                                    </button>
                                </td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div class="add-new">
                <button class="add-salary-btn" @click="showAddRow = true" v-if="!showAddRow">+新增工资条目</button>
                <div v-if="showAddRow" class="add-row-form">
                    <input v-model="addForm.year" type="number" min="2000" max="2100" placeholder="年份" class="salary-edit-input" />
                    <select v-model="addForm.month" class="salary-edit-input">
                        <option v-for="m in monthOptions" :key="m" :value="m">{{ m }}</option>
                    </select>
                    <input v-model.number="addForm.bonus" type="number" min="0" placeholder="奖金" class="salary-edit-input" />
                    <input v-model.number="addForm.fine" type="number" min="0" placeholder="罚金" class="salary-edit-input" />
                    <button class="salary-edit-btn" @click="onAddSalarySlip">保存</button>
                    <button class="salary-edit-btn salary-cancel-btn" @click="cancelAddRow">取消</button>
                </div>
                <button class="temp-salary-close" @click="handleClose">关闭</button>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import axios from 'axios';

const props = defineProps({
    show: Boolean,
    employeeInfo: Object, // {account, authority, department, id, name, username, sex, position, salary}
    operatorAccount: String,
    operatorAuthority: [String, Number],
});

const salarySlip = ref([]);

async function fetchSalarySlip() {
    if (!props.employeeInfo?.id) return;
    try {
        const res = await axios.get('/api/Staff/AllsalarySlip');
        salarySlip.value = (res.data || []).filter(item => item.STAFF_ID === props.employeeInfo.id).map(item => ({
            staffId: item.STAFF_ID,
            date: item.MONTH_TIME,
            salary: item.BASE_SALARY || props.employeeInfo.salary,
            attendence: item.ATD_COUNT,
            bonus: item.BONUS,
            fine: item.FINE,
        }));
    } catch (error) {
        console.error('获取工资条失败:', error);
    }
}

watch(() => props.show, (val) => {
    if (val) fetchSalarySlip();
}, {immediate: true});

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

const selectedYear = ref(String(now.getFullYear()));
const startMonth = ref('01');
const endMonth = ref('12');

watch(salarySlip, (val) => {
    // 若工资条数据变化，自动刷新年份选项
    const years = Array.from(new Set((val||[]).map(s => getYear(s.date)))).sort();
    if (!years.includes(selectedYear.value)) selectedYear.value = years[years.length-1] || String(now.getFullYear());
}, {immediate:true});

const filteredSalarySlip = computed(() => {
    // 只显示选中年份和月份区间的工资条
    const y = selectedYear.value;
    const sm = startMonth.value;
    const em = endMonth.value;
    return (salarySlip.value||[]).filter(item => {
        const itemYear = getYear(item.date);
        const itemMonth = getMonth(item.date);
        if (itemYear !== y) return false;
        return itemMonth >= sm && itemMonth <= em;
    });
});

const editRow = ref(null);
const editForm = ref({ salary: 0, bonus: 0, fine: 0, attendence: 0 });
const showAddRow = ref(false);
const addForm = ref({ year: String(now.getFullYear()), month: '01', salary: '', bonus: '', fine: '' });

function cancelAddRow() {
    showAddRow.value = false;
    addForm.value = { year: String(now.getFullYear()), month: '01', salary: '', bonus: '', fine: '' };
}

async function onAddSalarySlip() {
    const year = addForm.value.year;
    const month = addForm.value.month;
    // 检查是否重复(年，月相同)
    if (salarySlip.value.some(s => getYear(s.date) === year && getMonth(s.date) === month)) {
        alert(`${year}年${month}月已有工资条目，不可重复添加`);
        return;
    }
    try {
        await axios.post('/api/Staff/StaffSalaryManagement', {
            BASE_SALARY: props.employeeInfo.salary,
            FINE: addForm.value.fine,
            BONUS: addForm.value.bonus
        }, 
            {
                params: {
                    operatorAccount: props.operatorAccount,
                    staffId: props.employeeInfo.id,
                    monthTime: `${year}-${month}`
                }
            }
        );
        await fetchSalarySlip(); // 保存成功后刷新工资条
        alert('工资条目添加成功');
        console.log(salarySlip.value);
    } catch (error) {
        console.error('添加工资条目失败:', error);
        alert('添加工资条目失败');
    }
    cancelAddRow();
}

const emit = defineEmits(['close', 'salarySlipAdded']);
function handleClose() {
    // 关闭弹窗时取消编辑模式并撤销未保存的更改
    editRow.value = null;
    editForm.value = { salary: 0, bonus: 0, fine: 0, attendence: 0 };
    // 如果正在新增工资条，则等同于点击取消
    if (showAddRow.value) {
        cancelAddRow();
    }
    emit('close');
}

function canEditRow() {
    // 只有操作者权限高于被操作者且不是本人才能编辑
    const myAuth = Number(props.operatorAuthority);
    const targetAuth = Number(props.employeeInfo?.authority);
    if (!props.operatorAccount || !props.employeeInfo) return false;
    if (props.operatorAccount === props.employeeInfo.account) return false;
    if (myAuth >= targetAuth) return false;
    return true;
}

async function onEdit(item) {
    if (editRow.value === item.date) {
        try {
            const staffId = props.employeeInfo.id;
            const operatorAccount = props.operatorAccount;
            // monthTime格式修正：yyyy-MM-01
            let monthTime = item.date;
            if (/^\d{4}-\d{2}$/.test(monthTime)) {
                monthTime = monthTime + '-01';
            }
            await axios.post('/api/Staff/StaffSalaryManagement', {
                BASE_SALARY: editForm.value.salary,
                BONUS: editForm.value.bonus,
                FINE: editForm.value.fine
            }, {
                params: {
                    operatorAccount,
                    staffId,
                    monthTime
                }
            });
            await fetchSalarySlip(); // 修改成功后刷新工资条
            emit('salaryUpdated'); // 通知父组件刷新员工数据
        } catch (e) {
            alert('保存失败：' + (e?.response?.data || e.message));
        }
        editRow.value = null;
    } else {
        // 进入编辑
        editRow.value = item.date;
        editForm.value = {
            salary: props.employeeInfo.salary,
            bonus: item.bonus,
            fine: item.fine,
        };
    }
}

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
    transition: background 0.18s, transform 0.18s;
}
.temp-salary-close:hover {
    background: #357ae8;
    transform: scale(1.08);
}
.temp-salary-close:active {
    transform: scale(0.96);
}

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
    transition: background 0.18s, transform 0.18s;
}
.salary-edit-btn:hover {
    background: #ff9800;
    transform: scale(1.08);
}
.salary-edit-btn:active {
    transform: scale(0.96);
}

.salary-edit-input {
    width: 80px;
    padding: 6px 10px;
    border: 1.5px solid #ffb74d;
    border-radius: 7px;
    font-size: 15px;
    background: #fffbe6;
    color: #333;
    outline: none;
    box-shadow: 0 2px 8px rgba(255,183,77,0.08);
    transition: border 0.2s, box-shadow 0.2s;
}
.salary-edit-input:focus {
    width:80px;
    border: 2px solid #ff9800;
    box-shadow: 0 2px 12px rgba(255,152,0,0.18);
}

.add-salary-btn {
    margin: 18px 0 0 0;
    padding: 8px 32px;
    border: none;
    border-radius: 8px;
    background: #43c07a;
    color: #fff;
    font-size: 16px;
    cursor: pointer;
    font-weight: bold;
    align-self: center;
    transition: background 0.18s, transform 0.18s;
}
.add-salary-btn:hover {
    background: #2e9c5c;
    transform: scale(1.08);
}
.salary-edit-btn {
    margin-left: 8px;
}

.add-row-form  {
    margin:12px 0;
    width:100%;
    display:flex;
    justify-content:center;
    gap:12px;
    align-items:center;
}

.add-new {
    width:100%;
    display:flex;
    flex-direction:column;
    align-items:center;
}

.salary-cancel-btn {
    background: #ccc;
    color: #333;
}
</style>
