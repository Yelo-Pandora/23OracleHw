<template>
  <div class="tab1-content">
    <!-- 总览折线图 -->
    <div class="overview-chart">
      <h3>整体现金流趋势</h3>
      <div ref="overviewRef" class="chart"></div>
    </div>

    <!-- 模块卡片，一行两个 -->
    <div class="module-card"
         v-for="(chart, index) in charts"
         :key="chart.ModuleType"
         @click="handleCardClick(chart.ModuleType)">
      <h4>{{ chart.ModuleType }}</h4>
      <p class="value">￥{{ chart.NetFlow.toLocaleString() }}</p>
      <div class="mini-chart" :ref="el => setMiniRef(el, index)"></div>
      <div class="module-stats">
        <span class="income">收入: ￥{{ chart.TotalIncome.toLocaleString() }}</span>
        <span class="expense">支出: ￥{{ chart.TotalExpense.toLocaleString() }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, watch, nextTick, onMounted, onBeforeUnmount } from 'vue';
  import * as echarts from 'echarts';
  import { toRaw } from 'vue';

  const props = defineProps({
    charts: Array,
    periods: Array
  });
  const emit = defineEmits(['show-detail']);
  const handleCardClick = (module) => {
    emit('show-detail', module);
  };

  // 总览图引用 & 实例
  const overviewRef = ref(null);
  let overviewChart = null;

  // 迷你图引用 & 实例
  const miniRefs = ref([]);
  const miniCharts = ref([]);
  const setMiniRef = (el, index) => {
    if (el) miniRefs.value[index] = el;
  };

  //初始化总览图
  const initOverviewChart = () => {
    if (!overviewRef.value || !props.charts.length) return;

    if (overviewChart) overviewChart.dispose();
    overviewChart = echarts.init(overviewRef.value);
    overviewChart.setOption({
      tooltip: {
        trigger: 'axis',
        formatter: function (params) {
          let result = params[0].axisValue + '<br/>';
          params.forEach(param => {
            result += `${param.seriesName}: ${param.value > 0 ? '+' : ''}${param.value.toLocaleString()}<br/>`;
          });
          return result;
        }
      },
      legend: {
        data: props.charts.map(c => c.ModuleType),
        bottom: 0
      },
      xAxis: {
        type: 'category',
        data: props.periods,
        axisLabel: { rotate: props.periods.length > 6 ? 45 : 0 }
      },
      yAxis: {
        type: 'value',
        axisLabel: { formatter: value => value.toLocaleString() }
      },
      grid: { left: '5%', right: '5%', top: '10%', bottom: '15%', containLabel: true },
      series: props.charts.map(c => {
        const dataMap = new Map(c.TimeSeriesDatas.map(d => [d.Period, d.NetFlow]));
        const seriesData = props.periods.map(p => dataMap.get(p) ?? 0);
        return {
          name: c.ModuleType,
          type: 'line',
          smooth: true,
          symbol: 'circle',
          symbolSize: 6,
          data: seriesData
        };
      })
    });
  };
  const initMiniCharts = () => {
    props.charts.forEach((c, index) => {
      const el = miniRefs.value[index];
      if (!el) return;

      if (miniCharts.value[index]) miniCharts.value[index].dispose();

      const dataMap = new Map(c.TimeSeriesDatas.map(d => [d.Period, d.NetFlow]));
      const seriesData = props.periods.map(p => dataMap.get(p) ?? 0);

      const chart = echarts.init(el);
      chart.setOption({
        xAxis: { type: 'category', show: false, data: props.periods },
        yAxis: { type: 'value', show: false },
        grid: { left: 0, right: 0, top: 0, bottom: 0 },
        series: [{
          data: seriesData,
          type: 'line',
          smooth: true,
          symbol: 'none',
          lineStyle: { color: '#3498db' }
        }]
      });

      miniCharts.value[index] = chart;
    });
  };

  const resizeCharts = () => {
    overviewChart?.resize();
    miniCharts.value.forEach(chart => chart?.resize());
  };
  watch([() => props.charts, () => props.periods], () => {
    if (props.charts?.length && props.periods?.length) {
      nextTick(() => {
        initOverviewChart();
        initMiniCharts();
      });
    }
  }, { deep: true, immediate: true });
  onMounted(() => {
    window.addEventListener('resize', resizeCharts);
  });

  onBeforeUnmount(() => {
    window.removeEventListener('resize', resizeCharts);
    overviewChart?.dispose();
    miniCharts.value.forEach(chart => chart?.dispose());
  });
</script>

<style scoped>
  .tab1-content {
    display: flex;
    flex-wrap: wrap;
    gap: 1rem;
  }
  .overview-chart {
    width: 100%;
    margin-bottom: 1rem;
  }
  .chart {
    width: 100%;
    height: 400px;
  }
  .module-card {
    background-color: #fff;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    border-radius: 8px;
    padding: 1rem;
    width: calc(50% - 1rem); 
    box-sizing: border-box;
    cursor: pointer;
  }
  .mini-chart {
    width: 100%;
    height: 60px;
    margin-top: 0.5rem;
  }
  .value {
    font-size: 1.2rem;
    font-weight: bold;
    margin: 0.5rem 0;
  }
  .module-stats {
    display: flex;
    justify-content: space-between;
    margin-top: 0.5rem;
    font-size: 0.8rem;
  }
  .income {
    color: #27ae60;
  }
  .expense {
    color: #e74c3c;
  }
</style>
