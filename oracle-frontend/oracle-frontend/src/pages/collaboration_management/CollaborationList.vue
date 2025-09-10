<template>
  <div class="collaboration-list">
    <div class="search-section">
      <h2>合作方查询</h2>
      <form @submit.prevent="searchCollaborations" class="search-form">
        <div class="form-group">
          <label>合作方ID:</label>
          <input type="number" v-model="searchParams.id" min="1">
        </div>
        <div class="form-group">
          <label>合作方名称:</label>
          <input type="text" v-model="searchParams.name">
        </div>
        <div class="form-group">
          <label>负责人:</label>
          <input type="text" v-model="searchParams.contactor">
        </div>
        <div class="form-actions-row">
          <button type="submit" class="btn-search">查询</button>
          <button type="button" @click="resetSearch" class="btn-reset">重置</button>
        </div>
      </form>
    </div>

    <div class="results-section">
      <h3>查询结果（点击表头以排序）</h3>
      <div v-if="loading" class="loading">加载中...</div>
      <div v-else-if="collaborations.length === 0" class="no-results">
        暂无合作方数据
      </div>
      <table v-else class="collaboration-table">
        <thead>
          <tr>
            <th @click="toggleSort('COLLABORATION_ID')" class="sortable">ID <span v-if="sort.key==='COLLABORATION_ID'">{{ sort.order === 'asc' ? '▲' : (sort.order === 'desc' ? '▼' : '') }}</span></th>
            <th @click="toggleSort('COLLABORATION_NAME')" class="sortable">名称 <span v-if="sort.key==='COLLABORATION_NAME'">{{ sort.order === 'asc' ? '▲' : (sort.order === 'desc' ? '▼' : '') }}</span></th>
            <th @click="toggleSort('CONTACTOR')" class="sortable">负责人 <span v-if="sort.key==='CONTACTOR'">{{ sort.order === 'asc' ? '▲' : (sort.order === 'desc' ? '▼' : '') }}</span></th>
            <th @click="toggleSort('PHONE_NUMBER')" class="sortable">电话 <span v-if="sort.key==='PHONE_NUMBER'">{{ sort.order === 'asc' ? '▲' : (sort.order === 'desc' ? '▼' : '') }}</span></th>
            <th @click="toggleSort('EMAIL')" class="sortable">邮箱 <span v-if="sort.key==='EMAIL'">{{ sort.order === 'asc' ? '▲' : (sort.order === 'desc' ? '▼' : '') }}</span></th>
            <th v-if="userStore.role === '员工'">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="collab in displayedCollaborations" :key="collab.collaboration_ID">
            <td>{{ collab.COLLABORATION_ID }}</td>
            <td>{{ collab.COLLABORATION_NAME }}</td>
            <td>{{ collab.CONTACTOR || '-' }}</td>
            <td>{{ collab.PHONE_NUMBER || '-' }}</td>
            <td>{{ collab.EMAIL || '-' }}</td>
            <td v-if="userStore.role === '员工'">
              <button @click="editCollaboration(collab)" class="btn-edit">编辑</button>
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

import { computed } from 'vue';
const userStore = useUserStore();
const router = useRouter();
const collaborations = ref([]);
const loading = ref(false);
const sort = reactive({ key: '', order: '' });

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

const searchCollaborations = async () => {
  if (!checkAuth()) return;

  loading.value = true;
  try {
    const params = {};
    params.operatorAccountId = userStore.token;
    if (searchParams.id) params.id = searchParams.id;
    if (searchParams.name) params.name = searchParams.name;
    if (searchParams.contactor) params.contactor = searchParams.contactor;

    const response = await axios.get('/api/Collaboration', { params });
    collaborations.value = response.data;
  } catch (error) {
    console.error('查询合作方失败:', error);
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
  collaborations.value = [];
  sort.key = '';
  sort.order = '';
};

const editCollaboration = (collab) => {
  // 直接把列表中的对象发给父组件/编辑组件，避免再次请求接口回显
  emit('edit-collaboration', collab);
};

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

const displayedCollaborations = computed(() => {
  const list = Array.isArray(collaborations.value) ? collaborations.value.slice() : [];
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

// 组件挂载时自动加载数据
onMounted(() => {
  if (checkAuth()) {
    searchCollaborations();
  }
});

const emit = defineEmits(['edit-collaboration']);
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

.form-actions-row {
  grid-column: 1 / -1;
  display: flex;
  gap: 10px;
  justify-content: flex-start;
  align-items: center;
}

.btn-search, .btn-reset {
  padding: 10px 18px;
  min-width: 260px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  align-self: end;
}

.sortable {
  cursor: pointer;
  user-select: none;
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

.collaboration-table {
  width: 100%;
  border-collapse: collapse;
  margin-top: 15px;
}

.collaboration-table th,
.collaboration-table td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #ddd;
}

.collaboration-table th {
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
