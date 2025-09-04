<!-- pages/account_management/TempAuthEditor.vue -->
<template>
  <div class="temp-auth-editor">
    <header class="editor-header">
      <div class="header-left">
        <!-- 返回按钮 -->
        <router-link to="/account_management" class="back-link">&larr; 返回账号列表</router-link>
        <!-- 被操作的账号信息 -->
        <h2>编辑临时权限: <span class="account-name">{{ accountId }}</span></h2>
      </div>
      <div class="header-right">
        <!-- 操作者信息 -->
        <p>操作员: {{ userStore.userInfo?.username || 'N/A' }}</p>
      </div>
    </header>

    <div v-if="tempAuthStore.isLoading" class="loading-state">
      正在加载数据...
    </div>
    <div v-else-if="tempAuthStore.error" class="error-state">
      {{ tempAuthStore.error }}
    </div>

    <div v-else class="editor-content">
      <!-- 未授权活动列表 -->
      <section class="auth-section">
        <h3>未授予权限的活动</h3>
        <div class="table-wrapper">
          <table class="auth-table">
            <thead>
              <tr>
                <th>活动名称</th>
                <th>开始时间</th>
                <th>结束时间</th>
                <th class="permission-col">权限值</th>
                <th class="action-col">操作</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="ungrantedEventsWithInput.length === 0">
                <td colspan="5">所有活动均已设置权限</td>
              </tr>
              <tr v-for="event in ungrantedEventsWithInput" :key="event.EVENT_ID">
                <td>{{ event.EVENT_NAME }}</td>
                <td>{{ new Date(event.EVENT_START).toLocaleString() }}</td>
                <td>{{ event.EVENT_END ? new Date(event.EVENT_END).toLocaleString() : '无' }}</td>
                <td>
                  <input type="number" v-model="event.tempAuthValue" class="permission-input" placeholder="输入权限值" />
                </td>
                <td>
                  <button @click="handleGrant(event)" class="row-action-button">建立权限</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <!-- 已授权活动列表 -->
      <section class="auth-section">
        <h3>已授予权限的活动</h3>
        <div class="table-wrapper">
          <table class="auth-table">
            <thead>
              <tr>
                <th>活动名称</th>
                <th>权限值</th>
                <th class="action-col">操作</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="tempAuthStore.grantedEvents.length === 0">
                <td colspan="3">暂无已授予的权限</td>
              </tr>
              <tr v-for="event in tempAuthStore.grantedEvents" :key="event.EVENT_ID">
                <td>{{ event.EVENT_NAME }}</td>
                <!-- 找到对应的权限值来显示 -->
                <td>{{ getGrantedAuthValue(event.EVENT_ID) }}</td>
                <td>
                  <button @click="handleRevoke(event.EVENT_ID)" class="row-action-button row-action-button--danger">删除权限</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
import { onMounted, onUnmounted, computed, ref } from 'vue';
import { useTempAuthStore } from '@/pages/account_management/TempAuthStore';
import { useUserStore } from '@/user/user';

// 1. 接收来自路由的 props
const props = defineProps({
  accountId: {
    type: String,
    required: true,
  },
});

const tempAuthStore = useTempAuthStore();
const userStore = useUserStore();

// 为未授权的活动列表添加一个临时的输入框值
const ungrantedEventsWithInput = computed(() =>
  tempAuthStore.ungrantedEvents.map(event => ({
    ...event,
    tempAuthValue: ref(0) // 默认权限值为0
  }))
);

// 查找已授权活动的权限值
const getGrantedAuthValue = (eventId) => {
  const auth = tempAuthStore.tempAuthorities.find(
    a => a.ACCOUNT === props.accountId && a.EVENT_ID === eventId
  );
  return auth ? auth.TEMP_AUTHORITY : 'N/A';
};

// 按钮点击处理
const handleGrant = async (event) => {
  if (event.tempAuthValue.value === null || event.tempAuthValue.value === '') {
    alert('请输入一个有效的权限值。');
    return;
  }
  try {
    await tempAuthStore.grantAuthority(event.EVENT_ID, event.tempAuthValue);
    alert('权限授予成功！');
  } catch (error) {
    alert(`操作失败,${error}`);
  }
};

const handleRevoke = async (eventId) => {
  if (!confirm('您确定要删除此项临时权限吗？')) return;
  try {
    await tempAuthStore.revokeAuthority(eventId);
    alert('权限删除成功！');
  } catch (error) {
    alert(`操作失败: ${error}`);
  }
};


// 2. 在组件挂载时，使用 prop 初始化 store
onMounted(() => {
  tempAuthStore.initialize(props.accountId);
});

// 3. 在组件卸载时，清理 store，避免数据污染
onUnmounted(() => {
  tempAuthStore.cleanup();
});
</script>

<style scoped>
  /* 在这里添加新页面的样式 */
  .temp-auth-editor {
    padding: 2rem;
  }

  .editor-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: 2rem;
    border-bottom: 1px solid #eee;
    padding-bottom: 1rem;
  }

  .account-name {
    color: #4A90E2;
  }

  .back-link {
    text-decoration: none;
    color: #555;
    margin-bottom: 0.5rem;
    display: inline-block;
  }

  .auth-section {
    margin-top: 2rem;
  }

  .table-wrapper {
    background-color: #fff;
    border-radius: 8px;
    box-shadow: 0 2px 12px 0 rgba(0,0,0,0.08);
    overflow: hidden;
  }

  .auth-table {
    width: 100%;
    border-collapse: collapse;
  }

    .auth-table th, .auth-table td {
      padding: 12px 15px;
      border-bottom: 1px solid #dee2e6;
      text-align: left;
    }

    .auth-table thead th {
      background-color: #f5f7fa;
    }

  .permission-col {
    width: 150px;
  }

  .action-col {
    width: 150px;
    text-align: center;
  }

  .permission-input {
    width: 100px;
    padding: 4px;
    border: 1px solid #ccc;
    border-radius: 4px;
  }

  /* 行内操作按钮的通用样式 */
  .row-action-button {
    padding: 4px 8px; /* 比全局按钮更小，更紧凑 */
    font-size: 0.8rem;
    border: 1px solid #4A90E2;
    background-color: #fff;
    color: #4A90E2;
    border-radius: 4px;
    cursor: pointer;
    transition: all 0.2s;
  }
    .row-action-button:hover {
      background-color: #4A90E2;
      color: #fff;
    }

  /* 行内危险操作按钮的样式 */
  .row-action-button--danger {
    border-color: #e74c3c;
    color: #e74c3c;
  }

    .row-action-button--danger:hover {
      background-color: #e74c3c;
      color: #fff;
    }
</style>
