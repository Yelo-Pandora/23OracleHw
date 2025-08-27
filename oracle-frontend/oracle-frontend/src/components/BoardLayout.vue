<!-- src/components/DashboardLayout.vue -->
<template>
  <div class="dashboard-layout">
    <aside class="sidebar">
      <div class="sidebar-header">商业综合体管理系统</div>
      <nav class="sidebar-nav">
        <ul>
          <!-- v-for遍历计算出来的路由对象 -->
          <li v-for="route in visibleRoutes"
              :key="route.path"
              :class="{ active: $route.path === route.path }">
            <!-- 'to'属性直接绑定到路由的path -->
            <router-link :to="route.path">
              <span>{{ route.meta.title }}</span>
            </router-link>
          </li>
        </ul>
      </nav>
    </aside>

    <div class="main-content">
      <header class="header">
        <!-- 面包屑动态显示当前路由的标题 -->
        <div class="breadcrumb">{{ $route.meta.title }}</div>
        <div class="user-profile">
          <div class="avatar"></div>
          <span>{{ userStore.userInfo?.username || '用户' }}</span>
          <button v-if="userStore.token" class="logout-btn" @click="logout">退出</button>
        </div>
      </header>
      <main class="page-content">
        <!-- 此处列表的显示会根据路由动态替换 -->
        <slot></slot>
      </main>
    </div>
  </div>
</template>

<script setup>
  // 通过useRouter()获取路由配置，并计算出visibleRoutes。
  import { computed } from 'vue';
  import { useRouter } from 'vue-router';
  import { useUserStore } from '@/stores/user';

  const router = useRouter();
  const userStore = useUserStore();

  const visibleRoutes = computed(() => {
    const rawUserRole = userStore.role || '游客'
    // 与 router.beforeEach 中相同的 role 映射，保证中文/英文角色都能匹配
    const roleMap = {
      guest: '游客',
      visitor: '游客',
      merchant: '商户',
      shop: '商户',
      staff: '员工',
      employee: '员工'
    }
    const normalized = roleMap[String(rawUserRole).toLowerCase()] || rawUserRole

    const isAllowed = (roles, userR, normalizedR) => {
      if (!Array.isArray(roles)) return false
      return roles.includes(userR) || roles.includes(String(userR).toLowerCase()) || roles.includes(normalizedR)
    }

    return router.options.routes.filter(route => {
      if (!route.meta || !route.meta.title) return false;
      if (route.path === '/login') return false; // 明确排除登录页
      if (!route.meta.role_need) return false
      if (!isAllowed(route.meta.role_need, rawUserRole, normalized)) return false
      return true;
    });
  });

  function logout() {
    userStore.logout()
    router.push('/login')
  }
</script>

<style scoped>
  .dashboard-layout {
    /* 左侧导航栏的背景颜色 */
    --sidebar-bg: #ffffff;
    /* 左侧导航栏的文字颜色 */
    --sidebar-text: #333744;
    /* 左侧导航栏的激活文字颜色 */
    --sidebar-active-text: #1abc9c;
    /* 顶部页眉的背景颜色 */
    --header-bg: #343a40;
    /* 顶部页眉的文字颜色 */
    --header-text: #ffffff;
    /* 主内容区域的背景颜色 */
    --content-bg: #f4f6f9;
    /* 区块之间的分割线颜色 */
    --border-color: #dee2e6;
    /* 布局样式 */
    display: flex;
    height: 100vh;
  }

  body, html {
    margin: 0;
    padding: 0;
    height: 100%;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    background-color: var(--content-bg);
  }

  .sidebar {
    width: 210px;
    background-color: var(--sidebar-bg);
    color: var(--sidebar-text);
    display: flex;
    flex-direction: column;
    transition: width 0.3s;
    border-right: 1px solid var(--border-color);
    flex-shrink: 0;
  }

  .sidebar-header {
    padding: 20px;
    text-align: center;
    font-size: 1.2em;
    font-weight: 600;
    color: var(--sidebar-text);
    flex-shrink: 0;
    border-bottom: 1px solid var(--border-color);
  }

  .sidebar-nav {
    flex-grow: 1;
    overflow-y: auto;
  }

    .sidebar-nav ul {
      list-style: none;
      padding: 10px 0;
      margin: 0;
    }

    .sidebar-nav li a {
      display: flex;
      align-items: center;
      padding: 15px 20px;
      color: var(--sidebar-text);
      text-decoration: none;
      transition: background-color 0.2s, color 0.2s;
      font-weight: 500;
    }

      .sidebar-nav li a .icon {
        margin-right: 15px;
        width: 20px;
        height: 20px;
        opacity: 0.8;
      }

      .sidebar-nav li a:hover {
        background-color: #f8f9fa;
        color: var(--sidebar-active-text);
      }

    .sidebar-nav li.active > a {
      color: var(--sidebar-active-text);
      background-color: #e6f7f4;
    }

  .nav-dropdown > a::after {
    content: '▼';
    margin-left: auto;
    font-size: 0.7em;
    transition: transform 0.3s;
  }

  .nav-dropdown.open > a::after {
    transform: rotate(180deg);
  }

  /* 右侧主内容区 */
  .main-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }

  /* 顶部页眉 */
  .header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0 20px;
    height: 60px;
    background-color: var(--header-bg);
    color: var(--header-text);
    border-bottom: 1px solid #4a5158;
    flex-shrink: 0;
  }

    .header .breadcrumb {
      color: #f0f0f0;
    }

    .header .user-profile {
      display: flex;
      align-items: center;
      cursor: pointer;
    }

      .header .user-profile .avatar {
        width: 36px;
        height: 36px;
        border-radius: 50%;
        background-color: #ccc;
        margin-right: 10px;
      }


  /* 页面内容区域 */
  .page-content {
    flex-grow: 1;
    padding: 20px;
    overflow-y: auto;
    background-color: var(--content-bg);
  }
</style>
