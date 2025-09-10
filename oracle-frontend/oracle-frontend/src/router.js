import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from './user/user'
// 指定页面对应的vue应用实例
import Login from './pages/login/LoginPage.vue'
import Home from './pages/home/Home.vue'
//测试用的EmployeeInfo页面
//mport EmployeeInfo from '@/pages/employee_management/EmployeeInfo.vue'
//import Cashflow from '@/pages/cashflow_management/CashflowDashboard.vue'
import AccountContent from '@/pages/account_management/AccountContent.vue'
import TempAuthEditor from '@/pages/account_management/TempAuthEditor.vue'
// import Visualization from './pages/area_visualization/App.vue'
import DeviceManagement from '@/pages/equipment_management/Equipment_management.vue'
import EmployeeManagement from './pages/employee_management/EmployeeManagement.vue'
import TotalSalary from './pages/employee_management/TotalSalary.vue'
import CashFlow from '@/pages/cashflow/cashflow_management.vue'
import CashFlowOverview from '@/pages/cashflow/CashFlowDashBoard.vue'
import CashFlowDetails from '@/pages/cashflow/CashFlowDetail.vue'

import Mall from './pages/mall_management/MallManagement.vue'
import Parking from './pages/parking_management/ParkingManagement.vue'
import Events from './pages/event_management/EventManagement.vue'
import Equipment from './pages/equipment_management/EquipmentManagement.vue'
import ParkingQuery from './pages/parking_query/ParkingQuery.vue'
import EventQuery from './pages/event_query/EventQuery.vue'
import StoreStatusRequest from '@/pages/mall_management/StoreStatusRequest.vue';
import StoreStatusApproval from '@/pages/mall_management/StoreStatusApproval.vue';
import CreateMerchant from '@/pages/mall_management/CreateMerchant.vue';
import StoreManagement from '@/pages/store_management/StoreManagement.vue';
import StoreDetail from '@/pages/store_management/StoreDetail.vue';
import GetStore from '@/pages/store_management/GetStore.vue';
import MerchantRentStatisticsReport from '@/pages/mall_management/MerchantRentStatisticsReport.vue';
import MyRentStatisticsReport from '@/pages/store_management/MyRentStatisticsReport.vue';
import ParkingManagement from './pages/parking_management/ParkingManagement.vue'
import EventManagement from './pages/event_management/EventManagement.vue'
import EquipmentManagement from './pages/equipment_management/EquipmentManagement.vue'
import VehicleManagement from './pages/parking_management/VehicleManagement.vue'
import ParkingBilling from './pages/parking_management/ParkingBilling.vue'
import ParkingReport from './pages/parking_management/ParkingReport.vue'

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
  //区域管理页面
  {
    path: '/new_area_management',
    component: () => import('@/pages/new_area_management/App.vue'),
    meta: {
      requiresAuth: true,
      title: '区划管理',
      role_need: ['员工']  // 只有员工角色可以访问
    }
  },
  //针对商户的已有店面查询页面
  {
    path: '/new_area-search',
    component: () => import('@/pages/new_area_management/AreaSearch.vue'),
    meta: {
      requiresAuth: true,
      title: '已有店面查询',
      role_need: ['商户']  // 商户角色可以访问
    }
  },
  //商场管理页面
  {
    path: '/mall-management',
    component: Mall,
    meta: { requiresAuth: true, title: '商场管理', role_need: ['员工'] },
    children: [
      { path: 'store-status-approval', component: StoreStatusApproval, meta: { requiresAuth: true, title: '店面状态审批', role_need: ['员工'] } },
      { path: 'create-merchant', component: CreateMerchant, meta: { requiresAuth: true, title: '新增店面', role_need: ['员工'] } },
      { path: 'merchant-statistics-report', component: () => import('@/pages/mall_management/MerchantStatisticsReport.vue'), meta: { requiresAuth: true, title: '商户统计报表', role_need: ['员工'] } },
      { path: 'rent-collection', name: 'RentCollection', component: () => import('@/pages/mall_management/RentCollection.vue'), meta: { requiresAuth: true, title: '租金管理', role_need: ['员工'] } },
      {
        path: 'store-status-request',
        component: StoreStatusRequest,
        meta: { requiresAuth: true, title: '店面状态申请', role_need: ['商户'] },
      },
    //  {
    //    path: 'store-management',
    //    component: StoreManagement,
    //    meta: { requiresAuth: true, title: '我的店铺', role_need: ['商户'] },
    //},
    {
    path: '/store-management/get-store',
    component: GetStore,
    meta: { requiresAuth: true, title: '申请租赁店面', role_need: ['\u5546\u6237'] }
    },
    ]
  },
  // 店铺管理页面
  {
    path: '/store-management',
    component: StoreManagement,
  meta: { requiresAuth: true, title: '店铺管理', role_need: ['商户', '员工'] },
  },
  {
    path: '/store-management/store-detail',
    component: StoreDetail,
  meta: { requiresAuth: true, title: '店铺详情', role_need: ['商户', '员工'] },
  },
  {
    path: '/store-management/my-rent-bills',
    name: 'MyRentBills',
    component: () => import('@/pages/store_management/MyRentBills.vue'),
    meta: { requiresAuth: true, title: '我的租金', role_need: ['商户'] }
  },
  {
    path: '/mall-management/merchant-rent-statistics-report',
    name: 'MerchantRentStatisticsReport',
    component: MerchantRentStatisticsReport,
    meta: { requiresAuth: true, title: '商户租金统计报表', role_need: ['员工'] }
  },
  {
    path: '/store-management/my-rent-statistics-report',
    name: 'MyRentStatisticsReport',
    component: MyRentStatisticsReport,
    meta: { requiresAuth: true, title: '我的租金统计', role_need: ['商户'] }
  },
  // 活动管理/活动查询页面
  { path: '/event-query',
    //redirect: '/area/event-query',
    component: EventQuery,
    meta: {
      requiresAuth: true,
      title: '活动查询',
      role_need: ['员工', '商户', '游客']
    }
   },
  {
    path: '/event-management',
      //redirect: '/area/event-management',
    component: EventManagement,
    meta: {
      requiresAuth: true,
      title: '活动管理',
      role_need: ['员工']
    }
   },
  //合作方管理页面
  {
    path: '/collaboration_management',
    component: () => import('@/pages/collaboration_management/App.vue'),
    meta: {
      requiresAuth: true,
      title: '合作方管理',
      role_need: ['员工']  // 只有员工角色可以访问
    }
  },
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
  // 员工工资支出页面
  {
    path: '/cashflow_management/total_salary',
    component: TotalSalary,
    meta: {
      requiresAuth: true,
      title: '员工工资支出',
      role_need: ['员工'],
      accessAuth: 2
    }
  },
  // 停车场管理 (顶层路由)
  {
    path: '/parking-management',
    component: ParkingManagement,
    meta: { requiresAuth: true, title: '停车场管理', role_need: ['员工'] },
    children: [
      { path: 'vehicle-management', component: VehicleManagement, meta: { requiresAuth: true, role_need: ['员工'], title: '车辆管理' } },
      { path: 'parking-billing', component: ParkingBilling, meta: { requiresAuth: true, role_need: ['员工'], title: '计费管理' } },
      { path: 'parking-report', component: ParkingReport, meta: { requiresAuth: true, role_need: ['员工'], title: '停车报表' } },
    ]
  },
  // 车位查询 (顶层路由)
  {
    path: '/parking-query',
    component: ParkingQuery,
    meta: { requiresAuth: true, title: '车位查询', role_need: ['员工', '商户', '游客'] }
  },
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
  {
    path: '/cashflow',
    component: CashFlow,  
    meta: {
      requiresAuth: true,
      title: '现金流信息',
      role_need: ['员工']
    },
    children: [
      {
        path: '',
        name: 'CashFlowOverview',  
        component: CashFlowOverview,
        meta: { title: '现金流总览' }
      },
      {
        path: 'detail/:module',
        name: 'CashFlowDetail',    
        component: CashFlowDetails,
        meta: { title: '详情页' },
        props: true
      }
    ]
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
