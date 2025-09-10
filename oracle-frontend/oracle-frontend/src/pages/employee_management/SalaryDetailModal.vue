<template>
	<div class="salary-detail-modal" v-if="show">
		<div class="salary-detail-content">
			<div class="salary-detail-title">
				{{ year }}年{{ month }}月工资详情
				<button class="close-btn" @click="handleClose">关闭</button>
			</div>
			<div class="salary-detail-divider"></div>
					<!-- 搜索区（支持部门模糊搜索） -->
					<div class="salary-detail-filter-row">
						<input v-model="searchText" class="salary-search-input" placeholder="搜索姓名/账号/昵称/部门..." />
					</div>
			<div class="salary-detail-table-wrap">
				<table class="salary-detail-table">
					<thead>
						<tr>
							<th>员工姓名</th>
							<th>员工ID</th>
							<th>员工账号</th>
							<th>员工昵称</th>
                            <th>部门</th>
							<th>底薪</th>
							<th>奖金</th>
							<th>罚金</th>
							<th>总工资</th>
							<th v-if="editable">操作</th>
						</tr>
					</thead>
					<tbody>
						<tr v-for="emp in filteredList" :key="emp.staffId">
							<td>{{ emp.name }}</td>
							<td>{{ emp.staffId }}</td>
							<td>{{ emp.account }}</td>
							<td>{{ emp.username }}</td>
							<td>{{ emp.dept }}</td>
							<td>
								<template v-if="editRow === emp.staffId">
									<input v-model.number="editForm.salary" type="number" min="0" 
									class="salary-edit-input" />
								</template>
								<template v-else>{{ emp.salary }}</template>
							</td>
							<td>
								<template v-if="editRow === emp.staffId">
									<input v-model.number="editForm.bonus" type="number" min="0" 
									class="salary-edit-input" />
								</template>
								<template v-else>{{ emp.bonus }}</template>
							</td>
							<td>
								<template v-if="editRow === emp.staffId">
									<input v-model.number="editForm.fine" type="number" min="0" 
									class="salary-edit-input" />
								</template>
								<template v-else>{{ emp.fine }}</template>
							</td>
							<td>{{ calcTotalSalary(editRow === emp.staffId ? editForm.salary : emp.salary, editRow === emp.staffId ? editForm.bonus : emp.bonus, editRow === emp.staffId ? editForm.fine : emp.fine) }}</td>
							<td v-if="editable">
								<button class="salary-edit-btn" @click="onEdit(emp)">{{ editRow === emp.staffId ? '保存' : '编辑' }}</button>
							</td>
						</tr>
					</tbody>
				</table>
			</div>
		</div>
	</div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import axios from 'axios';
const props = defineProps({
	show: Boolean,
	year: String,
	month: String,
	operatorAccount: String,
	//新增显示可编辑属性，便于现金流使用，默认可编辑
  editable: {
    type: Boolean,
    default: true
  }
});

const emit = defineEmits(['close', 'salaryUpdated']);

const searchText = ref('');
const editRow = ref(null);
const editForm = ref({ salary: 0, bonus: 0, fine: 0 });

const staffList = ref([]); // 员工列表
const salarySlipList = ref([]); // 工资条列表
const employeeInfos = ref([]);

async function fillEmployeeAccounts() {
	if (!staffList.value.length) return;
	const infos = [];
	for (const staff of staffList.value) {
		let account = '';
		let username = '';
		const res = await axios.get('/api/Accounts/GetAccById', { params: { staffId: staff.STAFF_ID } });
		if (res.data && res.data.ACCOUNT) {
			account = res.data.ACCOUNT;
			username = res.data.USERNAME;
		}
		
		infos.push({
			staffId: staff.STAFF_ID,
			name: staff.STAFF_NAME,
			dept: staff.STAFF_APARTMENT,
			salary: staff.STAFF_SALARY,
			account,
			username
		});
	}
	employeeInfos.value = infos;
}

const mergedList = computed(() => {
		// 只显示当前月份的工资条
		return employeeInfos.value.map(emp => {
			// 工资条匹配用 STAFF_ID
			const slip = salarySlipList.value.find(
				s => s.STAFF_ID === emp.staffId && 
				(s.MONTH_TIME?.split('-')[0] === props.year && s.MONTH_TIME?.split('-')[1] === props.month)
			);
			return {
				...emp,
				bonus: slip ? slip.BONUS : 0,
				fine: slip ? slip.FINE : 0
			};
		});
});

const filteredList = computed(() => {
	const text = searchText.value.trim().toLowerCase();
	return mergedList.value.filter(emp => {
		return !text || [emp.name, emp.account, emp.username, emp.dept].some(v => (v||'').toLowerCase().includes(text));
	});
});

function calcTotalSalary(base, bonus, fine) {
	const b = Number(base)||0, bo=Number(bonus)||0, f=Number(fine)||0;
	return b + bo - f;
}

async function fetchStaffAndSalarySlip() {
	if (!props.year || !props.month) return;
	try {
		const [staffRes, slipRes] = await Promise.all([
			axios.get('/api/Staff/AllStaffs'),
			axios.get('/api/Staff/AllsalarySlip', { params: { monthTime: `${props.year}-${props.month}` } })
		]);
		staffList.value = staffRes.data || [];
		salarySlipList.value = slipRes.data || [];
		await fillEmployeeAccounts();
	} catch (e) {
		staffList.value = [];
		salarySlipList.value = [];
		employeeInfos.value = [];
		console.log('Error fetching staff, salary slip, or account:', e);
	}
}

watch(() => [props.year, props.month, props.show], ([y, m, show]) => {
	if (show && y && m) fetchStaffAndSalarySlip();
});

onMounted(() => {
	if (props.show && props.year && props.month) fetchStaffAndSalarySlip();
});

async function onEdit(emp) {
	if (editRow.value === emp.staffId) {
		try {
			await axios.post('/api/Staff/StaffSalaryManagement', {
				BASE_SALARY: Number(editForm.value.salary),
				BONUS: Number(editForm.value.bonus),
				FINE: Number(editForm.value.fine)
			}, {
				params: {
					operatorAccount: props.operatorAccount,
					staffId: emp.staffId,
					monthTime: `${props.year}-${props.month}`
				}
			});
			// 更新本地数据
			emp.salary = editForm.value.salary;
			emp.bonus = editForm.value.bonus;
			emp.fine = editForm.value.fine;
			editRow.value = null;
			emit('salaryUpdated'); // 通知父组件刷新数据
			alert('保存成功');
		} catch (e) {
			alert('保存失败：' + (e?.response?.data || e.message));
		}
	} else {
		editRow.value = emp.staffId;
		editForm.value = {
			salary: emp.salary,
			bonus: emp.bonus,
			fine: emp.fine
		};
	}
}

function handleClose() {
	// 编辑模式下关闭等同于取消编辑
	editRow.value = null;
	editForm.value = { salary: 0, bonus: 0, fine: 0 };
	// 关闭弹窗
	emit('close');
}
</script>

<style scoped>
.salary-detail-modal {
	position: fixed;
	left: 0; top: 0; right: 0; bottom: 0;
	background: rgba(0,0,0,0.2);
	display: flex;
	align-items: center;
	justify-content: center;
	z-index: 1000;
}
.salary-detail-content {
	background: #fff;
	border-radius: 16px;
	padding: 24px;
	min-width: 1000px;
	max-width: 1200px;
	box-shadow: 0 4px 24px #0002;
}
.salary-detail-title {
	font-size: 20px;
	font-weight: bold;
	margin-bottom: 18px;
	display: flex;
	justify-content: space-between;
	align-items: center;
}
.close-btn {
	background: #f7b731;
	color: #fff;
	border: none;
	border-radius: 6px;
	padding: 6px 18px;
	font-size: 14px;
	cursor: pointer;
	transition: transform 0.2s, box-shadow 0.2s;
}
.close-btn:hover {
	background: #f7c676;
	transform: scale(1.08);
	box-shadow: 0 2px 12px #f7b73155;
}
.salary-detail-divider {
	width: 100%; height: 1.5px;
	background: #e0e0e0;
	margin: 8px 0 16px 0;
}
.salary-detail-table-wrap {
	flex: 1;
	margin: 0; padding: 0;
	max-height: 38vh; min-height: 80px;
	width: 100%;
	overflow-y: auto;
	position: relative;
}

.salary-detail-filter-row {
	display: flex;
	gap: 18px;
	margin-bottom: 12px;
	align-items: center;
}
.salary-search-input {
	flex: 1;
	padding: 8px 14px;
	font-size: 15px;
	border: 1px solid #b6e0fa;
	border-radius: 6px;
	outline: none;
	background: #f8fbfd;
}
.salary-search-input:focus {
	border-color: #9cd1f6;
}
.salary-select-wrap {
	max-height: 40px;
	overflow: visible;
	position: relative;
}
.salary-select {
	min-width: 120px;
	max-width: 180px;
	max-height: 120px;
	padding: 8px 10px;
	font-size: 15px;
	border: 1px solid #b6e0fa;
	border-radius: 6px;
	background: #eaf3fa;
	outline: none;
	overflow-y: auto;
}
.salary-detail-table {
	width: 100%;
	border-collapse: separate;
	border-spacing: 1;
	border-radius: 12px;
	overflow: hidden;
	table-layout: fixed;
}
.salary-detail-table th,
.salary-detail-table td {
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
.salary-detail-table td {
	background: #f8fbfd;
}
.salary-detail-table td:nth-child(even) {
	background: #eaf3fa;
}
.salary-detail-table th:nth-child(even) {
	background: #b6e0fa;
}
.salary-detail-table th {
	background: #9cd1f6;
	font-weight: bold;
	position: sticky;
	top: 0;
	z-index: 2;
}
.salary-detail-table tbody tr {
	transition: background 0.2s;
}
.salary-detail-table tbody tr:hover {
	background: #e0f3ff !important;
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
	transition: background 0.2s, transform 0.18s;
}
.salary-edit-btn:hover {
	background: #ff9800;
	transform: scale(1.08);
}
.salary-edit-btn:active {
	transform: scale(0.96);
}
.salary-edit-input {
	width: 60%;
	padding: 8px;
	font-size: 15px;
	border: 1px solid #b6e0fa;
	border-radius: 6px;
	outline: none;
	background: #f8fbfd;
}
.salary-edit-input:focus {
	border-color: #9cd1f6;
}
</style>
