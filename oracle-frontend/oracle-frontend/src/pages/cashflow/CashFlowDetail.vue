<template>
  <div class="cashflow-detail">

    <div class="detail-header">
      <h2>{{ criteria.ModuleType }} 现金流详情</h2>
      <button class="back-overview-btn" @click="goBackOverview">← 返回总览</button>
    </div>

    <p>时间范围：{{ criteria.StartDate }} ~ {{ criteria.EndDate }}</p>

    <table class="detail-table">
      <thead>
        <tr>
          <th>Period</th>
          <th>NetFlow</th>
          <th>操作</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(item, idx) in timeSeriesData" :key="idx">
          <td>{{ item.Period }}</td>
          <td>{{ item.NetFlow }}</td>
          <td>
            <button @click="showDetail(item)">查看明细</button>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- 现金流通用明细弹窗 -->
    <div v-if="detailModalVisible && !isSalaryMonthView" class="modal-overlay">
      <div class="modal">
        <h3>Period: {{ selectedPeriod.Period }}</h3>
        <button class="close-btn" @click="detailModalVisible = false">关闭</button>
        <input v-model="searchKeyword" placeholder="搜索日期/描述/关联方" />

        <table class="modal-table">
          <thead>
            <tr>
              <th>日期</th>
              <th>类型</th>
              <th>类别</th>
              <th>描述</th>
              <th>金额</th>
              <th>关联方</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(r, i) in paginatedRecords" :key="i">
              <td>{{ formatDate(r.Date) }}</td>
              <td>{{ r.Type }}</td>
              <td>{{ r.Category }}</td>
              <td>{{ r.Description }}</td>
              <td>{{ r.Amount }}</td>
              <td>{{ r.RelatedPartyType || '-' }} {{ r.RelatedPartyId || '' }}</td>
            </tr>
          </tbody>
        </table>

        <div class="pagination">
          <button @click="prevPage" :disabled="currentPage === 1">上一页</button>
          <span>{{ currentPage }}/{{ totalPages }}</span>
          <button @click="nextPage" :disabled="currentPage === totalPages">下一页</button>
        </div>
      </div>
    </div>

    <!-- 员工工资专用弹窗 -->
    <SalaryDetailModal v-if="isSalaryMonthView"
                       :show="detailModalVisible"
                       :year="selectedYear"
                       :month="selectedMonth"
                       :operatorAccount="userStore.token"
                       :editable="false"
                       @close="detailModalVisible = false"
                       @salaryUpdated="handleSalaryUpdated" />
  </div>
</template>
<script setup lang="ts">
  import { ref, reactive, computed, watch, onMounted } from 'vue'
  import { useRoute } from 'vue-router'
  import { useRouter } from 'vue-router'
  import axios from 'axios'
  import { useUserStore } from '@/user/user'
  //导入工资弹窗组件
  import SalaryDetailModal from '@/pages/employee_management/SalaryDetailModal.vue'

  const userStore = useUserStore()
  const route = useRoute()
  const moduleType = ref(route.params.module)
  const startDate = ref(route.query.startDate || '2025-01-01')
  const endDate = ref(route.query.endDate || '2025-12-31')
  const timeGranularity = ref(route.query.timeGranularity || 'month')

  const router = useRouter()
  const goBackOverview = () => {
    router.push({ name: 'CashFlowOverview' })
  }
  //请求参数
  const criteria = reactive({
    StartDate: startDate.value,
    EndDate: endDate.value,
    TimeGranularity: timeGranularity.value,
    ModuleType: moduleType.value,
    RelatedPartyType: '',
    RelatedPartyId: null,
    AccountID: userStore.token,
    IncludeDetails: true
  })

  const timeSeriesData = ref([])
  const allDetails = ref([])
  const detailModalVisible = ref(false)
  const selectedPeriod = ref({})
  const detailRecords = ref([])
  const searchKeyword = ref('')
  const currentPage = ref(1)
  const pageSize = 10

  // 计算属性：是否为工资模块
  const isSalaryModule = computed(() => {
    return criteria.ModuleType === '员工工资' || criteria.ModuleType === '工资支出'
  })
  const isSalaryMonthView = computed(() => {
    return isSalaryModule.value && criteria.TimeGranularity === 'month'
  })
  const selectedYear = computed(() => {
    if (!selectedPeriod.value.Period) return ''
    return selectedPeriod.value.Period.split('-')[0]
  })

  const selectedMonth = computed(() => {
    if (!selectedPeriod.value.Period) return ''
    return selectedPeriod.value.Period.split('-')[1]
  })

  const filteredRecords = computed(() => {
    if (!searchKeyword.value) return detailRecords.value
    const kw = searchKeyword.value.toLowerCase()

    return detailRecords.value.filter(d => {
      const description = (d.Description || '').toLowerCase()
      const dateFormatted = formatDateForSearch(d.Date)
      const relatedPartyId = (d.RelatedPartyId || '').toString().toLowerCase()
      const relatedPartyType = (d.RelatedPartyType || '').toString().toLowerCase()

      return (
        description.includes(kw) ||
        dateFormatted.includes(kw) ||
        relatedPartyId.includes(kw) ||
        relatedPartyType.includes(kw)
      )
    })
  })

  const formatDateForSearch = (dateStr: string) => {
    if (!dateStr) return ''
    const d = new Date(dateStr)
    return `${d.toLocaleDateString()} ${d.getFullYear()} ${d.getMonth() + 1} ${d.getDate()}`.toLowerCase()
  }

  const totalPages = computed(() => Math.ceil(filteredRecords.value.length / pageSize))
  const paginatedRecords = computed(() => {
    const start = (currentPage.value - 1) * pageSize
    return filteredRecords.value.slice(start, start + pageSize)
  })

  watch([searchKeyword, detailRecords], () => { currentPage.value = 1 })

  // 请求数据
  const fetchData = async () => {
    try {
      const resp = await axios.post('/api/CashFlow/detail', criteria)

      if (resp.data.Success) {
        const data = resp.data.Data
        timeSeriesData.value = data.TimeSeriesDatas
        allDetails.value = data.Details
      } else {
        alert(resp.data.Message || '获取数据失败')
      }
    } catch (err) {
      console.error('请求异常:', err)
      alert('请求异常')
    }
  }

  // 显示明细
  const showDetail = (periodItem: any) => {
    selectedPeriod.value = periodItem

    // 如果是工资模块且时间颗粒度不是月，显示提示信息
    if (isSalaryModule.value && criteria.TimeGranularity !== 'month') {
      alert('请切换到月粒度查看工资详情')
      return
    }
    if (isSalaryMonthView.value) {
      detailModalVisible.value = true
      return
    }

    selectedPeriod.value = periodItem
    // 根据时间颗粒度解析Period对应的日期范围
    const parsePeriod = (period: string, granularity: string) => {
      switch (granularity.toLowerCase()) {
        case "day": {
          const dateParts = period.split('-')
          const year = parseInt(dateParts[0])
          const month = parseInt(dateParts[1]) - 1 
          const day = parseInt(dateParts[2])
          const start = new Date(Date.UTC(year, month, day, 0, 0, 0, 0))
          const end = new Date(Date.UTC(year, month, day, 23, 59, 59, 999))
          return { start, end }
        }

        case "week": {
          const weekMatch = period.match(/(\d{4})-W(\d{2})/)
          if (!weekMatch) return { start: new Date(), end: new Date() }

          const year = parseInt(weekMatch[1])
          const week = parseInt(weekMatch[2])
          const firstDayOfYear = new Date(Date.UTC(year, 0, 1))
          const daysToFirstMonday = firstDayOfYear.getUTCDay() === 0 ? 1 : (8 - firstDayOfYear.getUTCDay())
          const startDate = new Date(Date.UTC(year, 0, 1 + (week - 1) * 7 + daysToFirstMonday))
          const endDate = new Date(startDate)
          endDate.setUTCDate(startDate.getUTCDate() + 6)
          startDate.setUTCHours(0, 0, 0, 0)
          endDate.setUTCHours(23, 59, 59, 999)

          return { start: startDate, end: endDate }
        }

        case "month": {
          const monthMatch = period.match(/(\d{4})-(\d{2})/)
          if (!monthMatch) return { start: new Date(), end: new Date() }

          const monthYear = parseInt(monthMatch[1])
          const month = parseInt(monthMatch[2]) - 1 
          const monthStart = new Date(Date.UTC(monthYear, month, 1))
          const monthEnd = new Date(Date.UTC(monthYear, month + 1, 0))
          monthStart.setUTCHours(0, 0, 0, 0)
          monthEnd.setUTCHours(23, 59, 59, 999)

          return { start: monthStart, end: monthEnd }
        }

        case "quarter": {
          const quarterMatch = period.match(/(\d{4})-Q(\d)/)
          if (!quarterMatch) return { start: new Date(), end: new Date() }

          const quarterYear = parseInt(quarterMatch[1])
          const quarter = parseInt(quarterMatch[2])
          const quarterStartMonth = (quarter - 1) * 3
          const quarterStart = new Date(Date.UTC(quarterYear, quarterStartMonth, 1))
          const quarterEnd = new Date(Date.UTC(quarterYear, quarterStartMonth + 3, 0))
          quarterStart.setUTCHours(0, 0, 0, 0)
          quarterEnd.setUTCHours(23, 59, 59, 999)

          return { start: quarterStart, end: quarterEnd }
        }

        case "year": {
          const yearValue = parseInt(period)
          const yearStart = new Date(Date.UTC(yearValue, 0, 1))
          const yearEnd = new Date(Date.UTC(yearValue, 11, 31))
          yearStart.setUTCHours(0, 0, 0, 0)
          yearEnd.setUTCHours(23, 59, 59, 999)

          return { start: yearStart, end: yearEnd }
        }

        default:
          return { start: new Date(), end: new Date() }
      }
    }

    //解析Period对应的日期范围
    const { start, end } = parsePeriod(periodItem.Period, criteria.TimeGranularity)
    detailRecords.value = allDetails.value.filter(d => {
      const recordDate = new Date(d.Date)
      return recordDate >= start && recordDate <= end
    })

    console.log(`选中 Period ${periodItem.Period} 的明细条数:`, detailRecords.value.length)
    console.log('detailRecords:', detailRecords.value)

    searchKeyword.value = ''
    currentPage.value = 1
    detailModalVisible.value = true
  }
  const prevPage = () => { if (currentPage.value > 1) currentPage.value-- }
  const nextPage = () => { if (currentPage.value < totalPages.value) currentPage.value++ }
  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr)
    return `${d.getUTCFullYear()}/${d.getUTCMonth() + 1}/${d.getUTCDate()}`
  }

  onMounted(fetchData)
</script>

<style scoped>
 .detail-header{
     display:flex;
     justify-content:space-between;
     align-items:center;
     margin-bottom:10px;
 }
  .back-overview-btn {
    padding: 6px 16px;
    border: none;
    border-radius: 6px;
    background: #4caf50;
    color: #fff;
    font-size: 14px;
    cursor: pointer;
    transition: background 0.2s, transform 0.18s;
  }
  .back-overview-btn:hover {
    background: #45a049;
    transform: scale(1.05);
  }
  .back-overview-btn:active {
    transform: scale(0.95);
  }
  .cashflow-detail {
    padding: 24px;
    background: #fff;
    border-radius: 16px;
    font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
    box-shadow: 0 4px 24px #0002;
  }
    .cashflow-detail h2 {
      margin-bottom: 6px;
      font-size: 22px;
      font-weight: bold;
      color: #333;
    }
    .cashflow-detail p {
      margin-bottom: 20px;
      color: #666;
    }
  .detail-table {
    width: 100%;
    border-collapse: separate;
    border-spacing: 1;
    border-radius: 12px;
    overflow: hidden;
    table-layout: fixed;
    background: #fff;
    box-shadow: 0 2px 6px rgba(0,0,0,0.1);
  }
    .detail-table th, .detail-table td {
      padding: 12px 10px;
      border-bottom: 1px solid #eee;
      text-align: center;
      word-break: break-all;
      white-space: pre-line;
      height: 48px;
      box-sizing: border-box;
      font-size: 15px;
      overflow-wrap: break-word;
      vertical-align: middle;
      transition: background 0.2s;
    }
    .detail-table th {
      background: #9cd1f6;
      font-weight: bold;
      position: sticky;
      top: 0;
      z-index: 2;
      color: #555;
    }
    .detail-table td {
      background: #f8fbfd;
    }
      .detail-table td:nth-child(even) {
        background: #eaf3fa;
      }
    .detail-table th:nth-child(even) {
      background: #b6e0fa;
    }
    .detail-table tr:hover {
      background: #e0f3ff !important;
    }
    .detail-table button {
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
      .detail-table button:hover {
        background: #ff9800;
        transform: scale(1.08);
      }
      .detail-table button:active {
        transform: scale(0.96);
      }
  .modal-overlay {
    position: fixed;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
    background: rgba(0,0,0,0.2);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
  }
  .modal {
    background: #fff;
    border-radius: 16px;
    padding: 24px;
    min-width: 1000px;
    max-width: 1200px;
    max-height: 80vh;
    overflow: auto;
    box-shadow: 0 4px 24px #0002;
    position: relative;
  }
    .modal h3 {
      margin-top: 0;
      margin-bottom: 18px;
      font-size: 20px;
      font-weight: bold;
      color: #333;
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
  .modal input {
    width: 100%;
    padding: 8px 14px;
    margin-bottom: 12px;
    font-size: 15px;
    border: 1px solid #b6e0fa;
    border-radius: 6px;
    outline: none;
    background: #f8fbfd;
  }
    .modal input:focus {
      border-color: #9cd1f6;
    }
  .modal-table {
    width: 100%;
    border-collapse: separate;
    border-spacing: 1;
    border-radius: 12px;
    overflow: hidden;
    table-layout: fixed;
  }
    .modal-table th, .modal-table td {
      padding: 12px 10px;
      border-bottom: 1px solid #eee;
      text-align: center;
      word-break: break-all;
      white-space: pre-line;
      height: 48px;
      box-sizing: border-box;
      font-size: 15px;
      overflow-wrap: break-word;
      vertical-align: middle;
      transition: background 0.2s;
    }
    .modal-table th {
      background: #9cd1f6;
      font-weight: bold;
      position: sticky;
      top: 0;
      z-index: 2;
      color: #555;
    }
    .modal-table td {
      background: #f8fbfd;
    }
      .modal-table td:nth-child(even) {
        background: #eaf3fa;
      }
    .modal-table th:nth-child(even) {
      background: #b6e0fa;
    }
    .modal-table tr:hover {
      background: #e0f3ff !important;
    }
  .pagination {
    margin-top: 15px;
    text-align: center;
  }
    .pagination button {
      padding: 4px 16px;
      margin: 0 6px;
      border: none;
      border-radius: 6px;
      background: #ffb74d;
      color: #fff;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
      transition: background 0.2s, transform 0.18s;
    }
      .pagination button:hover:not(:disabled) {
        background: #ff9800;
        transform: scale(1.08);
      }
      .pagination button:active {
        transform: scale(0.96);
      }
      .pagination button:disabled {
        background: #ccc;
        cursor: not-allowed;
        transform: none;
      }
    .pagination span {
      margin: 0 10px;
      font-size: 14px;
      color: #666;
    }
</style>
