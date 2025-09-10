<template>
  <div class="dashboard-home">
    <!-- 顶部欢迎区 -->
    <div class="welcome-section">
      <h2>欢迎回来, {{ userStore.userName || 'Admin' }} 👋</h2>
    </div>

    <!-- 三个信息卡片 -->
    <div class="summary-cards">
      <div class="card income">
        <div class="icon">💰</div>
        <div>
          <h3>总收入</h3>
          <p class="value">{{ summary.TotalIncome }}</p>
        </div>
      </div>
      <div class="card expense">
        <div class="icon">💸</div>
        <div>
          <h3>总支出</h3>
          <p class="value">{{ summary.TotalExpense }}</p>
        </div>
      </div>
      <div class="card net">
        <div class="icon">📊</div>
        <div>
          <h3>净现金流</h3>
          <p class="value">{{ summary.NetFlow }}</p>
        </div>
      </div>
    </div>
    <div class="export-section">
      <button class="btn-export" @click="showExportDialog = true">📑 导出报表</button>
    </div>
    <div class="tab-container">
      <!-- tab 切换 -->
      <div class="tabs">
        <button :class="{ active: activeTab === 'tab1' }" @click="activeTab = 'tab1'">总览</button>
        <button :class="{ active: activeTab === 'tab2' }" @click="activeTab = 'tab2'">模块对比</button>
      </div>

      <div class="tab-time">
        <button ref="triggerBtn" class="icon-btn" @click.stop="togglePopover">⏱</button>
        <div v-if="showPopover" class="popover" ref="popover">
          <div class="time-filter">
            <input type="date" v-model="startDate" class="date-input" />
            <span>至</span>
            <input type="date" v-model="endDate" class="date-input" />
            <select v-model="granularity" class="select-input">
              <option value="day">日</option>
              <option value="month">月</option>
              <option value="quarter">季度</option>
              <option value="year">年</option>
            </select>
          </div>
        </div>
      </div>

    </div>

    <!-- tab 内容 -->
    <div class="tab-content">
      <CashflowTab1 v-if="activeTab === 'tab1' && charts.length && periods.length"
                    :charts="charts" :periods="periods" @show-detail="goDetail" />

      <CashflowTab2 v-if="activeTab === 'tab2' && charts.length && periods.length"
                    :charts="charts" :periods="periods" @show-detail="goDetail" />
    </div>

    <!--导出弹窗-->
    <div v-if="showExportDialog" class="dialog-backdrop">
      <div class="dialog">
        <h3>导出现金流报表</h3>

        <label>导出格式：</label>
        <select v-model="exportFormat" class="select-input">
          <option value="excel">Excel</option>
          <option value="pdf">PDF</option>
        </select>

        <label>导出明细数据：</label>
        <select v-model="exportType" class="select-input">
          <option value="summary">否</option>
          <option value="detailed">是</option>
        </select>

        <label>模块：</label>
        <select v-model="exportModule" class="select-input">
          <option value="所有模块">所有模块</option>
          <option value="商户租金">商户租金</option>
          <option value="活动结算">活动结算</option>
          <option value="停车场收费">停车场收费</option>
          <option value="设备维修">设备维修</option>
          <option value="促销活动">促销活动</option>
          <option value="员工工资">员工工资</option>
        </select>

        <label>关联方类型：</label>
        <select v-model="exportRelatedPartyType" class="select-input">
          <option value="">不选择</option>
          <option value="商户">商户</option>
          <option value="合作方">合作方</option>
        </select>

        <label>关联方ID：</label>
        <input type="text"
               v-model="exportRelatedPartyId"
               class="select-input"
               placeholder="请输入关联方ID（可选）" />
        <div class="dialog-actions">
          <button @click="showExportDialog = false">取消</button>
          <button class="btn-export" @click="exportReport">确定导出</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import DashboardLayout from '@/components/BoardLayout.vue'
  import CashflowTab1 from './CashflowOverview.vue'
  import CashflowTab2 from './CashFlowChart.vue'
  import { ref, watch, onMounted, onBeforeUnmount } from 'vue'
  import { useRouter } from 'vue-router'
  import { useUserStore } from '@/user/user'
  import { generatePeriods } from './CashFlowData.js'
  import axios from 'axios'

  const userStore = useUserStore()
  const router = useRouter()

  const popover = ref(null)
  const triggerBtn = ref(null)
  const startDate = ref('2025-01-01')
  const endDate = ref('2025-12-31')
  const granularity = ref('month')
  const showPopover = ref(false)
  const periods = ref([])

  const togglePopover = () => (showPopover.value = !showPopover.value)
  const handleClickOutside = (e) => {
    if (
      showPopover.value &&
      popover.value &&
      !popover.value.contains(e.target) &&
      triggerBtn.value &&
      !triggerBtn.value.contains(e.target)
    ) {
      showPopover.value = false
    }
  }
  onMounted(() => document.addEventListener('click', handleClickOutside))
  onBeforeUnmount(() => document.removeEventListener('click', handleClickOutside))

  const activeTab = ref('tab1')
  const charts = ref([])
  const summary = ref({ TotalIncome: 0, TotalExpense: 0, NetFlow: 0 })
  const fetchCashflowOverview = async () => {
    try {
      const req = {
        startDate: startDate.value,
        endDate: endDate.value,
        accountID: userStore.token, 
        timeGranularity: granularity.value
      }

      const res = await axios.post('/api/cashflow/overview', req);
      if (res.data.Success) {
        const data = res.data.Data;   //response
        summary.value = data.Summary;
        charts.value = data.Modules;
        periods.value = generatePeriods(startDate.value, endDate.value, granularity.value);  //计算periods
      } else {
        console.error('获取数据失败:', res.data.Message || '无 message 字段', res.data);
      }
    } catch (err) {
      if (err.response) {
        console.error('后端返回错误:', err.response.status, err.response.data);
      } else if (err.request) {
        console.error('没有收到响应:', err.request);
      } else {
        console.error('请求错误:', err.message);
      }
      console.error('完整错误对象:', err);
    }
  }

  onMounted(fetchCashflowOverview)
  watch([startDate, endDate, granularity], fetchCashflowOverview)

  const goDetail = (moduleType) => {
    router.push({
      name: 'CashFlowDetail',
      params: { module: moduleType },
      query: {
        startDate: startDate.value,    
        endDate: endDate.value,
        timeGranularity: granularity.value
      }
    })
  }

  //导出功能
  const showExportDialog = ref(false)
  const exportFormat = ref('excel')
  const exportType = ref('summary')
  const exportModule = ref('所有模块')
  const exportRelatedPartyType = ref('')
  const exportRelatedPartyId = ref('')
  const exportReport = async () => {
    try {
      const req = {
        startDate: startDate.value,
        endDate: endDate.value,
        accountID: userStore.token,
        timeGranularity: granularity.value,
        moduleType: exportModule.value,
        exportType: exportType.value,
        format: exportFormat.value,
        relatedPartyType: exportRelatedPartyType.value,
        relatedPartyId: exportRelatedPartyId.value ? parseInt(exportRelatedPartyId.value) : null
      }
      const res = await axios.post('/api/cashflow/export', req, {
        responseType: 'blob'
      })
      const blob = new Blob([res.data], { type: res.headers['content-type'] })
      const fileName = decodeURIComponent(
        res.headers['content-disposition']?.split('filename=')[1] ||
        `现金流报表.${exportFormat.value === 'excel' ? 'xlsx' : 'pdf'}`
      )
      const link = document.createElement('a')
      link.href = URL.createObjectURL(blob)
      link.download = fileName
      link.click()
      URL.revokeObjectURL(link.href)
      showExportDialog.value = false
    } catch (err) {
      console.error('导出失败:', err)
      alert('导出失败，请稍后重试')
    }
  }
</script>


<style scoped>
  .home-content {
    background: #fff;
    padding: 20px;
    border-radius: 12px;
  }
  .welcome-section {
    display: flex;
    align-items: center;
    gap: 15px;
    margin-bottom: 20px;
  }
    .welcome-section .avatar {
      width: 60px;
      height: 60px;
      border-radius: 50%;
    }
  .summary-cards {
    display: flex;
    gap: 20px;
    margin-bottom: 20px;
  }
    .summary-cards .card {
      flex: 1;
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 15px;
      border-radius: 10px;
      color: #fff;
    }
    .summary-cards .icon {
      font-size: 24px;
    }
    .summary-cards h3 {
      margin: 0;
      font-size: 14px;
    }
    .summary-cards .value {
      font-size: 20px;
      font-weight: bold;
    }
    .summary-cards .trend {
      font-size: 12px;
    }
  .trend.up {
    color: #4caf50;
  }
  .trend.down {
    color: #f44336;
  }
  .card.income {
    background: linear-gradient(135deg, #4caf50, #81c784);
  }
  .card.expense {
    background: linear-gradient(135deg, #f44336, #e57373);
  }
  .card.net {
    background: linear-gradient(135deg, #2196f3, #64b5f6);
  }
  .tab-container {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12px;
  }
  .tabs {
    display: flex;
    gap: 10px;
    margin-bottom: 15px;
  }
    .tabs button {
      padding: 8px 16px;
      border: none;
      background: #f0f0f0;
      border-radius: 6px;
      cursor: pointer;
      transition: background 0.2s;
    }
      .tabs button.active {
        background: #007bff;
        color: #fff;
      }
      .tabs button:hover {
        background: #e0e0e0;
      }
  .tab-content {
    min-height: 400px;
  }
  .tab-time {
    position: relative;
  }
  .icon-btn {
    border: none;
    background: transparent;
    cursor: pointer;
    font-size: 18px;
  }
  .popover {
    position: absolute;
    top: 40px;
    right: 16px;
    background: white;
    border: 1px solid #ddd;
    border-radius: 8px;
    padding: 12px;
    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    z-index: 10;
  }
  .time-filter {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }
  .date-input,
  .select-input {
    padding: 6px 10px;
    border: 1px solid #ccc;
    border-radius: 6px;
    font-size: 14px;
  }
    .date-input:focus,
    .select-input:focus {
      border-color: #409eff;
      box-shadow: 0 0 4px rgba(64, 158, 255, 0.4);
      outline: none;
    }
  .export-section {
    margin: 10px 0 20px;
    text-align: right;
  }
  .btn-export {
    padding: 6px 14px;
    background: #4caf50;
    color: #fff;
    border: none;
    border-radius: 6px;
    cursor: pointer;
  }
    .btn-export:hover {
      background: #43a047;
    }
  .dialog-backdrop {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0,0,0,0.4);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 999;
  }
  .dialog {
    background: #fff;
    padding: 20px;
    border-radius: 10px;
    width: 320px;
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  .dialog-actions {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    margin-top: 10px;
  }
</style>
