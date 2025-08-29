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

    <div v-if="loading" class="loading">生成报表中...</div>

    <div v-else-if="reportData.length === 0" class="no-data">
      <p>暂无数据，请选择日期范围后生成报表</p>
    </div>

    <div v-else class="report-results">
      <h3>统计结果</h3>

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
            <th>合作方ID</th>
            <th>活动次数</th>
            <th>总投资金额</th>
            <th>平均活动收益</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in reportData" :key="item.CollaborationId">
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
import { reactive, ref, computed, onMounted } from 'vue';
import { useUserStore } from '@/user/user';
import { useRouter } from 'vue-router';
import axios from 'axios';

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
    alert('请选择开始日期和结束日期');
    return;
  }

  if (filters.startDate > filters.endDate) {
    alert('开始日期不能晚于结束日期');
    return;
  }

  if (filters.endDate > maxDate) {
    alert('结束日期不能晚于当前日期');
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
  } catch (error) {
    console.error('生成报表失败:', error);
    if (error.response && error.response.status === 401) {
      alert('登录已过期，请重新登录');
      userStore.logout();
      router.push('/login');
    } else {
      alert('生成报表失败，请稍后重试');
    }
  } finally {
    loading.value = false;
  }
};

const exportReport = () => {
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
    alert('没有可导出的内容');
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

    // 将画布尺寸（px）转换为毫米用于PDF放置
    const pxPerMm = canvas.width / ((pageWidth) * (document.documentElement.clientWidth / document.documentElement.clientWidth || 1));

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
    alert('导出PDF失败，请稍后重试');
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
.filter-group select {
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
</style>
