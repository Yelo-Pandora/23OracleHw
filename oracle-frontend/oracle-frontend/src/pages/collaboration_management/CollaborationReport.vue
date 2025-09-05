<template>
  <div class="collaboration-report">
    <h2>合作方统计报表</h2>
    <div class="report-filters">
      <div class="filter-group">
        <label>快捷选择:</label>
        <select v-model="quickKey" @change="onQuickChange" class="quick-select">
          <option value="">- 选择快捷范围 -</option>
          <option value="all">有史以来</option>
          <option value="year">近一年</option>
          <option value="half">近半年</option>
          <option value="thisQuarter">本季度</option>
          <option value="lastQuarter">上季度</option>
          <option value="thisMonth">本月</option>
        </select>
      </div>
      <div class="filter-group">
        <label>开始日期:</label>
        <input type="date" v-model="filters.startDate" :max="filters.endDate || maxDate">
      </div>

      <div class="filter-group">
        <label>结束日期:</label>
        <input type="date" v-model="filters.endDate" :min="filters.startDate" :max="maxDate">
      </div>

      <div class="filter-group">
        <label>合作方名称:</label>
        <input
          type="text"
          v-model="filters.collaborationName"
          placeholder="请输入合作方名称"
        />
      </div>

  <button @click="generateReport" class="btn-generate">生成报表</button>
  <button @click="exportReport" class="btn-export" :disabled="!reportData.length">导出报表</button>
  <button @click="exportPDF" class="btn-export-pdf" :disabled="!reportData.length">导出为 PDF</button>
    </div>
    <h3>（点击表头以排序）</h3>
    <div v-if="loading" class="loading">生成报表中...</div>

    <div v-else-if="reportData.length === 0" class="no-data">
      <p>暂无数据，请选择日期范围后生成报表</p>
    </div>

    <div v-else class="report-results">
      <h3>统计结果</h3>

          <div class="visualization-section" v-if="reportData.length">
            <div class="chart-controls">
              <label>图表类型：</label>
              <!-- 不在生成报表时自动渲染画布，只有用户选择图表类型后才显示 -->
              <select v-model="selectedChartType" @change="onChartTypeChange">
                <option value="bar">选择图表类型</option>
                <option value="bar">总投资金额 (柱状图)</option>
                <option value="pie">活动次数分布 (饼图)</option>
              </select>
            </div>
            <!-- 只有当用户选择渲染图表（showChart=true）时才创建画布与容器，避免预留空白 -->
            <div class="chart-container" v-if="showChart">
              <canvas id="collabChart"></canvas>
            </div>
          </div>

      <div class="summary-cards">
        <div class="summary-card">
          <div class="card-title">合作方总数</div>
          <div class="card-value">{{ summary.totalCollaborations }}</div>
        </div>

        <div class="summary-card">
          <div class="card-title">活动总数</div>
          <div class="card-value">{{ summary.totalEvents }}</div>
        </div>

        <div class="summary-card">
          <div class="card-title">总投资金额</div>
          <div class="card-value">¥{{ summary.totalInvestment.toLocaleString() }}</div>
        </div>

        <div class="summary-card">
          <div class="card-title">平均活动收益</div>
          <div class="card-value">¥{{ summary.avgRevenue.toLocaleString() }}</div>
        </div>
      </div>

      <table class="report-table">
        <thead>
          <tr>
              <th @click="toggleSort('CollaborationId')" class="sortable">合作方ID <span v-if="sort.key==='CollaborationId'">{{ sort.order === 'asc' ? '▲' : (sort.order === 'desc' ? '▼' : '') }}</span></th>
              <th @click="toggleSort('EventCount')" class="sortable">活动次数 <span v-if="sort.key==='EventCount'">{{ sort.order === 'asc' ? '▲' : (sort.order === 'desc' ? '▼' : '') }}</span></th>
              <th @click="toggleSort('TotalInvestment')" class="sortable">总投资金额 <span v-if="sort.key==='TotalInvestment'">{{ sort.order === 'asc' ? '▲' : (sort.order === 'desc' ? '▼' : '') }}</span></th>
              <th @click="toggleSort('AvgRevenue')" class="sortable">平均活动收益 <span v-if="sort.key==='AvgRevenue'">{{ sort.order === 'asc' ? '▲' : (sort.order === 'desc' ? '▼' : '') }}</span></th>
          </tr>
        </thead>
        <tbody>
            <tr v-for="item in displayedReportData" :key="item.CollaborationId">
            <td>{{ item.CollaborationId }}</td>
            <td>{{ item.EventCount }}</td>
            <td>¥{{ item.TotalInvestment.toLocaleString() }}</td>
            <td>¥{{ item.AvgRevenue.toLocaleString() }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref, computed, onMounted, nextTick, onBeforeUnmount } from 'vue';
import { useUserStore } from '@/user/user';
import { useRouter } from 'vue-router';
import axios from 'axios';
import alert from '@/utils/alert';
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);

const userStore = useUserStore();
const router = useRouter();

// 当前日期（用于限制选择范围）
const maxDate = new Date().toISOString().split('T')[0]; // 当前日期
// 极早的默认开始日期
const EARLIEST_DATE = '1900-01-01';

const filters = reactive({
  // 默认从极早值到当前日期
  startDate: EARLIEST_DATE,
  endDate: maxDate,
  industry: ''
});

const reportData = ref([]);
const loading = ref(false);
// 快捷选择的 key
const quickKey = ref('');

// chart
const selectedChartType = ref('bar');
let collabChart = null;
// 控制是否显示图表容器（只有用户显式选择后才显示）
const showChart = ref(false);

// 检查登录状态
const checkAuth = () => {
  if (!userStore.token) {
    router.push('/login');
    return false;
  }
  return true;
};

const summary = computed(() => {
  if (reportData.value.length === 0) {
    return {
      totalCollaborations: 0,
      totalEvents: 0,
      totalInvestment: 0,
      avgRevenue: 0
    };
  }

  return {
    totalCollaborations: reportData.value.length,
    totalEvents: reportData.value.reduce((sum, item) => sum + item.EventCount, 0),
    totalInvestment: reportData.value.reduce((sum, item) => sum + item.TotalInvestment, 0),
    avgRevenue: reportData.value.reduce((sum, item) => sum + item.AvgRevenue, 0) / reportData.value.length
  };
});

const generateReport = async () => {
  if (!checkAuth()) return;

  // 验证日期
  if (!filters.startDate || !filters.endDate) {
    await alert('请选择开始日期和结束日期');
    return;
  }

  if (filters.startDate > filters.endDate) {
    await alert('开始日期不能晚于结束日期');
    return;
  }

  if (filters.endDate > maxDate) {
    await alert('结束日期不能晚于当前日期');
    return;
  }

  loading.value = true;

  try {
    const params = {
      operatorAccountId: userStore.token,
      startDate: filters.startDate,
      endDate: filters.endDate
    };

    if (filters.industry) {
      params.industry = filters.industry;
    }

    const response = await axios.get('/api/Collaboration/report', { params });
  reportData.value = response.data;
  // 不在这里渲染图表，避免预留空白。用户选择图表类型后才渲染。
  showChart.value = false;
  } catch (error) {
    console.error('生成报表失败:', error);
    if (error.response && error.response.status === 401) {
      await alert('登录已过期，请重新登录');
      userStore.logout();
      router.push('/login');
    } else {
      await alert('生成报表失败，' + (error || '，请稍后重试'));
    }
  } finally {
    loading.value = false;
  }
};

// 图表渲染: 基于 reportData 的 Collaboration 项目展示不同类型图表
const renderCharts = () => {
  // 销毁已有实例
  try {
    if (collabChart) {
      collabChart.destroy();
      collabChart = null;
    }
  } catch (e) {
    // ignore
  }

  if (!Array.isArray(reportData.value) || reportData.value.length === 0) return;

  // 取前 20 条以防过多标签
  const items = reportData.value.slice(0, 20);
  const labels = items.map(i => i.CollaborationId ? String(i.CollaborationId) : '-');
  const investmentData = items.map(i => Number(i.TotalInvestment || 0));
  const eventCountData = items.map(i => Number(i.EventCount || 0));
  const avgRevenueData = items.map(i => Number(i.AvgRevenue || 0));

  const ctx = document.getElementById('collabChart') && document.getElementById('collabChart').getContext
    ? document.getElementById('collabChart').getContext('2d')
    : null;
  if (!ctx) return;

  let config = null;
  switch (selectedChartType.value) {
    case 'pie':
      config = {
        type: 'pie',
        data: { labels, datasets: [{ data: eventCountData, backgroundColor: generateColors(labels.length) }] },
        options: { plugins: { title: { display: true, text: '合作方活动次数分布（前20）' } } }
      };
      break;
    case 'bar':
    default:
      config = {
        type: 'bar',
        data: { labels, datasets: [{ label: '总投资金额', data: investmentData, backgroundColor: '#2196F3' }] },
        options: { plugins: { title: { display: true, text: '合作方总投资金额（前20）' } }, responsive: true }
      };
      break;
  }

  try {
    collabChart = new Chart(ctx, config);
  } catch (e) {
    console.error('渲染合作方图表失败', e);
  }
};

// 当用户更改图表类型时的处理：显示容器并渲染
const onChartTypeChange = () => {
  // 如果用户选择为空或没有数据，不显示
  if (!selectedChartType.value || !reportData.value || reportData.value.length === 0) {
    // 销毁已有图表并隐藏
    try { if (collabChart) { collabChart.destroy(); collabChart = null; } } catch (e) {}
    showChart.value = false;
    return;
  }
  showChart.value = true;
  // 等待 DOM 中的 canvas 出现
  nextTick().then(() => {
    renderCharts();
  });
};

// 生成颜色数组
const generateColors = (n) => {
  const base = ['#4CAF50', '#F44336', '#FFC107', '#2196F3', '#9C27B0', '#00BCD4', '#8BC34A', '#FF9800', '#607D8B', '#E91E63'];
  const arr = [];
  for (let i = 0; i < n; i++) arr.push(base[i % base.length]);
  return arr;
};

onBeforeUnmount(() => {
  if (collabChart) {
    try { collabChart.destroy(); } catch (e) { /* ignore */ }
    collabChart = null;
  }
});

  const exportReport = async () => {
  // 这里实现导出功能，可以导出为Excel或PDF
  // 简单实现：导出为CSV
  const csvContent = [
    ['合作方ID', '活动次数', '总投资金额', '平均活动收益'],
    ...reportData.value.map(item => [
      item.CollaborationId,
      item.EventCount,
      item.TotalInvestment,
      item.AvgRevenue
    ])
  ].map(row => row.join(',')).join('\n');

  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  const url = URL.createObjectURL(blob);

  link.setAttribute('href', url);
  link.setAttribute('download', `合作方统计报表_${filters.startDate}_至_${filters.endDate}.csv`);
  link.style.visibility = 'hidden';

  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
};

const exportPDF = async () => {
  if (!reportData.value.length) return;
  const el = document.querySelector('.report-results');
  if (!el) {
    await alert('没有可导出的内容');
    return;
  }
  loading.value = true;

  // 辅助函数：创建一个隐藏容器并附加清理后的克隆元素
  const createCleanClone = (sourceEl) => {
    const clone = sourceEl.cloneNode(true);

    // 移除克隆元素中的所有iframe以避免跨域/克隆问题
    const iframes = clone.querySelectorAll('iframe');
    iframes.forEach(f => f.remove());

    // 可选：移除脚本
    clone.querySelectorAll('script').forEach(s => s.remove());

    // 内联最小计算样式以确保布局稳定性
    const copyStyles = (src, dst) => {
      try {
        const cs = window.getComputedStyle(src);
        if (cs) {
          for (let i = 0; i < cs.length; i++) {
            const prop = cs[i];
            dst.style.setProperty(prop, cs.getPropertyValue(prop), cs.getPropertyPriority(prop));
          }
        }
      } catch (e) {
        // 忽略计算样式错误
      }
    };

    const walk = (sNode, dNode) => {
      copyStyles(sNode, dNode);
      const sChildren = Array.from(sNode.children || []);
      const dChildren = Array.from(dNode.children || []);
      for (let i = 0; i < sChildren.length; i++) {
        walk(sChildren[i], dChildren[i]);
      }
    };

    try {
      walk(sourceEl, clone);
    } catch (e) {
      // 后备方案：如果内联失败，继续使用克隆元素
    }

    // 修复内联SVG：序列化并替换<svg>元素以保留视觉效果
    const svgs = clone.querySelectorAll('svg');
    svgs.forEach(svg => {
      try {
        const serializer = new XMLSerializer();
        const svgStr = serializer.serializeToString(svg);
        const encoded = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svgStr);
        const img = document.createElement('img');
        img.src = encoded;
        img.style.width = svg.getAttribute('width') || getComputedStyle(svg).width;
        img.style.height = svg.getAttribute('height') || getComputedStyle(svg).height;
        svg.parentNode.replaceChild(img, svg);
      } catch (e) {
        // 忽略SVG序列化错误
      }
    });

    // 隐藏交互控件（例如图表类型选择），使其不出现在导出的克隆中
    try {
      const controls = clone.querySelectorAll('.chart-controls');
      controls.forEach(c => { c.style.display = 'none'; });
    } catch (e) {
      // ignore
    }

    // 修复画布（canvas）：将原始 DOM 中的 canvas 内容序列化成图片并替换克隆中的 canvas
    try {
      const srcCanvases = Array.from(sourceEl.querySelectorAll('canvas'));
      const cloneCanvases = Array.from(clone.querySelectorAll('canvas'));

      srcCanvases.forEach((srcCanvas, idx) => {
        try {
          // 优先尝试使用 toDataURL（Chart.js 渲染的 canvas 通常可以）
          const dataUrl = srcCanvas.toDataURL('image/png');
          const img = document.createElement('img');
          img.src = dataUrl;
          // 保持尺寸一致（优先使用内联样式，否则使用属性或计算样式）
          img.style.width = srcCanvas.style.width || (srcCanvas.width ? srcCanvas.width + 'px' : getComputedStyle(srcCanvas).width);
          img.style.height = srcCanvas.style.height || (srcCanvas.height ? srcCanvas.height + 'px' : getComputedStyle(srcCanvas).height);
          try { copyStyles(srcCanvas, img); } catch (e) { /* ignore */ }

          const target = cloneCanvases[idx];
          if (target && target.parentNode) target.parentNode.replaceChild(img, target);
        } catch (e) {
          // 如果 toDataURL 失败（例如被 taint），则尽量保留原有 canvas（html2canvas 可能仍能捕获）
        }
      });
    } catch (e) {
      // 忽略替换 canvas 的任何错误
    }

    return clone;
  };

  // 创建隐藏容器
  const container = document.createElement('div');
  container.style.position = 'absolute';
  container.style.left = '-9999px';
  container.style.top = '0';
  container.style.width = `${el.offsetWidth}px`;
  container.style.background = '#ffffff';
  container.id = 'tmp-export-container';
  document.body.appendChild(container);

  try {
    const clone = createCleanClone(el);
    container.appendChild(clone);

    const html2canvas = (await import('html2canvas')).default;
    const { jsPDF } = await import('jspdf');

    // 渲染克隆元素
    const canvas = await html2canvas(clone, {
      scale: 2,
      useCORS: true,
      allowTaint: false,
      logging: false,
      imageTimeout: 20000,
      scrollX: 0,
      scrollY: -window.scrollY
    });

    const imgData = canvas.toDataURL('image/png');
    const pdf = new jsPDF('p', 'mm', 'a4');
    const pageWidth = pdf.internal.pageSize.getWidth();
    const pageHeight = pdf.internal.pageSize.getHeight();

    // 计算图像渲染尺寸，保持宽高比
    const imgProps = { width: canvas.width, height: canvas.height };
    let renderWidth = pageWidth - 20; // 毫米
    // 高度（毫米）= imgProps.height / (canvas.width / renderWidth_mm)
    const renderHeight = (imgProps.height * renderWidth) / imgProps.width;

    // 如果内容高度超过一页，进行分割
    if (renderHeight <= pageHeight - 20) {
      pdf.addImage(imgData, 'PNG', 10, 10, renderWidth, renderHeight);
    } else {
      // 将画布垂直分割成多页
      const canvasPageHeight = Math.floor((imgProps.width * (pageHeight - 20)) / renderWidth);
      let remainingHeight = imgProps.height;
      let page = 0;
      const tmpCanvas = document.createElement('canvas');
      const tmpCtx = tmpCanvas.getContext('2d');

      while (remainingHeight > 0) {
        const sy = page * canvasPageHeight;
        const sh = Math.min(canvasPageHeight, imgProps.height - sy);
        tmpCanvas.width = imgProps.width;
        tmpCanvas.height = sh;
        tmpCtx.clearRect(0, 0, tmpCanvas.width, tmpCanvas.height);
        tmpCtx.drawImage(canvas, 0, sy, imgProps.width, sh, 0, 0, imgProps.width, sh);
        const pageData = tmpCanvas.toDataURL('image/png');
        const pageRenderHeight = (sh * renderWidth) / imgProps.width;
        if (page > 0) pdf.addPage();
        pdf.addImage(pageData, 'PNG', 10, 10, renderWidth, pageRenderHeight);
        remainingHeight -= sh;
        page += 1;
      }
    }

    pdf.save(`合作方统计报表_${filters.startDate}_至_${filters.endDate}.pdf`);
  } catch (err) {
  console.error('导出PDF失败', err);
  await alert('导出PDF失败，请稍后重试');
  } finally {
    // 清理工作
    const tmp = document.getElementById('tmp-export-container');
    if (tmp) document.body.removeChild(tmp);
    loading.value = false;
  }
};

// Helper: format Date -> YYYY-MM-DD (local)
const pad = (n) => n.toString().padStart(2, '0');
const formatLocal = (d) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

// 设置快捷范围
const setQuickRange = (key) => {
  const today = new Date();
  let start, end = new Date();

  switch (key) {
    case 'all':
      filters.startDate = EARLIEST_DATE;
      filters.endDate = maxDate;
      return;

    case 'year':
      start = new Date(today);
      start.setFullYear(start.getFullYear() - 1);
      break;

    case 'half':
      start = new Date(today);
      start.setMonth(start.getMonth() - 6);
      break;

    case 'thisQuarter': {
      const month = today.getMonth();
      const qStartMonth = Math.floor(month / 3) * 3;
      start = new Date(today.getFullYear(), qStartMonth, 1);
      end = new Date(today.getFullYear(), qStartMonth + 3, 0);
      break;
    }

    case 'lastQuarter': {
      const month = today.getMonth();
      let qStartMonth = Math.floor(month / 3) * 3 - 3;
      let year = today.getFullYear();
      if (qStartMonth < 0) {
        qStartMonth += 12;
        year -= 1;
      }
      start = new Date(year, qStartMonth, 1);
      end = new Date(year, qStartMonth + 3, 0);
      break;
    }

    case 'thisMonth':
      start = new Date(today.getFullYear(), today.getMonth(), 1);
      end = new Date(today.getFullYear(), today.getMonth() + 1, 0);
      break;

    default:
      return;
  }

  // 如果没有在 switch 中设置 end，使用今天
  if (!end) end = today;

  // 保证 end 不超过当前日期
  const todayStr = formatLocal(new Date());
  const endStr = formatLocal(end) > todayStr ? todayStr : formatLocal(end);

  filters.startDate = start ? formatLocal(start) : EARLIEST_DATE;
  filters.endDate = endStr;
};

const onQuickChange = () => {
  if (!quickKey.value) return;
  setQuickRange(quickKey.value);
};

// 排序状态: key 为字段名, order 为 'asc' | 'desc' | ''
const sort = reactive({ key: '', order: '' });

const toggleSort = (key) => {
  if (sort.key !== key) {
    sort.key = key;
    sort.order = 'asc';
    return;
  }
  if (sort.order === 'asc') sort.order = 'desc';
  else if (sort.order === 'desc') { sort.key = ''; sort.order = ''; }
  else sort.order = 'asc';
};

// 根据 sort 对 reportData 进行排序并返回展示用数组
const displayedReportData = computed(() => {
  const list = Array.isArray(reportData.value) ? reportData.value.slice() : [];
  if (!sort.key) return list;
  const key = sort.key;
  const order = sort.order === 'asc' ? 1 : -1;
  list.sort((a, b) => {
    const va = a[key];
    const vb = b[key];
    if (va == null && vb == null) return 0;
    if (va == null) return -1 * order;
    if (vb == null) return 1 * order;
    const na = Number(va);
    const nb = Number(vb);
    if (!isNaN(na) && !isNaN(nb)) return (na - nb) * order;
    return String(va).localeCompare(String(vb)) * order;
  });
  return list;
});

// 组件挂载时检查登录状态
onMounted(() => {
  checkAuth();
});
</script>

<style scoped>
.collaboration-report {
  max-width: 1000px;
  margin: 0 auto;
}

.report-filters {
  display: flex;
  flex-wrap: wrap;
  gap: 15px;
  margin-bottom: 30px;
  padding: 20px;
  background-color: #f8f9fa;
  border-radius: 8px;
}

.filter-group {
  display: flex;
  flex-direction: column;
}

.filter-group label {
  margin-bottom: 5px;
  font-weight: bold;
}

.filter-group input,
.filter-group select,
.chart-controls select {
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.quick-select {
  min-width: 180px;
}

.btn-generate, .btn-export, .btn-export-pdf {
  padding: 8px 15px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  align-self: flex-end;
}

.btn-generate {
  background-color: #007bff;
  color: white;
}

.btn-export {
  background-color: #28a745;
  color: white;
}

.btn-export:disabled {
  background-color: #6c757d;
  cursor: not-allowed;
}

.btn-export-pdf {
  background-color: #17a2b8;
  color: white;
  margin-left: 10px;
}

.btn-export-pdf:disabled {
  background-color: #6c757d;
  cursor: not-allowed;
}

.summary-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.summary-card {
  padding: 20px;
  background-color: #f8f9fa;
  border-radius: 8px;
  text-align: center;
  box-shadow: 0 2px 5px rgba(0,0,0,0.1);
}

.card-title {
  font-size: 14px;
  color: #6c757d;
  margin-bottom: 10px;
}

.card-value {
  font-size: 24px;
  font-weight: bold;
  color: #007bff;
}

.report-table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 20px;
}

.report-table th,
.report-table td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #ddd;
}

.report-table th {
  background-color: #f8f9fa;
  font-weight: bold;
}

.loading, .no-data {
  text-align: center;
  padding: 40px;
  font-size: 18px;
  color: #6c757d;
}

.visualization-section { margin-bottom: 20px; }
.chart-controls { display:flex; align-items:center; gap:10px; margin-bottom:10px; }
.chart-container { position: relative; height: 360px; width: 100%; }
</style>
