<template>
  <DashboardLayout>
    <div class="collaboration-management">
      <div class="header">
        <h1>合作方管理</h1>
        <div class="header-actions">
          <button class="btn-primary" @click="activeTab = 'list'">合作方列表</button>
          <button class="btn-primary" @click="activeTab = 'add'" v-if="userStore.role === '员工'">添加合作方</button>
          <button class="btn-primary" @click="activeTab = 'report'" v-if="userStore.role === '员工'">统计报表</button>
        </div>
      </div>

          <div class="content">
            <CollaborationList :key="listKey" v-if="activeTab === 'list'" @edit-collaboration="handleEditCollaboration" />
            <AddCollaboration v-else-if="activeTab === 'add'" @saved="handleSaved" @cancel="activeTab = 'list'" />
            <EditCollaboration
              v-else-if="activeTab === 'edit'"
              :collaboration="editingCollaboration"
              @saved="handleSaved"
              @cancel="handleCancelEdit"
              @deleted="handleDeleted"
            />
            <CollaborationReport v-else-if="activeTab === 'report'" />
          </div>
    </div>
  </DashboardLayout>
</template>

<script setup>
import { ref } from 'vue';
import { useUserStore } from '@/user/user';
import DashboardLayout from '@/components/BoardLayout.vue';
import CollaborationList from './CollaborationList.vue';
import AddCollaboration from './AddCollaboration.vue';
import EditCollaboration from './EditCollaboration.vue';
import CollaborationReport from './CollaborationReport.vue';

const userStore = useUserStore();
const activeTab = ref('list');
const editingCollaboration = ref(null);
const listKey = ref(0);

const handleEditCollaboration = (collab) => {
  // 接收整个对象并进入编辑页
  editingCollaboration.value = collab;
  activeTab.value = 'edit';
};

const handleSaved = () => {
  // 保存后回到列表并清除编辑对象
  editingCollaboration.value = null;
  activeTab.value = 'list';
};

const handleCancelEdit = () => {
  // 取消编辑时清理并返回列表
  editingCollaboration.value = null;
  activeTab.value = 'list';
};

const handleDeleted = () => {
  // 删除后清理编辑对象，返回列表并强制刷新列表组件
  editingCollaboration.value = null;
  activeTab.value = 'list';
  listKey.value += 1;
};
</script>

<style scoped>
.collaboration-management {
  padding: 20px;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.header-actions {
  display: flex;
  gap: 10px;
}

.btn-primary {
  background-color: #007BFF;
  color: white;
  border: none;
  padding: 10px 15px;
  border-radius: 4px;
  cursor: pointer;
  font-weight: bold;
}

.btn-primary:hover {
  background-color: #0056b3;
}

.content {
  background-color: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}
</style>
