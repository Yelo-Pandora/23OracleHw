<template>
  <div class="area-list">
    <div class="search-section">
      <h2>区域查询</h2>
      <form @submit.prevent="searchareas" class="search-form">
        <div class="form-group">
          <label>区域ID:</label>
          <input type="number" v-model="searchParams.id" min="1">
        </div>
        <div class="form-group">
          <label>是否空置:</label>
          <select v-model="searchParams.isEmpty">
            <option value="">全部</option>
            <option value="1">是</option>
            <option value="0">否</option>
          </select>
        </div>
        <div class="form-group">
          <label>最小区域面积:</label>
          <input type="number" v-model="searchParams.areaMin" min="0" step="0.01">
        </div>
        <div class="form-group">
          <label>最大区域面积:</label>
          <input type="number" v-model="searchParams.areaMax" min="0" step="0.01">
        </div>
        <div class="form-group">
          <label>区域类型:</label>
          <select v-model="searchParams.category">
            <option value="">全部</option>
            <option value="RETAIL">商铺</option>
            <option value="PARKING">停车</option>
            <option value="EVENT">活动</option>
            <option value="OTHER">其他</option>
          </select>
        </div>
        <button type="submit" class="btn-search">查询</button>
        <button type="button" @click="resetSearch" class="btn-reset">重置</button>
      </form>
    </div>

    <div class="results-section">
      <h3>查询结果</h3>
      <div v-if="loading" class="loading">加载中...</div>
      <div v-else-if="areas.length === 0" class="no-results">
        暂无区域数据
      </div>
      <table v-else class="area-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>类别</th>
            <th>是否空置</th>
            <th>面积</th>
            <th>基础租金</th>
            <th>租赁状态</th>
            <th>场地费</th>
            <th>容量</th>
            <th>停车费</th>
            <th>类型(其他)</th>
            <th v-if="userStore.role === '员工'">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="a in areas" :key="a.AREA_ID">
            <td>{{ a.AREA_ID }}</td>
            <td>{{ a.CATEGORY || '-' }}</td>
            <td>{{ a.ISEMPTY === 1 ? '是' : (a.ISEMPTY === 0 ? '否' : '-') }}</td>
            <td>{{ a.AREA_SIZE != null ? a.AREA_SIZE : '-' }}</td>
            <td>{{ a.BaseRent != null ? a.BaseRent : '-' }}</td>
            <td>{{ a.RentStatus || '-' }}</td>
            <td>{{ a.AreaFee != null ? a.AreaFee : '-' }}</td>
            <td>{{ a.Capacity != null ? a.Capacity : '-' }}</td>
            <td>{{ a.ParkingFee != null ? a.ParkingFee : '-' }}</td>
            <td>{{ a.Type || '-' }}</td>
            <td v-if="userStore.role === '员工'">
              <button @click="editarea(a)" class="btn-edit">编辑</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue';
import { useUserStore } from '@/user/user';
import axios from 'axios';
import alert from '@/utils/alert';
import { useRouter } from 'vue-router';

const emit = defineEmits(['edit-area']);

const userStore = useUserStore();
const router = useRouter();
const areas = ref([]);
const loading = ref(false);

// 前端搜索参数：与后端 AreasController 匹配
const searchParams = reactive({
  id: null, // 如果填写则调用 ByID
  category: '', // 对应后端的 category
  isEmpty: '', // 对应后端的 isEmpty (0/1)
  areaMin: null, // 前端本地过滤
  areaMax: null
});

// 检查登录状态
const checkAuth = () => {
  if (!userStore.token) {
    router.push('/login');
    return false;
  }
  return true;
};

// 执行搜索：当有 id 时调用 ByID，否则调用 ByCategory
const searchareas = async () => {
  if (!checkAuth()) return;

  loading.value = true;
  try {
    let response;
    if (searchParams.id) {
      const params = { id: searchParams.id };
      response = await axios.get('/api/Areas/ByID', { params });
      // 后端返回单对象或 404
      areas.value = response.data ? [response.data] : [];
    } else {
      const params = {};
      if (searchParams.category) params.category = searchParams.category;
      if (searchParams.isEmpty !== '') params.isEmpty = searchParams.isEmpty;

      response = await axios.get('/api/Areas/ByCategory', { params });
      areas.value = Array.isArray(response.data) ? response.data : [];
    }

    // 前端根据面积 min/max 做二次过滤（后端接口未提供范围筛选）
    if (searchParams.areaMin != null || searchParams.areaMax != null) {
      areas.value = areas.value.filter(a => {
        const size = a.AREA_SIZE || 0;
        if (searchParams.areaMin != null && size < searchParams.areaMin) return false;
        if (searchParams.areaMax != null && size > searchParams.areaMax) return false;
        return true;
      });
    }
  } catch (error) {
    console.error('查询区域失败:', error);
    if (error.response && error.response.status === 401) {
      await alert('登录已过期，请重新登录');
      userStore.logout();
      router.push('/login');
    }
    else if (error && error.status === 400) {
      await alert('查询失败，请检查请求参数');
    }
    else if (error && error.status === 404) {
      await alert('未找到相关区域');
    } else {
      await alert('查询失败，' + (error || '，请稍后重试'));
    }
  } finally {
    loading.value = false;
  }
};

const resetSearch = () => {
  searchParams.id = null;
  searchParams.category = '';
  searchParams.isEmpty = '';
  searchParams.areaMin = null;
  searchParams.areaMax = null;
  areas.value = [];
};

const editarea = (collab) => {
  // 直接把列表中的对象发给父组件/编辑组件，避免再次请求接口回显
  emit('edit-area', collab);
};

// 组件挂载时自动加载数据
onMounted(() => {
  if (checkAuth()) {
    searchareas();
  }
});
</script>

<style scoped>
.search-section {
  margin-bottom: 30px;
  padding-bottom: 20px;
  border-bottom: 1px solid #eee;
}

.search-form {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 15px;
  margin-top: 15px;
}

.form-group {
  display: flex;
  flex-direction: column;
}

.form-group label {
  margin-bottom: 5px;
  font-weight: bold;
}

.form-group input {
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.btn-search, .btn-reset {
  padding: 8px 15px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  align-self: end;
}

.btn-search {
  background-color: #28a745;
  color: white;
}

.btn-reset {
  background-color: #6c757d;
  color: white;
  margin-left: 10px;
}

.area-table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 15px;
}

.area-table th,
.area-table td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #ddd;
}

.area-table th {
  background-color: #f8f9fa;
  font-weight: bold;
}

.btn-edit {
  background-color: #ffc107;
  color: #212529;
  border: none;
  padding: 5px 10px;
  border-radius: 4px;
  cursor: pointer;
}

.loading, .no-results {
  text-align: center;
  padding: 20px;
  color: #6c757d;
}

.form-group select {
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

</style>
