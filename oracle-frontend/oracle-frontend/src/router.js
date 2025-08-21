import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from './user/user'
// 指定页面对应的vue应用实例，开发完成后自行解除注释
import Login from './pages/login/LoginPage.vue'
import Home from './pages/home/Home.vue'
// import Visualization from './pages/area_visualization/App.vue'
// import Equipment from './pages/Equipment_management/App.vue'
// import Employee from './pages/employee_management/App.vue'
// import Events from './pages/events_management/App.vue'
// import Mall from './pages/mall_management/App.vue'
// import Cashflow from './pages/cashflow_management/App.vue'
// import Collaboration from './pages/collaboration_management/App.vue'
// import Parking from './pages/parking_management/App.vue'

// 定义路由，开发完成后自行解除注释
const routes = [
  // 登陆界面
  {
    path: '/login',
    component: Login,
    meta: { requiresAuth: false, title: '登录'},
  },
  // 首页
  {
    path: '/',
    component: Home,
    meta: { requiresAuth: true,title: '主页' },
  },
//   // 区域图形可视化页面
//   { path: '/area_visualization', component: Visualization, meta: { requiresAuth: true } },
//   // 活动管理页面
//   { path: '/events_management', component: Events, meta: { requiresAuth: true } },
//   // 员工信息管理页面
//   { path: '/employee_management', component: Employee, meta: { requiresAuth: true } },
//   // 合作方信息管理页面
//   { path: '/collaboration_management', component: Collaboration, meta: { requiresAuth: true } },
//   //商场/店铺管理页面
//   { path: '/mall_management', component: Mall, meta: { requiresAuth: true } },
//   //停车场管理页面
//   { path: '/parking_management', component: Parking, meta: { requiresAuth: true } },
//   //设备管理页面
//   { path: '/equipment_management', component: Equipment, meta: { requiresAuth: true } },
//   //现金流管理页面
//   { path: '/cashflow_management', component: Cashflow, meta: { requiresAuth: true } },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

//检查登陆状态，没登录的话会先回到登录界面
router.beforeEach((to, from, next) => {
  const userStore = useUserStore()
  const isLoggedIn = !!userStore.token // 通过 token 判断用户是否登录
  if (to.meta.requiresAuth && !isLoggedIn) {
    // a. 如果页面需要登录，但用户未登录，则强制跳转到登录页
    next('/login') 
  }
  else if (to.name === 'login' && isLoggedIn) {
    // b. 如果用户已登录，但又尝试访问登录页，则直接跳转到首页
    next('/')
  }
  else {
    // c. 其他所有情况（无需登录的页面，或需要登录且已登录的页面），直接放行
    next() 
  }
  if (to.meta.title) {
    document.title = to.meta.title;
  }
})
export default router
