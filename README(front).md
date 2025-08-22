\# 数据库前端项目基础文档

\## 前端项目之前的docker工作

在开始前端项目代码编写前，需要先进行一步docker工作

```bash

// 先将docker容器卸载，并清除一下缓存

docker-compose down -v

// 再使用build将代码挂载到镜像

docker-compose build

// 最后再重新打开容器（打开后可以先把前端的容器给关掉）

docker-compose up -d

```

这样做之后后端项目将运行在本机的8081端口上，可以事先在浏览器上通过该端口检查后端部分功能

\## 前端项目环境搭建

\### 先决条件

1\. 确保设备上安装了node.js（docker环境上已安装，此处是为了能够正常开发和调试）

2\. 本项目使用 npm 作为包管理器



\### 安装与运行

```bash

// 拉取代码到本地后，在项目目录下打开powershell，安装项目的所有依赖包

npm install

// 然后便可通过指令或是VS等软件启动本地开发服务器开发调试

npm run dev

```



\## 项目结构

项目主要部分src文件夹结构如下所示：

```code

src/

├── assets/         # 静态资源 (图片、全局CSS等)

├── components/     # 全局公共组件

│   └── BoardLayout.vue  # 核心布局组件，规定除登陆页面外的所有页面的主体页面布局

├── pages/          # 页面组件 (每个子目录代表一个功能模块)

│   ├── home/

│   ├── login/

│   └── ...

├── router.js    # 唯一的路由配置文件

├── user/         # Pinia 状态管理

│   └── user.js     # 用户信息和会话管理

├── App.vue         # Vue 应用的根组件

└── main.js         # 应用的入口文件

```

项目的其他部分如根目录下的各种配置文件已配置好，如有需要修改，请先告知全体成员

\## 注意事项

为了保证项目代码的统一性、可维护性和健壮性，各组在编写代码时应遵守以下规范。



\#### 1. 新增一个后台页面的步骤

所有需要后台布局的页面都请遵循以下三步流程：



\*\*第一步：创建页面组件\*\*

在 src/pages/ 目录下，找到自己负责的功能的文件夹，并在其中创建 .vue 文件 (例如 src/pages/home/Home.vue)。

\*\*第二步：使用通用布局包裹页面\*\*

在该页面的 .vue 文件中，导入并使用 DashboardLayout 组件来包裹页面内容

```Vue

<!-- 例如: src/pages/home/Home.vue -->

<template>

&nbsp; <DashboardLayout>

&nbsp;   <h1>首页</h1>

&nbsp;   <p>这里是具体的功能实现区域。</p>

&nbsp; </DashboardLayout>

</template>



<script setup>

// 首先应该导入通用的页面组件

import DashboardLayout from '@/components/BoardLayout.vue';

</script>

```

\*\*第三步：在 router.js 中注册路由\*\*

打开src/router.js文件，在routes数组中添加新页面的路由配置对象。

path: 页面的访问路径 (例如 /home)。

component: 导入的页面组件

meta: 界面控制的核心元数据

requiresAuth: true: 表示这个页面需要登录后才能访问。

title: '首页': 这个标题会自动显示在左侧菜单和顶部页眉中。

role\_need: \['员工', '商户']: 一个数组，定义了哪些角色可以看到并访问这个页面。

```JavaScript

// src/router.js

// ...其它import

import Home from '@/pages/home/Home.vue';

//...

const routes = \[

&nbsp; // ... 其他路由

&nbsp; {

&nbsp;   path: '/',

&nbsp;   component: Home,

&nbsp;   meta: { 

&nbsp;     requiresAuth: true, 

&nbsp;     title: '主页', 

&nbsp;     role\_need: \['员工', '商户', '游客'] 

&nbsp;   }

&nbsp; },

];

```

\#### 2. 状态管理 (Pinia)

\*\*所有与用户登录状态（token, role, userInfo）相关的数据，请通过src/stores/user.js进行修改。\*\*

\*\*如有需要额外的涉及用户登录状态的操作，请先告知全体\*\*

登录成功后，调用 userStore.login(...)。

用户登出时，调用 userStore.logout()。

```JavaScript

// 在组件 <script setup> 中获取用户角色

import { useUserStore } from '@/stores/user';

const userStore = useUserStore();

const currentUserRole = userStore.role;

```

\#### 3. 样式 (CSS)

页面和组件的`<style>`标签须添加`scoped`属性，以避免组件间的样式冲突。

