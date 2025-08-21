<!-- src/components/DashboardLayout.vue -->
<template>
  <div class="dashboard-layout">
    <!-- 左侧导航栏 -->
    <aside class="sidebar">
      <div class="sidebar-header">
        通用后台管理系统
      </div>
      <nav class="sidebar-nav">
        <ul>
          <li class="active">
            <a href="#">
              <!-- SVG 图标会继承父元素的 color -->
              <svg class="icon" viewBox="0 0 1024 1024" xmlns="http://www.w3.org/2000/svg"><path fill="currentColor" d="M946.5 505L534.6 93.4a31.93 31.93 0 0 0-45.2 0L77.5 505c-12 12-18.8 28.3-18.8 45.3V884c0 35.3 28.7 64 64 64h204.3V691.2H499.1V948H703.4c35.3 0 64-28.7 64-64V550.3c0-17-6.8-33.3-18.9-45.3z"></path></svg>
              <span>首页</span>
            </a>
          </li>
        </ul>
      </nav>
    </aside>

    <!-- 右侧主区域 -->
    <div class="main-content">
      <!-- 顶部页眉 -->
      <header class="header">
        <div class="breadcrumb">
          首页
        </div>
        <div class="user-profile">
          <div class="avatar"></div>
          <span>Admin</span>
        </div>
      </header>

      <!-- 页面内容插入位置 -->
      <main class="page-content">
        <slot></slot>
      </main>
    </div>
  </div>
</template>

<script setup>
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
  /* 整体 */
  body, html {
    margin: 0;
    padding: 0;
    height: 100%;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    background-color: var(--content-bg);
  }

  /* 左侧导航栏 */
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
