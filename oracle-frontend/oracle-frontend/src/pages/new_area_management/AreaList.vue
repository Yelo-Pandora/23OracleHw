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
          <label>区域名称:</label>
          <input type="text" v-model="searchParams.name">
        </div>
        <div class="form-group">
          <label>负责人:</label>
          <input type="text" v-model="searchParams.contactor">
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
            <th>名称</th>
            <th>负责人</th>
            <th>电话</th>
            <th>邮箱</th>
            <th v-if="userStore.role === '员工'">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="collab in areas" :key="collab.area_ID">
            <td>{{ collab.area_ID }}</td>
            <td>{{ collab.area_NAME }}</td>
            <td>{{ collab.CONTACTOR || '-' }}</td>
            <td>{{ collab.PHONE_NUMBER || '-' }}</td>
            <td>{{ collab.EMAIL || '-' }}</td>
            <td v-if="userStore.role === '员工'">
              <button @click="editarea(collab)" class="btn-edit">编辑</button>
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

const userStore = useUserStore();
const router = useRouter();
const areas = ref([]);
const loading = ref(false);

const searchParams = reactive({
  id: null,
  name: '',
  contactor: ''
});

// 检查登录状态
const checkAuth = () => {
  if (!userStore.token) {
    router.push('/login');
    return false;
  }
  return true;
};

const searchareas = async () => {
  if (!checkAuth()) return;

  loading.value = true;
  try {
    const params = {};
    params.operatorAccountId = userStore.token;
    if (searchParams.id) params.id = searchParams.id;
    if (searchParams.name) params.name = searchParams.name;
    if (searchParams.contactor) params.contactor = searchParams.contactor;

    const response = await axios.get('/api/area', { params });
    areas.value = response.data;
  } catch (error) {
    console.error('查询区域失败:', error);
    if (error.response && error.response.status === 401) {
      await alert('登录已过期，请重新登录');
      userStore.logout();
      router.push('/login');
    } else {
      await alert('查询失败，' + (error || '，请稍后重试'));
    }
  } finally {
    loading.value = false;
  }
};

const resetSearch = () => {
  searchParams.id = null;
  searchParams.name = '';
  searchParams.contactor = '';
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

const emit = defineEmits(['edit-area']);
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
</style>
