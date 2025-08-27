<template>
	<DashboardLayout>
		<div class="create-area">
			<h1>新增店面区域</h1>

			<form @submit.prevent="createArea" class="form">
				<div class="row">
					<label>区域 ID</label>
					<input type="number" v-model.number="areaId" min="1" @input="clearDuplicateError" required />
					<div v-if="duplicateExists" class="error">{{ duplicateErrorMessage }}</div>
				</div>

				<div class="row">
					<label>区域面积 (㎡)</label>
					<input type="number" v-model.number="areaSize" min="1" required />
				</div>

				<div class="row">
					<label>基础租金 (元/月)</label>
					<input type="number" v-model.number="baseRent" min="0.01" step="0.01" required />
				</div>

				<div class="actions">
					<button type="submit" :disabled="submitting">创建店面区域</button>
				</div>

				<div v-if="loading" class="info">加载中...</div>
				<div v-if="error && !duplicateExists" class="error">{{ error }}</div>
				<div v-if="success" class="success">{{ success }}</div>
			</form>
		</div>
	</DashboardLayout>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'
import DashboardLayout from '@/components/BoardLayout.vue'
import { useUserStore } from '@/stores/user'

const areaId = ref(null)
const areaSize = ref(100)
const baseRent = ref(1000)

const loading = ref(false)
const submitting = ref(false)
const error = ref('')
const success = ref('')
const duplicateExists = ref(false)
const duplicateErrorMessage = ref('')

// operator account: prefer logged-in user
const userStore = useUserStore()
const operatorAccount = ref(userStore.userInfo?.account || userStore.token || 'admin')

async function createArea() {
	error.value = ''
	success.value = ''
	duplicateErrorMessage.value = '' // 清除错误消息

		// 如果预检查发现重复，阻止提交并在输入框下方提示（不要在表单底部重复显示）
		if (duplicateExists.value) {
			return
		}

	if (!areaId.value || areaId.value <= 0) {
		error.value = '区域ID必须为正整数'
		return
	}
	if (!areaSize.value || areaSize.value <= 0) {
		error.value = '区域面积必须大于0'
		return
	}
	if (baseRent.value == null || baseRent.value <= 0) {
		error.value = '基础租金必须大于0'
		return
	}

	submitting.value = true
	loading.value = true
	try {
			const dto = {
				AreaId: areaId.value,
				AreaSize: areaSize.value,
				BaseRent: baseRent.value,
				OperatorAccount: operatorAccount.value
			}

		const res = await axios.post('/api/Store/CreateRetailArea', dto)
		success.value = res.data?.message || '创建成功'
		// 显示返回的区域信息（可选）
		if (res.data) {
			success.value += `：区域ID ${res.data.areaId}，状态 ${res.data.rentStatus}`
		}
	} catch (e) {
		console.error(e)
			// 解析后端返回的重复 ID 错误并给出更详细的提示
			const resp = e?.response?.data
			const msg = resp?.error || (resp?.details ? Array.isArray(resp.details) ? resp.details.join('; ') : JSON.stringify(resp.details) : null)
				if (msg && typeof msg === 'string' && (msg.includes('该区域ID已存在') || msg.includes('已存在') || (msg.toLowerCase().includes('duplicate')))) {
					duplicateExists.value = true
					duplicateErrorMessage.value = msg // 设置静态错误消息
				} else {
					error.value = msg || e.message || '创建失败'
				}
	} finally {
		submitting.value = false
		loading.value = false
	}
}

	// 预检查：在用户输入区域ID并失焦时，查询可用区域列表做简单存在性检查（仅可捕获在可用列表中的重复）
	function clearDuplicateError() {
		// 用户修改输入时清除由后端返回的重复错误信息
		if (duplicateExists.value) {
			duplicateExists.value = false
			duplicateErrorMessage.value = ''
		}
		if (error.value) {
			error.value = ''
		}
	}
</script>

<style scoped>
.create-area { padding: 16px }
.form { max-width:600px; background:#fff; padding:16px; border-radius:6px }
.row { margin-bottom:12px }
label { display:block; font-weight:600; margin-bottom:6px }
input { width:100%; padding:8px; box-sizing:border-box }
.actions { margin-top:12px }
.error { color:#c00; margin-top:8px }
.success { color:#080; margin-top:8px }
.info { color:#666; margin-top:8px }
</style>

