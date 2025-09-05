<template>
  <DashboardLayout>
    <div class="collaboration-management">
      <!-- 如果没有权限，显示无权访问提示 -->
      <div v-if="!hasAccess" class="no-access">
        <h2>无权访问</h2>
        <p>您没有查看合作方管理页面的权限。如需访问，请联系管理员。</p>
        <div class="no-access-actions">
          <button class="btn-primary" @click="goHome">返回首页</button>
        </div>
      </div>

      <!-- 有权限则显示正常页面 -->
      <div v-else>
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
    </div>
  </DashboardLayout>
</template>

<script setup>
import { ref, computed } from 'vue';
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

// 判断 authority 字段是否为 1 或 2
const hasAccess = computed(() => {
  const auth = Number(userStore.userInfo?.authority)
  return auth === 1 || auth === 2
})

const goHome = () => {
  // 简单返回首页
  window.location.href = '/'
}

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

.no-access {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  text-align: center;
}
.no-access h2 {
  margin-bottom: 12px;
}
.no-access p {
  margin-bottom: 18px;
  color: #666;
}
.no-access-actions .btn-primary {
  padding: 8px 14px;
}
</style>
