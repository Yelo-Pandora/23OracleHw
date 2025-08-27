<template>
  <div class="store-status-request">
    <h2>店面状态变更申请</h2>
    <p>提交申请后，物业管理人员将在审批列表中处理。</p>

    <div class="box">
      <label>选择店铺ID（填写或选择）</label>
      <input type="number" v-model.number="storeId" min="1" />
      <button @click="loadStoreStatus" :disabled="!storeId">查询当前状态</button>
    </div>

    <div v-if="storeInfo" class="box">
      <p>店铺：{{ storeInfo.storeName }} （ID: {{ storeInfo.storeId }}）</p>
      <p>当前状态：{{ storeInfo.currentStatus }}</p>
      <p>可申请的变更类型：<span v-if="storeInfo.allowedChangeTypes.length === 0">无</span>
        <span v-else>{{ storeInfo.allowedChangeTypes.join(', ') }}</span>
      </p>
    </div>

    <form @submit.prevent="submitRequest" class="box">
      <label>变更类型</label>
      <select v-model="changeType" required>
        <option value="" disabled>请选择</option>
        <option v-for="t in allowedTypesForSelect" :key="t" :value="t">{{ t }}</option>
      </select>

      <label>目标状态</label>
      <input type="text" v-model="targetStatus" placeholder="可选：若留空系统会推断" />

      <label>申请原因</label>
      <textarea v-model="reason" rows="4" required></textarea>

      <div class="actions">
        <button type="submit" :disabled="submitting || !canSubmit">提交申请</button>
      </div>

      <div v-if="error" class="error">{{ error }}</div>
      <div v-if="success" class="success">{{ success }}</div>
    </form>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'
import { useUserStore } from '@/stores/user'

const storeId = ref(null)
const storeInfo = ref(null)
const changeType = ref('')
const targetStatus = ref('')
const reason = ref('')
const submitting = ref(false)
const error = ref('')
const success = ref('')

const allowedTypesForSelect = ref(["退租", "维修", "暂停营业", "恢复营业"]) // will be filtered by storeInfo

const userStore = useUserStore()

const canSubmit = computed(() => {
  return storeId.value && changeType.value && reason.value && !!(userStore.userInfo?.account || userStore.token)
})

async function loadStoreStatus() {
  error.value = ''
  success.value = ''
  storeInfo.value = null
  try {
    const res = await axios.get(`/api/Store/StoreStatus/${storeId.value}`)
    storeInfo.value = res.data
    // 如果后端提供 allowedChangeTypes，则同步到下拉
    if (storeInfo.value && Array.isArray(storeInfo.value.allowedChangeTypes)) {
      // 保留默认顺序并过滤空
      allowedTypesForSelect.value = storeInfo.value.allowedChangeTypes
    }
  } catch (e) {
    error.value = e?.response?.data?.error || '无法查询店铺状态'
  }
}

async function submitRequest() {
  error.value = ''
  success.value = ''
  submitting.value = true
  try {
    const dto = {
      StoreId: Number(storeId.value),
      ChangeType: changeType.value,
      Reason: reason.value,
      TargetStatus: targetStatus.value,
      ApplicantAccount: userStore.userInfo?.account || userStore.token || ''
    }

    const res = await axios.post('/api/Store/StatusChangeRequest', dto)
    success.value = res.data?.message || '申请提交成功'
    // 清空表单但保留 storeId
    changeType.value = ''
    targetStatus.value = ''
    reason.value = ''
  } catch (e) {
    error.value = e?.response?.data?.error || '申请提交失败'
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.box { background:#fff; padding:12px; margin-bottom:12px; border-radius:6px }
label { display:block; font-weight:600; margin-bottom:6px }
input, select, textarea { width:100%; padding:8px; box-sizing:border-box }
.actions { margin-top:12px }
.error { color:#c00 }
.success { color:#080 }
</style>
