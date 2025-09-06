import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from './user/user'
// 指定页面对应的vue应用实例，开发完成后自行解除注释
import Login from './pages/login/LoginPage.vue'
import Home from './pages/home/Home.vue'
//测试用的EmployeeInfo页面
import EmployeeInfo from '@/pages/employee_management/EmployeeInfo.vue'
import AccountContent from '@/pages/account_management/AccountContent.vue'
import TempAuthEditor from '@/pages/account_management/TempAuthEditor.vue'
// import Visualization from './pages/area_visualization/App.vue'
import DeviceManagement from '@/pages/equipment_management/Equipment_management.vue'
import EmployeeManagement from './pages/employee_management/EmployeeManagement.vue'
import TotalSalary from './pages/employee_management/TotalSalary.vue'

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
    meta: { requiresAuth: true, title: '主页', role_need: ['员工', '商户', '游客'] },
  },
  // 员工信息页面（测试）
  // {
  //   path: '/employee_management',
  //   component: EmployeeInfo,
  //   meta: {
  //     requiresAuth: true,
  //     title: '员工信息', // 这个 title 会显示在页眉和菜单中
  //     role_need: ['员工']  // 假设只有“员工”角色能看到
  //   }
  // },
//   //区域可视化页面
//   {path: '/area_visualization', component: Visualization, meta: { requiresAuth: true, role_need: ['员工', '商户', '游客'] } },
//   // 活动管理/活动查询页面
//   { path: '/events_management', component: Events, meta: { requiresAuth: true, role_need: ['员工', '商户', '游客'] } },
  // 员工信息管理页面
  { 
    path: '/employee_management', 
    component: EmployeeManagement, 
    meta: { 
      requiresAuth: true,
      title: '员工信息管理',
      role_need: ['员工'] 
    }
  },
  {
    path: '/cashflow_management/total_salary',
    component: TotalSalary,
    meta: {
      requiresAuth: true,
      title: '工资总支出',
      role_need: ['员工'],
      accessAuth: 2
    }
  },
  // 账号信息管理页面
  {
    path: '/account_management',
    component: AccountContent,
    meta: {
      requiresAuth: true,
      title: '账号信息',
      role_need: ['员工', '商户'],
    },
    children: [
      {
        // 定义子路由
        // :accountId 是一个动态参数，我们将用它来传递被操作的账号
        path: 'temp-auth/:accountId',
        name: 'TempAuthEditor',
        component: TempAuthEditor,
        props: true // 3. 将路由参数 (:accountId) 作为 props 传递给组件
      }
    ]
  },
//   //区域可视化页面
//   {path: '/area_visualization', component: Visualization, meta: { requiresAuth: true, role_need: ['员工', '商户', '游客'] } },
//   // 活动管理/活动查询页面
//   { path: '/events_management', component: Events, meta: { requiresAuth: true, role_need: ['员工', '商户', '游客'] } },
//   // 员工信息管理页面
//   { path: '/employee_management', component: Employee, meta: { requiresAuth: true, role_need: ['员工'] } },
{
  path: '/collaboration_management',
  component: () => import('@/pages/collaboration_management/App.vue'),
  meta: {
    requiresAuth: true,
    title: '合作方管理',
    role_need: ['员工']  // 只有员工角色可以访问
  }
},
{
  path: '/equipment_management',
  component: DeviceManagement,
  meta: {
    requiresAuth: true,
    title: '设备信息',
    role_need: ['员工']
  },
  children: [
    {
      path: '',
      name: 'DeviceList',
      component: () => import('@/pages/equipment_management/EquipmentList.vue'),
    },
    {
      path: ':id',
      name: 'DeviceDetail',
      component: () => import('@/pages/equipment_management/EquipmentDetail.vue'),
      props: true
    }
  ]
},
//   //商场(店铺)管理/商场平面图查看页面
//   { path: '/mall_management', component: Mall, meta: { requiresAuth: true, role_need: ['员工', '商户', '游客'] } },
//   //停车场管理/车位查询页面
//   { path: '/parking_management', component: Parking, meta: { requiresAuth: true, role_need: ['员工', '商户', '游客'] } },
//   //设备管理页面
//   { path: '/equipment_management', component: Equipment, meta: { requiresAuth: true, role_need: ['员工'] } },
//   //现金流管理页面
//   { path: '/cashflow_management', component: Cashflow, meta: { requiresAuth: true, role_need: ['员工'] } },
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
