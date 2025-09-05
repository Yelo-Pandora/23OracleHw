<template>
  <div class="parking-report">
    <div class="page-header">
      <button @click="goBack" class="back-btn">←</button>
      <h2>停车场运营报表</h2>
    </div>
    
    <!-- 报表参数设置 -->
    <div class="report-config">
      <div class="config-form">
        <div class="form-group">
          <label>时间范围：</label>
          <div class="date-range">
            <input v-model="reportParams.startDate" type="date" class="date-input"/>
            <span class="date-separator">至</span>
            <input v-model="reportParams.endDate" type="date" class="date-input"/>
          </div>
        </div>
        
        <div class="form-group">
          <label>停车场：</label>
          <select v-model="reportParams.areaId" class="area-select">
            <option value="">全部</option>
            <option v-for="lot in parkingLotOptions" :key="lot.AreaId" :value="String(lot.AreaId)">
              {{ lot.ParkingLotName || (`停车场${lot.AreaId}`) }}
            </option>
          </select>
        </div>

        <div class="form-group">
          <label>报表类型：</label>
          <select v-model="reportParams.reportType" class="area-select">
            <option value="1">日报</option>
            <option value="2">周报</option>
            <option value="3">月报</option>
          </select>
        </div>
        
        <div class="form-actions">
          <button @click="generateReport" class="generate-btn" :disabled="loading">
            {{ loading ? '生成中...' : '📊 生成报表' }}
          </button>
          <button @click="exportExcel" class="export-btn" :disabled="!reportData || loading">
            📥 导出CSV
          </button>
        </div>
      </div>
    </div>

    <!-- 报表结果 -->
    <div class="report-results" v-if="reportData">
      <!-- 总体统计 -->
      <div class="summary-stats">
        <div class="stat-card">
          <div class="stat-icon">🚗</div>
          <div class="stat-value">{{ reportData.totalParkingCount }}</div>
          <div class="stat-label">总停车次数</div>
        </div>
        <div class="stat-card">
          <div class="stat-icon">💰</div>
          <div class="stat-value">¥{{ (reportData.totalRevenue || 0).toFixed(2) }}</div>
          <div class="stat-label">总收入</div>
        </div>
        <div class="stat-card">
          <div class="stat-icon">⏰</div>
          <div class="stat-value">{{ (reportData.averageParkingHours || 0).toFixed(1) }} 小时</div>
          <div class="stat-label">平均停车时长</div>
        </div>

      </div>

      <!-- 图表区域 -->
      <div class="charts-grid">
        <div class="chart-container">
          <h4>每日收入</h4>
          <Line :data="dailyRevenueChartData" :options="chartOptions" />
        </div>
        <div class="chart-container">
          <h4>每日停车次数</h4>
          <Line :data="dailyCountChartData" :options="chartOptions" />
        </div>
      </div>

      <!-- 每日统计表格 -->
      <div class="daily-stats">
        <h4>详细数据</h4>
        <div class="table-container">
          <table class="stats-table">
            <thead>
              <tr>
                <th>日期</th>
                <th>停车次数</th>
                <th>收入 (元)</th>
                <th>平均时长 (小时)</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="daily in reportData.dailyStatistics" :key="daily.date">
                <td>{{ formatDate(daily.date) }}</td>
                <td>{{ daily.parkingCount || 0 }}</td>
                <td>{{ (daily.revenue || 0).toFixed(2) }}</td>
                <td>{{ (daily.averageHours || 0).toFixed(1) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div class="no-data" v-if="hasSearched && !reportData && !loading">
      <p>该时间段内无停车记录，请尝试扩大时间范围。</p>
    </div>

    <div class="error-message" v-if="errorMessage">
      <p>{{ errorMessage }}</p>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { Line } from 'vue-chartjs'
import { Chart as ChartJS, Title, Tooltip, Legend, CategoryScale, LinearScale, PointElement, LineElement } from 'chart.js'

ChartJS.register(Title, Tooltip, Legend, CategoryScale, LinearScale, PointElement, LineElement)

const router = useRouter()

const reportParams = ref({
  startDate: '',
  endDate: '',
  areaId: '',
  reportType: '1', // 1=日报, 2=周报, 3=月报
})

const reportData = ref(null)
const loading = ref(false)
const hasSearched = ref(false)
const errorMessage = ref('')

// 动态加载停车场下拉选项
const parkingLotOptions = ref([])
const loadParkingLotOptions = async () => {
  try {
    const resp = await fetch('/api/Parking/ParkingLots')
    if (resp.ok) {
      const data = await resp.json()
      const list = (data.data || data.Data || data) || []
      parkingLotOptions.value = list
    } else {
      console.error('加载停车场列表失败，状态码:', resp.status)
    }
  } catch (e) {
    console.error('加载停车场列表出错:', e)
  }
}

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  scales: {
    y: {
      beginAtZero: true
    }
  }
}

const dailyRevenueChartData = computed(() => {
  if (!reportData.value || !reportData.value.dailyStatistics) {
    return { labels: [], datasets: [] }
  }
  const labels = reportData.value.dailyStatistics.map(d => formatDate(d.date))
  return {
    labels,
    datasets: [
      {
        label: '每日收入 (元)',
        backgroundColor: '#42A5F5',
        borderColor: '#42A5F5',
        data: reportData.value.dailyStatistics.map(d => d.revenue)
      }
    ]
  }
})

const dailyCountChartData = computed(() => {
  if (!reportData.value || !reportData.value.dailyStatistics) {
    return { labels: [], datasets: [] }
  }
  const labels = reportData.value.dailyStatistics.map(d => formatDate(d.date))
  return {
    labels,
    datasets: [
      {
        label: '每日停车次数',
        backgroundColor: '#FFA726',
        borderColor: '#FFA726',
        data: reportData.value.dailyStatistics.map(d => d.parkingCount)
      }
    ]
  }
})

const hourlyTrafficChartData = computed(() => {
  if (!reportData.value || !reportData.value.hourlyTraffic) {
    return { labels: [], datasets: [] }
  }
  const labels = reportData.value.hourlyTraffic.map(h => `${h.hour}:00`)
  return {
    labels,
    datasets: [
      {
        label: '车流量',
        backgroundColor: '#66BB6A',
        data: reportData.value.hourlyTraffic.map(h => h.vehicleCount)
      }
    ]
  }
})

// 将可能的 TimeSpan 格式(如 "03:30:00")转成小时数，或直接解析数值
const toHours = (val) => {
  if (val == null) return 0
  if (typeof val === 'number') return val
  const s = String(val)
  if (/^\d{1,2}:\d{2}(:\d{2})?$/.test(s)) {
    const parts = s.split(':').map(x => parseInt(x, 10) || 0)
    const h = parts[0] || 0
    const m = parts[1] || 0
    const sec = parts[2] || 0
    return h + m / 60 + sec / 3600
  }
  const n = parseFloat(s)
  return isNaN(n) ? 0 : n
}

const generateReport = async () => {
  if (!reportParams.value.startDate || !reportParams.value.endDate) {
    alert('请选择统计时间范围')
    return
  }

  try {
    loading.value = true
    hasSearched.value = true
    errorMessage.value = ''
    reportData.value = null

    // 根据报表类型自动对齐时间范围
    const rt = parseInt(reportParams.value.reportType || '1', 10)
    const alignRange = (startStr, endStr, type) => {
      const toDate = (s) => new Date(`${s}T00:00:00`)
      let start = toDate(startStr)
      let end = toDate(endStr)
      if (type === 2) {
        // 周报：start 对齐到周一，end 对齐到周日
        const day = (start.getDay() + 6) % 7 // 周一=0 ... 周日=6
        start.setDate(start.getDate() - day)
        const dayEnd = (end.getDay() + 6) % 7
        end.setDate(end.getDate() + (6 - dayEnd))
      } else if (type === 3) {
        // 月报：start 到当月1日，end 到当月最后一天
        start = new Date(start.getFullYear(), start.getMonth(), 1)
        end = new Date(end.getFullYear(), end.getMonth() + 1, 0)
      }
      const pad = (n) => String(n).padStart(2, '0')
      const toYMD = (d) => `${d.getFullYear()}-${pad(d.getMonth()+1)}-${pad(d.getDate())}`
      // 同步回输入框展示（便于用户看到对齐结果）
      reportParams.value.startDate = toYMD(start)
      reportParams.value.endDate = toYMD(end)
      // 返回完整时间
      return {
        start: `${toYMD(start)}T00:00:00`,
        end: `${toYMD(end)}T23:59:59`
      }
    }
    const { start, end } = alignRange(reportParams.value.startDate, reportParams.value.endDate, rt)

    const requestData = {
      startDate: start,
      endDate: end,
      areaId: reportParams.value.areaId ? parseInt(reportParams.value.areaId) : null,
      operatorAccount: 'admin',
      reportType: rt
    }

    const response = await fetch('/api/Parking/OperationReport', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(requestData)
    })

    if (response.ok) {
      const data = await response.json()
      if (data.success || data.Success) {
        const result = data.data || data.Data
        reportData.value = {
          totalParkingCount: result.totalParkingCount || result.TotalParkingCount || 0,
          totalRevenue: parseFloat(result.totalRevenue || result.TotalRevenue || 0),
          averageParkingHours: toHours(result.averageParkingHours || result.AverageParkingHours || 0),
          peakUtilizationRate: parseFloat(result.peakUtilizationRate || result.PeakUtilizationRate || 0),
          dailyStatistics: (result.dailyStatistics || result.DailyStatistics || []).map(daily => ({
            date: daily.date || daily.Date,
            parkingCount: daily.parkingCount || daily.ParkingCount || 0,
            revenue: parseFloat(daily.revenue || daily.Revenue || 0),
            averageHours: toHours(daily.averageHours || daily.AverageHours || daily.AverageParkingHours || 0),
            peakUtilizationRate: parseFloat(daily.peakUtilizationRate || daily.PeakUtilizationRate || 0)
          })),
          hourlyTraffic: (result.hourlyTraffic || result.HourlyTraffic || []).map(hour => ({
            hour: hour.hour || hour.Hour || 0,
            vehicleCount: hour.vehicleCount || hour.VehicleCount || 0
          }))
        }
      } else {
        errorMessage.value = data.message || data.Message || '生成报表失败'
      }
    } else {
      const errorData = await response.json()
      errorMessage.value = errorData.error || '生成报表失败'
    }
  } catch (error) {
    console.error('生成报表出错:', error)
    errorMessage.value = '生成报表时发生错误'
  } finally {
    loading.value = false
  }
}

const exportExcel = async () => {
  if (!reportData.value) {
    alert('请先生成报表')
    return
  }
  try {
    const rows = []
    // 标题
    rows.push(['日期','停车次数','收入(元)','平均时长(小时)'])
    // 明细
    ;(reportData.value.dailyStatistics || []).forEach(d => {
      rows.push([
        formatDate(d.date),
        String(d.parkingCount || 0),
        (Number(d.revenue || 0)).toFixed(2),
        (Number(d.averageHours || 0)).toFixed(1)
      ])
    })
    // 汇总
    rows.push([])
    rows.push(['汇总','总停车次数','总收入(元)','平均停车时长(小时)'])
    rows.push([
      '',
      String(reportData.value.totalParkingCount || 0),
      (Number(reportData.value.totalRevenue || 0)).toFixed(2),
      (Number(reportData.value.averageParkingHours || 0)).toFixed(1)
    ])

    // 生成CSV文本（包含BOM，避免中文乱码）
    const csvContent = '\ufeff' + rows.map(r => r.map(v => {
      const s = String(v ?? '')
      // 若包含引号/逗号/换行，用双引号包裹并转义引号
      if (/[",\n]/.test(s)) {
        return '"' + s.replace(/"/g, '""') + '"'
      }
      return s
    }).join(',')).join('\n')

    // 触发下载
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    const start = reportParams.value.startDate || ''
    const end = reportParams.value.endDate || ''
    a.download = `停车场报表_${start}_${end}.csv`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  } catch (e) {
    console.error('导出CSV失败:', e)
    alert('导出失败，请重试')
  }
}

const goBack = () => router.push('/parking-management')

const formatDate = (dateString) => {
  if (!dateString) return '-'
  return new Date(dateString).toLocaleDateString('zh-CN')
}

onMounted(() => {
  const today = new Date()
  const weekAgo = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000)
  reportParams.value.endDate = today.toISOString().split('T')[0]
  reportParams.value.startDate = weekAgo.toISOString().split('T')[0]
  // 先拉取停车场下拉选项
  loadParkingLotOptions()
  // 默认生成一次（全部停车场）
  generateReport()
})
</script>

<style scoped>
.parking-report {
  padding: 20px;
  max-width: 1400px;
  margin: 0 auto;
  background-color: #f4f7f6;
}

.page-header {
  display: flex;
  align-items: center;
  gap: 15px;
  margin-bottom: 20px;
}

.back-btn {
  background: #007bff;
  color: white;
  border: none;
  border-radius: 50%;
  width: 40px;
  height: 40px;
  font-size: 18px;
  cursor: pointer;
  transition: all 0.2s;
}

.back-btn:hover { background: #0056b3; }

h2, h3, h4 {
  color: #333;
  margin-bottom: 20px;
}

.report-config {
  background: white;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.config-form {
  display: grid;
  grid-template-columns: 1fr 1fr 200px;
  gap: 20px;
  align-items: end;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.form-group label {
  font-weight: bold;
  color: #333;
}

.date-range {
  display: flex;
  align-items: center;
  gap: 10px;
}

.date-input, .area-select {
  padding: 10px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}

.form-actions {
  display: flex;
  gap: 15px;
}

.generate-btn, .export-btn {
  padding: 12px 24px;
  border: none;
  border-radius: 6px;
  font-size: 16px;
  cursor: pointer;
  transition: all 0.2s;
}

.generate-btn { background: #28a745; color: white; }
.export-btn { background: #17a2b8; color: white; }

.report-results {
  background: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.summary-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.stat-card {
  background: #f8f9fa;
  border-radius: 8px;
  padding: 20px;
  text-align: center;
  border: 1px solid #dee2e6;
}

.stat-icon { font-size: 24px; margin-bottom: 10px; }
.stat-value { font-size: 28px; font-weight: bold; color: #007bff; }
.stat-label { font-size: 14px; color: #6c757d; }

.charts-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  margin-bottom: 30px;
}

.chart-container {
  height: 400px;
  padding: 20px;
  border: 1px solid #ddd;
  border-radius: 8px;
}

.daily-stats {
  margin-top: 20px;
}

.table-container {
  overflow-x: auto;
}

.stats-table {
  width: 100%;
  border-collapse: collapse;
}

.stats-table th, .stats-table td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #ddd;
}

.stats-table th { background: #f8f9fa; }

.no-data, .error-message {
  text-align: center;
  padding: 40px;
  background: #f8f9fa;
  border-radius: 8px;
}
</style>
