<template>
  <div class="event-management-page">
    <h2>活动管理</h2>
    <el-form :model="form" ref="formRef" label-width="80px" class="event-form">
      <el-form-item label="活动ID查找">
        <el-input v-model="searchId" placeholder="输入活动ID查找" style="width:200px;" />
        <el-button type="primary" @click="searchActivity" style="margin-left:8px;">查找</el-button>
        <el-button @click="clearSearch" style="margin-left:8px;">清空</el-button>
      </el-form-item>
      <el-form-item label="活动名称">
        <el-input v-model="form.EVENT_NAME" />
      </el-form-item>
      <el-form-item label="开始时间">
        <el-date-picker v-model="form.EVENT_START" type="datetime" value-format="yyyy-MM-dd HH:mm:ss" />
      </el-form-item>
      <el-form-item label="结束时间">
        <el-date-picker v-model="form.EVENT_END" type="datetime" value-format="yyyy-MM-dd HH:mm:ss" />
      </el-form-item>
      <el-form-item label="描述">
        <el-input v-model="form.Description" />
      </el-form-item>
      <el-form-item label="费用">
        <el-input-number v-model="form.Cost" :min="0" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="handleAddOrUpdate">{{ form.EVENT_ID ? '修改' : '新增' }}</el-button>
        <el-button @click="resetForm">重置</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="filteredActivities" style="width: 100%; margin-top: 24px;">
      <el-table-column prop="EVENT_ID" label="ID" width="60" />
      <el-table-column prop="EVENT_NAME" label="名称" />
      <el-table-column prop="EVENT_START" label="开始时间" />
      <el-table-column prop="EVENT_END" label="结束时间" />
      <el-table-column prop="Description" label="描述" />
      <el-table-column prop="Cost" label="费用" />
      <el-table-column label="操作" width="180">
        <template #default="scope">
          <el-button size="small" @click="editActivity(scope.row)">编辑</el-button>
          <el-button size="small" type="danger" @click="deleteActivity(scope.row.EVENT_ID)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup>
import axios from 'axios';
import { ref, computed, onMounted } from 'vue';
import { ElMessage } from 'element-plus';

const activities = ref([]);
const form = ref({
  EVENT_ID: null,
  EVENT_NAME: '',
  EVENT_START: '',
  EVENT_END: '',
  Description: '',
  Cost: 0
});
const formRef = ref();
const searchId = ref('');
const filteredActivities = computed(() => {
  if (!searchId.value) return activities.value;
  return activities.value.filter(a => String(a.EVENT_ID) === String(searchId.value));
});

const fetchActivities = async () => {
  try {
    const response = await axios.get('http://localhost:8081/api/SaleEvent');
    activities.value = response.data;
  } catch (err) {
    ElMessage.error('获取活动列表失败');
    console.error(err);
  }
};

const handleAddOrUpdate = async () => {
  try {
    if (form.value.EVENT_ID) {
      // 修改
      await axios.put(`http://localhost:8081/api/SaleEvent/${form.value.EVENT_ID}`, form.value);
      ElMessage.success('活动修改成功');
    } else {
      // 新增
      await axios.post('http://localhost:8081/api/SaleEvent', form.value);
      ElMessage.success('活动新增成功');
    }
    resetForm();
    fetchActivities();
  } catch (err) {
    ElMessage.error('操作失败');
    console.error(err);
  }
};

const deleteActivity = async (id) => {
  try {
    await axios.delete(`http://localhost:8081/api/SaleEvent/${id}`);
    ElMessage.success('活动删除成功');
    fetchActivities();
  } catch (err) {
    ElMessage.error('删除失败');
    console.error(err);
  }
};

const editActivity = (activity) => {
  form.value = { ...activity };
};

const resetForm = () => {
  form.value = {
    EVENT_ID: null,
    EVENT_NAME: '',
    EVENT_START: '',
    EVENT_END: '',
    Description: '',
    Cost: 0
  };
  if (formRef.value) formRef.value.clearValidate();
};

const searchActivity = async () => {
  if (!searchId.value) {
    ElMessage.warning('请输入活动ID');
    return;
  }

};

const clearSearch = () => {
  searchId.value = '';
};

onMounted(() => {
  fetchActivities();
});
</script>

<style scoped>
.event-management-page { padding: 16px; }
.event-form { max-width: 500px; margin-bottom: 24px; }
</style>