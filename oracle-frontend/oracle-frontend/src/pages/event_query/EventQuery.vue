<template>
  <div class="placeholder-page">
    <div class="header">
      <h2>活动查询</h2>
    </div>

    <div class="filter-container">
      <el-form :model="filterForm" class="filter-form">
        <el-row :gutter="20">
          <el-col :span="8">
            <el-form-item label="活动状态">
              <el-select v-model="filterForm.status" placeholder="请选择活动状态" clearable style="width: 100%;">
                <el-option label="未开始" value="upcoming"></el-option>
                <el-option label="进行中" value="ongoing"></el-option>
                <el-option label="已结束" value="completed"></el-option>
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="10">
            <el-form-item label="活动日期">
              <el-date-picker
                v-model="filterForm.dateRange"
                type="daterange"
                range-separator="至"
                start-placeholder="开始日期"
                end-placeholder="结束日期"
                value-format="yyyy-MM-dd"
                clearable
                style="width: 100%;"
              >
              </el-date-picker>
            </el-form-item>
          </el-col>
          <el-col :span="6" class="button-col">
            <el-form-item>
              <!-- 查询按钮 -->
              <button
                type="button"
                @click="handleQueryClick"
                :disabled="queryLoading"
                class="custom-button custom-button--primary"
                :class="{ 'is-loading': queryLoading, 'is-clicked': isQueryClicked }"
              >
                <span v-if="!queryLoading">查询</span>
                <span v-else>查询中...</span>
              </button>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
    </div>

    <el-tabs v-model="activeTab" @tab-change="handleTabChange">
      <el-tab-pane label="全部活动" name="all"></el-tab-pane>
      <el-tab-pane label="场地活动" name="venue"></el-tab-pane>
      <el-tab-pane label="促销活动" name="promotion"></el-tab-pane>
    </el-tabs>

    <div v-if="loading" class="empty-state">
      <i class="el-icon-loading"></i>
      <p>加载中...</p>
    </div>

    <div v-else-if="activities.length === 0" class="empty-state">
      <i class="el-icon-date"></i>
      <p>暂无活动数据</p>
    </div>

    <div v-else class="card-container">
      <el-row :gutter="20">
        <el-col v-for="activity in activities" :key="activity.EVENT_ID" :span="8" style="margin-bottom: 20px;">
          <div class="activity-card">
            <div class="card-header">
              <div class="card-title">{{ activity.EVENT_NAME }}</div>
            </div>
            <div class="card-content">
              <div class="card-detail">
                <i class="el-icon-time detail-icon"></i>
                <span class="detail-text">时间: {{ activity.EVENT_START }} 至 {{ activity.EVENT_END }}</span>
              </div>
              <div class="card-detail">
                <i class="el-icon-document detail-icon"></i>
                <span class="detail-text">描述: {{ activity.Description }}</span>
              </div>
              <div class="card-detail" v-if="activity.Cost !== undefined">
                <i class="el-icon-money detail-icon"></i>
                <span class="detail-text">费用: {{ activity.Cost }}</span>
              </div>
            </div>
            <div class="card-footer">
              <span :class="getStatusClass(activity.status)">
                {{ getStatusText(activity.status) }}
              </span>
            </div>
          </div>
        </el-col>
      </el-row>
    </div>

    <div class="pagination-wrapper">
      <el-pagination
        v-model:current-page="currentPage"
        v-model:page-size="pageSize"
        :page-sizes="[6, 12, 24]"
        :small="true"
        :background="true"
        layout="total, sizes, prev, pager, next, jumper"
        :total="total"
        @size-change="handleSizeChange"
        @current-change="handleCurrentChange"
        class="custom-pagination"
      />
    </div>
  </div>
</template>

<script setup>
import axios from 'axios';
import { ref, onMounted } from 'vue';
import { ElMessage } from 'element-plus';

const filterForm = ref({
  status: '',
  dateRange: []
});

const activeTab = ref('all');
const activities = ref([]);
const loading = ref(false); // 整体加载状态
const queryLoading = ref(false); // 查询按钮加载状态
const currentPage = ref(1);
const pageSize = ref(6);
const total = ref(0);

// 按钮点击效果状态
const isQueryClicked = ref(false);

function calcStatus(activity) {
  const now = new Date();
  const start = new Date(activity.EVENT_START);
  const end = new Date(activity.EVENT_END);
  if (now < start) return 'upcoming';
  if (now >= start && now <= end) return 'ongoing';
  return 'completed';
}

const getStatusClass = (status) => {
  return `status-${status}`;
};

// 查询按钮点击处理函数
const handleQueryClick = async () => {
  if (queryLoading.value) return; // 防止重复点击

  isQueryClicked.value = true; // 触发点击效果
  queryLoading.value = true;   // 触发加载状态
  loading.value = true;        // 触发整体加载状态

  // 点击效果持续时间
  setTimeout(() => {
    isQueryClicked.value = false;
  }, 150);
  try {
    // 使用模拟数据展示效果，正式使用删除，使用下方api调用
    // const response = await axios.get('/api/SaleEvent');
    // let list = response.data.map(a => ({
    //   ...a,
    //   status: calcStatus(a)
    // }));
    
    // 模拟 API 调用延迟
    await new Promise(resolve => setTimeout(resolve, 800));
    const mockData = [
      { EVENT_ID: 1, EVENT_NAME: '春季大促', EVENT_START: '2023-03-01', EVENT_END: '2023-03-15', Description: '全场商品8折起', Cost: 0 },
      { EVENT_ID: 2, EVENT_NAME: '新品发布会', EVENT_START: '2023-05-20', EVENT_END: '2023-05-21', Description: '邀请您参加我们的新品发布会', Cost: 0 },
      { EVENT_ID: 3, EVENT_NAME: '周年庆典', EVENT_START: '2022-12-01', EVENT_END: '2022-12-31', Description: '公司成立十周年，感恩回馈', Cost: 0 },
      { EVENT_ID: 4, EVENT_NAME: '夏日清凉节', EVENT_START: '2023-07-01', EVENT_END: '2023-07-15', Description: '夏季专属折扣', Cost: 0 },
      { EVENT_ID: 5, EVENT_NAME: '中秋赏月会', EVENT_START: '2023-09-29', EVENT_END: '2023-10-01', Description: '中秋佳节，共赏明月', Cost: 0 },
      { EVENT_ID: 6, EVENT_NAME: '年终清仓', EVENT_START: '2023-12-20', EVENT_END: '2023-12-31', Description: '年终大促，清仓甩卖', Cost: 0 },
      { EVENT_ID: 7, EVENT_NAME: '会员日特惠', EVENT_START: '2023-06-18', EVENT_END: '2023-06-18', Description: '会员尊享额外9折', Cost: 0 },
      { EVENT_ID: 8, EVENT_NAME: '开学季大促', EVENT_START: '2023-08-15', EVENT_END: '2023-09-10', Description: '学生专享，学习用品特价', Cost: 0 },
    ];
    const response = { data: mockData };

    let list = response.data.map(a => ({
      ...a,
      status: calcStatus(a)
    }));

    if (filterForm.value.status) {
      list = list.filter(a => a.status === filterForm.value.status);
    }
    if (filterForm.value.dateRange.length === 2) {
      const [start, end] = filterForm.value.dateRange;
      list = list.filter(a => a.EVENT_START >= start && a.EVENT_END <= end);
    }

    // 模拟分页逻辑
    total.value = list.length;
    const start = (currentPage.value - 1) * pageSize.value;
    const end = start + pageSize.value;
    activities.value = list.slice(start, end);

  } catch (err) {
    ElMessage.error('获取活动数据失败，请稍后重试');
    console.error(err);
  } finally {
    queryLoading.value = false;
    loading.value = false;
  }
};

// 注意：resetFilter 函数已移除，因为没有了重置按钮

const handleTabChange = () => {
  currentPage.value = 1;
  handleQueryClick(); // 切换Tab时也触发查询
};

const handleSizeChange = (newSize) => {
  pageSize.value = newSize;
  currentPage.value = 1;
  handleQueryClick(); // 更改页面大小时触发查询
};

const handleCurrentChange = (newPage) => {
  currentPage.value = newPage;
  handleQueryClick(); // 更改页码时触发查询
};

const getStatusText = (status) => {
  const statusMap = {
    upcoming: '未开始',
    ongoing: '进行中',
    completed: '已结束'
  };
  return statusMap[status] || '未知状态';
};

onMounted(() => {
  handleQueryClick(); // 页面加载时自动查询
});
</script>

<style scoped>
.placeholder-page {
  padding: 20px;
  background-color: #f5f7fa;
  min-height: 100vh;
  box-sizing: border-box;
}

.header {
  margin-bottom: 20px;
}
.header h2 {
  color: #303133;
  font-size: 24px;
  font-weight: 600;
  margin: 0;
}

.filter-container {
  background-color: #ffffff;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
  margin-bottom: 20px;
}

/* 按钮列和按钮样式 */
.button-col {
  display: flex;
  align-items: flex-end;
}

/* 自定义按钮基础样式 */
.custom-button {
  display: inline-flex;
  justify-content: center;
  align-items: center;
  padding: 8px 16px;
  font-size: 14px;
  font-weight: 500;
  line-height: 1;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  background-color: #ffffff;
  color: #606266;
  cursor: pointer;
  transition: all 0.2s ease;
  outline: none;
  /* margin-right: 10px; 移除了按钮之间的右边距 */
  position: relative; /* 为伪元素定位做准备 */
  overflow: hidden; /* 防止波纹溢出 */
}

/* 主要按钮样式 */
.custom-button--primary {
  background-color: #409eff;
  border-color: #409eff;
  color: #ffffff;
}

/* 按钮悬停效果 */
.custom-button:hover {
  background-color: #f5f7fa;
  border-color: #c0c4cc;
  color: #606266;
}
.custom-button--primary:hover {
  background-color: #66b1ff;
  border-color: #66b1ff;
  color: #ffffff;
}

/* 按钮按下/聚焦效果 */
.custom-button:active,
.custom-button:focus {
  border-color: #409eff;
  box-shadow: 0 0 0 2px rgba(64, 158, 255, 0.2);
}
.custom-button--primary:active,
.custom-button--primary:focus {
  background-color: #3a8ee6;
  border-color: #3a8ee6;
  box-shadow: 0 0 0 2px rgba(64, 158, 255, 0.2);
}

/* 按钮禁用状态 */
.custom-button:disabled {
  cursor: not-allowed;
  opacity: 0.7;
  background-color: #ffffff;
  border-color: #ebeef5;
  color: #c0c4cc;
}
.custom-button--primary:disabled {
  background-color: #a0cfff;
  border-color: #a0cfff;
  color: #ffffff;
}

/* 自定义点击效果类 - 波纹动画 */
.custom-button.is-clicked::after {
  content: "";
  position: absolute;
  top: 50%;
  left: 50%;
  width: 0;
  height: 0;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.5); /* 白色半透明波纹 */
  transform: translate(-50%, -50%);
  animation: ripple 0.4s linear;
}

/* 波纹动画关键帧 */
@keyframes ripple {
  to {
    width: 200%;
    height: 200%;
    opacity: 0;
  }
}

/* 查询按钮加载状态 */
.custom-button.is-loading {
  pointer-events: none;
  opacity: 0.8;
}


.card-container {
  margin-bottom: 20px;
}

.activity-card {
  background-color: #ffffff;
  border: 1px solid #ebeef5;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  transition: all 0.3s cubic-bezier(0.645, 0.045, 0.355, 1);
  height: 100%;
  display: flex;
  flex-direction: column;
}

.activity-card:hover {
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.15);
  transform: translateY(-2px);
  border-color: #dcdfe6;
}

.card-header {
  padding: 16px 20px 10px;
  border-bottom: 1px solid #eee;
  background-color: #fafafa;
}
.card-title {
  font-size: 18px;
  font-weight: 500;
  color: #303133;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.card-content {
  padding: 16px 20px;
  flex-grow: 1;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.card-detail {
  display: flex;
  align-items: flex-start;
  font-size: 14px;
  color: #606266;
}
.detail-icon {
  margin-right: 8px;
  margin-top: 1px;
  color: #909399;
  flex-shrink: 0;
}
.detail-text {
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.5;
}

.card-footer {
  padding: 12px 20px;
  border-top: 1px solid #eee;
  background-color: #fafafa;
  text-align: right;
}
.status-upcoming {
  color: #909399;
  background-color: #f4f4f5;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
}
.status-ongoing {
  color: #67c23a;
  background-color: #f0f9ff;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
}
.status-completed {
  color: #f56c6c;
  background-color: #fef0f0;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
}

.pagination-wrapper {
  display: flex;
  justify-content: center;
  margin-top: 20px;
  margin-bottom: 20px;
}

.custom-pagination {
  padding: 12px 16px;
  background-color: #ffffff;
  border-radius: 6px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.custom-pagination :deep(.el-pagination__sizes .el-input .el-input__inner) {
  height: 28px;
  line-height: 28px;
  font-size: 12px;
}
.custom-pagination :deep(.el-pagination__total) {
  font-size: 12px;
  color: #606266;
}
.custom-pagination :deep(.btn-prev),
.custom-pagination :deep(.btn-next),
.custom-pagination :deep(.el-pager li) {
  min-width: 28px;
  height: 28px;
  line-height: 28px;
  font-size: 12px;
  border-radius: 4px;
  margin: 0 2px;
}
.custom-pagination :deep(.el-pagination__jump) {
  font-size: 12px;
  color: #606266;
  margin-left: 12px;
}
.custom-pagination :deep(.el-pagination__editor.el-input .el-input__inner) {
  height: 28px;
  line-height: 28px;
  font-size: 12px;
  width: 40px;
  border-radius: 4px;
  margin: 0 4px;
}

.el-tabs {
  margin-bottom: 20px;
}
.el-tabs :deep(.el-tabs__header) {
  background-color: #ffffff;
  padding: 0 20px;
  border-radius: 8px 8px 0 0;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
  margin: 0;
}
.el-tabs :deep(.el-tabs__content) {
  background-color: #ffffff;
  padding: 20px;
  border-radius: 0 0 8px 8px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
  min-height: 400px;
}
</style>
