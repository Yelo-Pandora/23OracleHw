import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from './user/user'
// 指定页面对应的vue应用实例，开发完成后自行解除注释
import Login from './pages/login/LoginPage.vue'
import Home from './pages/home/Home.vue'
//测试用的EmployeeInfo页面
import EmployeeInfo from '@/pages/employee_management/EmployeeInfo.vue'
// 占位页面（后续替换为真实实现）
import Mall from './pages/mall_management/MallManagement.vue'
import Parking from './pages/parking_management/ParkingManagement.vue'
import Events from './pages/event_management/EventManagement.vue'
import Collaboration from './pages/collaboration_management/CollaborationManagement.vue'
import Equipment from './pages/equipment_management/EquipmentManagement.vue'
import Staff from './pages/staff_management/StaffManagement.vue'
import ParkingQuery from './pages/parking_query/ParkingQuery.vue'
import EventQuery from './pages/event_query/EventQuery.vue'
import AreaManagement from './pages/area_management/AreaManagement.vue'
import StoreStatusRequest from '@/pages/mall_management/StoreStatusRequest.vue';
import StoreStatusApproval from '@/pages/mall_management/StoreStatusApproval.vue';
import CreateMerchant from '@/pages/mall_management/CreateMerchant.vue';
import MallMap from '@/pages/mall_map/MallMap.vue';
import StoreManagement from '@/pages/store_management/StoreManagement.vue';
import StoreDetail from '@/pages/store_management/StoreDetail.vue';

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
  {
    path: '/employee_management',
    component: EmployeeInfo,
    meta: {
      requiresAuth: true,
      title: '员工信息', // 这个 title 会显示在页眉和菜单中
      role_need: ['员工']  // 假设只有“员工”角色能看到
    }
  },
  // 区域管理主页面（左侧跳转，右侧内容）
  {
    path: '/area',
    component: AreaManagement,
    meta: { requiresAuth: true, title: '区域管理', role_need: ['员工', '商户', '游客'] },
    children: [
      { path: '', redirect: '/area/mall-management' },
      { path: 'mall-management', component: Mall, meta: { requiresAuth: true, role_need: ['员工', '商户'] } },
      { path: 'parking-management', component: Parking, meta: { requiresAuth: true, role_need: ['员工'] } },
      { path: 'event-management', component: Events, meta: { requiresAuth: true, role_need: ['员工'] } },
      { path: 'collaboration-management', component: Collaboration, meta: { requiresAuth: true, role_need: ['员工'] } },
      { path: 'equipment-management', component: Equipment, meta: { requiresAuth: true, role_need: ['员工'] } },
      { path: 'staff-management', component: Staff, meta: { requiresAuth: true, role_need: ['员工'] } },
      { path: 'mall-map', component: MallMap, meta: { requiresAuth: true, role_need: ['游客', '商户', '员工'] } },
      { path: 'parking-query', component: ParkingQuery, meta: { requiresAuth: true, role_need: ['游客', '商户', '员工'] } },
      { path: 'event-query', component: EventQuery, meta: { requiresAuth: true, role_need: ['游客', '商户', '员工'] } },
      { path: 'store-management', component: EventQuery, meta: { requiresAuth: true, role_need: ['商户'] } },
    ]
  },
  // 商场管理页面
  {
    path: '/mall-management',
    component: Mall,
    meta: { requiresAuth: true, title: '商场管理', role_need: ['员工'] },
    children: [
      { path: 'store-status-approval', component: StoreStatusApproval, meta: { requiresAuth: true, title: '店面状态审批', role_need: ['员工'] } },
      { path: 'create-merchant', component: CreateMerchant, meta: { requiresAuth: true, title: '新增店面', role_need: ['员工'] } },
      {
        path: 'store-status-request',
        component: StoreStatusRequest,
        meta: { requiresAuth: true, title: '店面状态申请', role_need: ['商户'] },
      },
      {
        path: 'store-management',
        component: StoreManagement,
        meta: { requiresAuth: true, title: '我的店铺', role_need: ['商户'] },
      },
    ]
  },
  // 店铺管理页面
  {
    path: '/store-management',
    component: StoreManagement,
    meta: { requiresAuth: true, title: '店铺管理', role_need: ['商户'] },
  },
  {
    path: '/store-management/store-detail',
    component: StoreDetail,
    meta: { requiresAuth: true, title: '店铺详情', role_need: ['商户'] },
  },
//   //区域可视化页面
//   {path: '/area_visualization', component: Visualization, meta: { requiresAuth: true, role_need: ['员工', '商户', '游客'] } },
//   // 活动管理/活动查询页面
//   { path: '/events_management', component: Events, meta: { requiresAuth: true, role_need: ['员工', '商户', '游客'] } },
//   // 员工信息管理页面
//   { path: '/employee_management', component: Employee, meta: { requiresAuth: true, role_need: ['员工'] } },
//   // 合作方信息管理页面
//   { path: '/collaboration_management', component: Collaboration, meta: { requiresAuth: true, role_need: ['员工']} },
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
  let userStore
  let isLoggedIn = false
  try {
    userStore = useUserStore()
    isLoggedIn = !!userStore.token // 通过 token 判断用户是否登录
  } catch (err) {
    // 如果 Pinia 还未安装（例如主入口加载顺序问题），避免抛出导致页面空白
    console.warn('useUserStore() not available yet in router guard:', err)
    userStore = { token: null, role: '游客' }
    isLoggedIn = false
  }
  if (to.meta.requiresAuth && !isLoggedIn) {
    // a. 如果页面需要登录，但用户未登录，则强制跳转到登录页
    next('/login') 
  }
  else if (to.path === '/login' && isLoggedIn) {
    // b. 如果用户已登录，但又尝试访问登录页，则直接跳转到首页
    next('/')
  }
  else {
    // c. 其他所有情况（无需登录的页面，或需要登录且已登录的页面）
    // 进一步检查 role_need（如果存在）
    const requiredRoles = to.meta.role_need
    if (requiredRoles && requiredRoles.length > 0) {
      const rawUserRole = userStore.role || '游客'
      // 简单归一化：支持英文 role 名称到中文的映射
      const roleMap = {
        guest: '游客',
        visitor: '游客',
        merchant: '商户',
        shop: '商户',
        staff: '员工',
        employee: '员工'
      }
      const normalized = roleMap[String(rawUserRole).toLowerCase()] || rawUserRole

      const isAllowed = (roles, userR) => {
        return roles.includes(userR) || roles.includes(String(userR).toLowerCase())
      }

      if (!isAllowed(requiredRoles, rawUserRole) && !isAllowed(requiredRoles, normalized)) {
        console.warn(`访问被拒绝: 角色 ${rawUserRole} 无权访问 ${to.path}`)

        // 找一个对当前用户可访问的回退路由，避免无限重定向
        // 优先选择已注册的绝对路径作为回退，避免 next('mall-management') 导致无匹配
        const fallback = router.getRoutes().find(r => r.meta && Array.isArray(r.meta.role_need) && (isAllowed(r.meta.role_need, rawUserRole) || isAllowed(r.meta.role_need, normalized)) && r.path && r.path.startsWith('/'))
        let fallbackPath = fallback ? (fallback.path || '/') : '/login'

        if (fallbackPath && fallbackPath !== to.path) {
          next(fallbackPath)
        } else {
          // 如果找不到合适回退或回退正是当前路径，则中止导航
          next(false)
        }
        return
      }
    }
    next()
  }
  if (to.meta.title) {
    document.title = to.meta.title;
  }
})
export default router
