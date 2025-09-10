<template>
  <div class="tab2-content">
    <h3>现金流趋势</h3>
    <div class="module-comparison">
      <div class="module-chart"
           v-for="(chart, index) in charts"
           :key="chart.ModuleType"
           @click="handleChartClick(chart.ModuleType)">
        <h4>{{ chart.ModuleType }}</h4>
        <div class="chart-stats">
          <span class="income">收入: {{ formatCurrency(chart.TotalIncome) }}</span>
          <span class="expense">支出: {{ formatCurrency(chart.TotalExpense) }}</span>
          <span class="net" :class="{ positive: chart.NetFlow >= 0, negative: chart.NetFlow < 0 }">
            净流: {{ formatCurrency(chart.NetFlow) }}
          </span>
        </div>
        <div class="chart" :ref="el => setChartRef(el, index)"></div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'
  import * as echarts from 'echarts'

  const props = defineProps({
    charts: Array,
    periods: Array
  })
  const emit = defineEmits(['show-detail'])
  const handleChartClick = (module) => emit('show-detail', module)

  const chartRefs = ref([])

  const chartInstances = ref([])

  const setChartRef = (el, index) => { if (el) chartRefs.value[index] = el }

  const formatCurrency = (value) => new Intl.NumberFormat('zh-CN', { style: 'currency', currency: 'CNY' }).format(value)

  const getColorForModule = (name) => ({
    "商户租金": '#5470c6',
    "活动结算": '#91cc75',
    "停车场收费": '#fac858',
    "设备维修": '#ee6666',
    "促销活动": '#73c0de',
    "员工工资": '#3ba272'
  }[name] || '#5470c6')

  // 初始化图表
  const initCharts = () => {
    if (!props.charts.length || !props.periods.length) return

    nextTick(() => {
      props.charts.forEach((chartData, index) => {
        const el = chartRefs.value[index]
        if (!el) return
        chartInstances.value[index]?.dispose()
        const chart = echarts.init(el)

        // 映射 TimeSeriesDatas → series.data
        const seriesData = props.periods.map(p => {
          const item = chartData.TimeSeriesDatas.find(d => d.Period === p)
          return item ? item.NetFlow : 0
        })

        chart.setOption({
          tooltip: { trigger: 'axis' },
          xAxis: { type: 'category', data: props.periods, axisLabel: { rotate: props.periods.length > 6 ? 45 : 0 } },
          yAxis: {
            type: 'value',
            min: () => Math.min(0, ...seriesData),
            max: () => Math.max(...seriesData) * 1.1,
            axisLabel: { formatter: v => v.toLocaleString() }
          },
          grid: { left: '10%', right: '5%', top: '15%', bottom: '15%', containLabel: true },
          series: [{
            name: chartData.ModuleType,
            type: 'line',
            smooth: seriesData.filter(v => v !== 0).length > 1,
            symbol: 'circle',
            symbolSize: seriesData.some(v => Math.abs(v) < 10000) ? 8 : 6,
            lineStyle: { width: seriesData.some(v => Math.abs(v) < 10000) ? 3 : 1.5, color: getColorForModule(chartData.ModuleType) },
            itemStyle: { color: getColorForModule(chartData.ModuleType) },
            data: seriesData
          }]
        })

        chartInstances.value[index] = chart
      })
    })
  }

  const resizeCharts = () => chartInstances.value.forEach(chart => chart?.resize())

  watch(() => [props.charts, props.periods], () => initCharts(), { deep: true })
  onMounted(() => { window.addEventListener('resize', resizeCharts); initCharts() })
  onBeforeUnmount(() => { window.removeEventListener('resize', resizeCharts); chartInstances.value.forEach(c => c?.dispose()) })
</script>

<style scoped>
  .tab2-content {
    padding: 1rem;
  }
  .module-comparison {
    display: flex;
    flex-wrap: wrap;
    gap: 1rem;
  }
  .module-chart {
    flex: 1 1 calc(50% - 1rem);
    background: #f8f8f8;
    padding: 1rem;
    border-radius: 8px;
    cursor: pointer;
  }
  .chart-stats {
    display: flex;
    justify-content: space-between;
    font-size: 0.85rem;
    margin-bottom: 0.5rem;
  }
  .income {
    color: #27ae60;
  }

  .expense {
    color: #e74c3c;
  }

  .net.positive {
    color: #27ae60;
  }

  .net.negative {
    color: #e74c3c;
  }

  .chart {
    width: 100%;
    height: 200px;
  }
</style>

<style scoped>
  .tab2-content {
    background: #fff;
    border-radius: 12px;
    padding: 24px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  }

  h3 {
    font-size: 22px;
    color: #2c3e50;
    margin-bottom: 20px;
    padding-bottom: 12px;
    border-bottom: 1px solid #eaeaea;
    font-weight: 600;
  }

  .module-comparison {
    display: grid;
    grid-template-columns: repeat(2, 1fr); 
    gap: 1rem; 
  }
  .module-chart {
    background: #f8f8f8;
    padding: 1rem;
    border-radius: 8px;
    cursor: pointer;
    transition: transform 0.2s, box-shadow 0.2s;
    box-sizing: border-box;
  }
    .module-chart:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
    }
    .module-chart h4 {
      text-align: center;
      margin-bottom: 12px;
      color: #2c3e50;
      font-size: 16px;
      font-weight: 500;
    }
  .chart-stats {
    display: flex;
    flex-direction: column;
    gap: 4px;
    margin-bottom: 10px;
    font-size: 12px;
  }
    .chart-stats span {
      display: flex;
      justify-content: space-between;
    }
  .income {
    color: #27ae60;
  }
  .expense {
    color: #e74c3c;
  }
  .net {
    font-weight: bold;
  }
  .positive {
    color: #27ae60;
  }
  .negative {
    color: #e74c3c;
  }
  .chart {
    height: 220px;
    width: 100%;
    min-height: 200px;
  }
  @media (max-width: 1200px) {
    .module-comparison {
      grid-template-columns: repeat(2, 1fr);
    }
  }
  @media (max-width: 768px) {
    .module-comparison {
      grid-template-columns: 1fr;
    }
  }
</style>
