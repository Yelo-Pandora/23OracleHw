<template>
  <DashboardLayout>
  <div class="placeholder-page">
    <div class="header">
      <h2>活动查询</h2>
    </div>

    <!-- 筛选区域 -->
    <div class="filter-section">
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
                <div class="buttons-container">
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
                  <!-- 清除筛选按钮 -->
                  <button
                    type="button"
                    @click="handleClearFilters"
                    class="custom-button custom-button--secondary"
                  >
                    清除筛选
                  </button>
                </div>
              </el-form-item>
            </el-col>
          </el-row>
        </el-form>
      </div>

      <!-- 筛选按钮区域 -->
      <div class="filter-buttons-section">
        <div class="filter-buttons">
          <button
            type="button"
            @click="applyFilter('type', 'venue')"
            :class="['custom-button', 'custom-button--secondary', { 'is-active': activeFilters.type === 'venue' }]"
          >
            场地活动
          </button>
          <button
            type="button"
            @click="applyFilter('type', 'promotion')"
            :class="['custom-button', 'custom-button--secondary', { 'is-active': activeFilters.type === 'promotion' }]"
          >
            促销活动
          </button>
          
          <button
            type="button"
            @click="applyFilter('status', 'upcoming')"
            :class="['custom-button', 'custom-button--secondary', { 'is-active': activeFilters.status === 'upcoming' }]"
          >
            未开始
          </button>
          <button
            type="button"
            @click="applyFilter('status', 'ongoing')"
            :class="['custom-button', 'custom-button--secondary', { 'is-active': activeFilters.status === 'ongoing' }]"
          >
            进行中
          </button>
          <button
            type="button"
            @click="applyFilter('status', 'completed')"
            :class="['custom-button', 'custom-button--secondary', { 'is-active': activeFilters.status === 'completed' }]"
          >
            已结束
          </button>

        </div>
      </div>
    </div>

    <!-- 内容区域 -->
    <div class="content-section">
      <div v-if="loading" class="empty-state">
        <i class="el-icon-loading"></i>
        <p>加载中...</p>
      </div>

      <div v-else-if="activities.length === 0" class="empty-state">
        <i class="el-icon-date"></i>
        <p>暂无活动数据</p>
      </div>

      <div v-else>
        <div class="card-container">
          <el-row :gutter="20">
            <el-col v-for="activity in activities" :key="activity.EVENT_ID" :span="8" style="margin-bottom: 20px;">
              <div class="activity-card">
                <div class="card-header">
                  <div class="card-title">{{ activity.EVENT_NAME }}</div>
                </div>
                <div class="card-content">
                  <!-- 时间 -->
                  <div class="card-detail">
                    <i class="el-icon-time detail-icon"></i>
                    <span class="detail-text">时间: {{ formatDate(activity.EVENT_START) }} 至 {{ formatDate(activity.EVENT_END) }}</span>
                  </div>

                  <!-- 根据活动类型显示不同字段 -->
                  <!-- 场地活动特有字段 -->
                  <template v-if="activeFilters.type === 'venue'">
                    <div class="card-detail">
                      <i class="el-icon-user detail-icon"></i>
                      <span class="detail-text">容量: {{ activity.Capacity }}</span>
                    </div>
                    <div class="card-detail">
                      <i class="el-icon-money detail-icon"></i>
                      <span class="detail-text">花费: {{ activity.Cost }}</span>
                    </div>
                    <div class="card-detail">
                      <i class="el-icon-price-tag detail-icon"></i>
                      <span class="detail-text">收费: {{ activity.Fee }}</span>
                    </div>
                    <div class="card-detail">
                      <i class="el-icon-user-solid detail-icon"></i>
                      <span class="detail-text">参与人数: {{ activity.Participants }}</span>
                    </div>
                  </template>

                  <!-- 促销活动特有字段 -->
                  <template v-else-if="activeFilters.type === 'promotion'">
                    <div class="card-detail">
                      <i class="el-icon-document detail-icon"></i>
                      <span class="detail-text">描述: {{ activity.Description }}</span>
                    </div>
                  </template>

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
      </div>
    </div>

    <!-- 分页控件 -->
    <div v-if="total > pageSize" class="pagination-container">
      <div class="pagination-info">
        共 {{ total }} 条，第 {{ currentPage }} / {{ totalPages }} 页
      </div>
      <div class="pagination-controls">
        <button
          class="custom-button custom-button--secondary"
          :disabled="currentPage <= 1"
          @click="handlePrevPage"
        >
          上一页
        </button>
        <button
          class="custom-button custom-button--secondary"
          :disabled="currentPage >= totalPages"
          @click="handleNextPage"
          style="margin-left: 10px;"
        >
          下一页
        </button>
      </div>
    </div>
  </div>
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '@/components/BoardLayout.vue';
import axios from 'axios';
import { ref, onMounted, reactive, computed } from 'vue';
import { ElMessage } from 'element-plus';
const filterForm = ref({
  status: '',
  dateRange: []
});

// 使用 activeFilters 来管理当前筛选状态
const activeFilters = reactive({
  type: 'promotion', // 默认选中促销活动
  status: '' // 'upcoming', 'ongoing', 'completed'
});

const activities = ref([]);
const loading = ref(false);
const queryLoading = ref(false);
const currentPage = ref(1);
const pageSize = ref(6);
const total = ref(0);

const isQueryClicked = ref(false);

// 计算总页数
const totalPages = computed(() => Math.ceil(total.value / pageSize.value));

const formatDate = (dateString) => {
  if (!dateString) return '';
  const date = new Date(dateString);
  if (isNaN(date.getTime())) {
    const parts = dateString.split('T')[0];
    if (parts && parts.match(/^\d{4}-\d{2}-\d{2}$/)) {
       return parts; 
    }
    return dateString;
  }
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

// 计算活动状态
function calcStatus(activity) {
  const now = new Date();
  const start = new Date(activity.EVENT_START);
  const end = new Date(activity.EVENT_END);
  if (isNaN(start.getTime()) || isNaN(end.getTime())) {
      console.warn('Invalid date for activity:', activity.EVENT_NAME);
      return 'completed';
  }
  if (now < start) return 'upcoming';
  if (now >= start && now <= end) return 'ongoing';
  return 'completed';
}

const getStatusClass = (status) => {
  return `status-${status}`;
};

// 应用筛选条件
const applyFilter = (filterType, value) => {
  // 如果点击的是已经激活的类型筛选按钮，则清除该筛选
  if (filterType === 'type' && activeFilters[filterType] === value) {
    activeFilters[filterType] = '';
  } else {
    activeFilters[filterType] = value;
  }
  // 应用筛选时重置到第一页
  currentPage.value = 1;
  handleQueryClick();
};

// 清除所有筛选条件
const handleClearFilters = () => {
  // 重置表单筛选
  filterForm.value.status = '';
  filterForm.value.dateRange = [];
  
  // 重置快速筛选按钮状态
  activeFilters.type = 'promotion'; // 重置为默认类型
  activeFilters.status = '';
  
  // 重置分页
  currentPage.value = 1;
  
  // 重新加载数据
  handleQueryClick();
};

const handleQueryClick = async () => {
  if (queryLoading.value) return;

  isQueryClicked.value = true;
  queryLoading.value = true;
  loading.value = true;

  setTimeout(() => {
    isQueryClicked.value = false;
  }, 150);
  
  try {
    let list = []; // 用于存储最终处理后的活动列表
    if (activeFilters.type === 'venue') {
      
      const response = await axios.get('/api/VenueEvent/events');
      list = response.data.map(a => ({
        ...a,
        status: calcStatus(a)
      }));

    } else if (activeFilters.type === 'promotion') {

      const response = await axios.get('/api/SaleEvent');
      list = response.data.map(a => ({
        ...a,
        status: calcStatus(a)
      }));

    }

    const statusFilter = filterForm.value.status || activeFilters.status;
    if (statusFilter) {
      list = list.filter(a => a.status === statusFilter);
    }

    if (filterForm.value.dateRange && filterForm.value.dateRange.length === 2) {
      const [start, end] = filterForm.value.dateRange;
      list = list.filter(a => {
         const activityStartDate = formatDate(a.EVENT_START);
         const activityEndDate = formatDate(a.EVENT_END);

         const filterStart = new Date(start);
         const filterEnd = new Date(end);
         const eventStart = new Date(activityStartDate);
         const eventEnd = new Date(activityEndDate);

         return eventStart <= filterEnd && eventEnd >= filterStart;
      });
    }

    // --- 分页处理 ---
    total.value = list.length;
    
    // 根据当前页和每页大小计算显示的数据
    const start = (currentPage.value - 1) * pageSize.value;
    const end = start + pageSize.value;
    activities.value = list.slice(start, end);

  } catch (err) {
    ElMessage.error('获取活动数据失败，请稍后重试');
    console.error(err);
    activities.value = []; // 出错时清空列表
    total.value = 0;
  } finally {
    queryLoading.value = false;
    loading.value = false;
  }
};

// 处理上一页点击
const handlePrevPage = () => {
  if (currentPage.value > 1) {
    currentPage.value--;
    handleQueryClick(); // 重新加载当前页数据
  }
};

// 处理下一页点击
const handleNextPage = () => {
  if (currentPage.value < totalPages.value) {
    currentPage.value++;
    handleQueryClick(); // 重新加载当前页数据
  }
};

const getStatusText = (status) => {
  const statusMap = {
    upcoming: '未开始',
    ongoing: '进行中',
    completed: '已结束'
  };
  return statusMap[status] || '未知状态';
};

// 页面加载时，默认加载促销活动
onMounted(() => {
  handleQueryClick(); 
});
</script>

<style scoped>
.placeholder-page {
  padding: 20px;
  background-color: #f5f7fa;
  min-height: 100vh;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
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

.filter-section {
  margin-bottom: 20px;
}

.filter-container {
  background-color: #ffffff;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
  margin-bottom: 15px;
}

.filter-buttons-section {
  background-color: #ffffff;
  padding: 15px 20px;
  border-radius: 8px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.button-col {
  display: flex;
  align-items: flex-end;
}

.buttons-container {
  display: flex;
  flex-direction: column;
  gap: 15px;
  width: 100%;
}

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
  position: relative;
  overflow: hidden;
}

.custom-button--primary {
  background-color: #409eff;
  border-color: #409eff;
  color: #ffffff;
}

.custom-button--secondary {
  background-color: #f0f2f5;
  border-color: #dcdfe6;
  color: #606266;
  margin-right: 8px;
}

.custom-button--warning {
  background-color: #fdf6ec;
  border-color: #f5dab3;
  color: #e6a23c;
}

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
.custom-button--secondary:hover {
  background-color: #e1e5ea;
  border-color: #c0c4cc;
}
.custom-button--warning:hover {
  background-color: #faecd8;
  border-color: #f5dab3;
}

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

.custom-button.is-active {
  background-color: #409eff;
  border-color: #409eff;
  color: #ffffff;
}

.custom-button.is-clicked::after {
  content: "";
  position: absolute;
  top: 50%;
  left: 50%;
  width: 0;
  height: 0;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.5);
  transform: translate(-50%, -50%);
  animation: ripple 0.4s linear;
}

@keyframes ripple {
  to {
    width: 200%;
    height: 200%;
    opacity: 0;
  }
}

.custom-button.is-loading {
  pointer-events: none;
  opacity: 0.8;
}

.filter-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.content-section {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.card-container {
  flex: 1;
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

.pagination-container {
  background-color: #ffffff;
  padding: 15px 20px;
  border-radius: 8px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
  display: flex;
  justify-content: space-between; /* 左右分布 */
  align-items: center;
}

.pagination-info {
  font-size: 14px;
  color: #606266;
}

.pagination-controls {
  display: flex;
  align-items: center;
}

.empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  color: #909399;
  font-size: 16px;
}
.empty-state i {
  font-size: 48px;
  margin-bottom: 16px;
}
</style>