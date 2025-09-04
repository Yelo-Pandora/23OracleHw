import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from './user/user'
// 指定页面对应的vue应用实例
import Login from './pages/login/LoginPage.vue'
import Home from './pages/home/Home.vue'
//测试用的EmployeeInfo页面
import EmployeeInfo from '@/pages/employee_management/EmployeeInfo.vue'
// 真实页面
import Mall from './pages/mall_management/MallManagement.vue'
import Parking from './pages/parking_management/ParkingManagement.vue'
import ParkingQuery from './pages/parking_query/ParkingQuery.vue'
import ParkingBilling from './pages/parking_billing/ParkingBilling.vue'
import VehicleManagement from './pages/vehicle_management/VehicleManagement.vue'
import ParkingReport from './pages/parking_report/ParkingReport.vue'

// 定义路由
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
      title: '员工信息',
      role_need: ['员工']
    }
  },
  // 商场管理
  {
    path: '/mall-management',
    component: Mall,
    meta: { requiresAuth: true, title: '商场管理', role_need: ['员工', '商户', '游客'] }
  },
  // 停车场管理
  {
    path: '/parking-management',
    component: Parking,
    meta: { requiresAuth: true, title: '停车场管理', role_need: ['员工'] }
  },
  // 停车收费
  {
    path: '/parking-billing',
    component: ParkingBilling,
    meta: { requiresAuth: true, title: '停车收费', role_need: ['员工'] }
  },
  // 停车查询
  {
    path: '/parking-query',
    component: ParkingQuery,
    meta: { requiresAuth: true, title: '停车查询', role_need: ['游客', '商户', '员工'] }
  },
  // 停车报表
  {
    path: '/parking-report',
    component: ParkingReport,
    meta: { requiresAuth: true, title: '停车报表', role_need: ['员工'] }
  },
  // 车辆管理
  {
    path: '/vehicle-management',
    component: VehicleManagement,
    meta: { requiresAuth: true, title: '车辆管理', role_need: ['员工'] }
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

//检查登陆状态
router.beforeEach((to, from, next) => {
  let userStore
  let isLoggedIn = false
  try {
    userStore = useUserStore()
    isLoggedIn = !!userStore.token
  } catch (err) {
    console.warn('useUserStore() not available yet in router guard:', err)
    userStore = { token: null, role: '游客' }
    isLoggedIn = false
  }
  if (to.meta.requiresAuth && !isLoggedIn) {
    next('/login') 
  }
  else if (to.path === '/login' && isLoggedIn) {
    next('/')
  }
  else {
    const requiredRoles = to.meta.role_need
    if (requiredRoles && requiredRoles.length > 0) {
      const rawUserRole = userStore.role || '游客'
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
        const fallback = router.getRoutes().find(r => r.meta && Array.isArray(r.meta.role_need) && (isAllowed(r.meta.role_need, rawUserRole) || isAllowed(r.meta.role_need, normalized)))
        const fallbackPath = fallback ? (fallback.path || '/') : '/login'

        if (fallbackPath && fallbackPath !== to.path) {
          next(fallbackPath)
        } else {
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
