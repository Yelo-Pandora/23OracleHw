
<template>
	<DashboardLayout>
		<div class="area-management">
			<aside class="nav">
				<h3>区域管理</h3>
				<ul>
					<li v-for="item in navItems" :key="item.path">
						<router-link :to="item.path">{{ item.label }}</router-link>
					</li>
				</ul>
			</aside>

			<section class="content">
				<!-- 如果子路由存在，显示子路由页面，否则显示占位信息 -->
				<router-view />
				<div v-if="!hasSubRoute" class="placeholder">
					<h2>{{ welcomeTitle }}</h2>
					<p>请选择左侧的项以进入对应的管理/查看页面。</p>
				</div>
			</section>
		</div>
	</DashboardLayout>
</template>

<script setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import DashboardLayout from '@/components/BoardLayout.vue';
import { useUserStore } from '@/stores/user';

// 从 Pinia 获取用户角色，遵循 README 中的约定
const userStore = useUserStore();
const role = computed(() => userStore.role || '游客');

// 根据不同角色返回不同的侧边导航项（label: 显示名, path: 待跳转路由）
// 说明/假设：路由名/路径基于项目常见规则，若项目中路由不同请告知我来调整
const navItems = computed(() => {
		if (role.value === '员工') {
			return [
				{ label: '首页', path: '/' },
				{ label: '商场管理', path: '/area/mall-management' },
				{ label: '停车场管理', path: '/area/parking-management' },
				{ label: '活动管理（含合作方）', path: '/area/event-management' },
				{ label: '员工管理', path: '/area/staff-management' },
				{ label: '设备管理', path: '/area/equipment-management' },
			];
		}

	if (role.value === '商户') {
			return [
				{ label: '首页', path: '/' },
				{ label: '店铺管理（仅限己方）', path: '/area/store-management' },
				{ label: '车位查询', path: '/area/parking-query' },
				{ label: '活动查询', path: '/area/event-query' },
			];
	}

	// 游客
		return [
			{ label: '首页', path: '/' },
			{ label: '商场平面图查看', path: '/area/mall-map' },
			{ label: '车位查询', path: '/area/parking-query' },
			{ label: '活动查询', path: '/area/event-query' },
		];
});

const route = useRoute();
// 简单判定是否存在子路由（若路径严格为 /area 或 /area/ 则视为无子路由）
const hasSubRoute = computed(() => !/^\/area(?:\/)?$/.test(route.path));

const welcomeTitle = computed(() => {
	if (role.value === '员工') return '员工 — 区域管理入口';
	if (role.value === '商户') return '商户 — 区域入口';
	return '游客 — 区域查看入口';
});
</script>

<style scoped>
.area-management {
	display: flex;
	gap: 16px;
	padding: 16px;
}

.nav {
	width: 220px;
	background: #fff;
	border: 1px solid #e6e6e6;
	border-radius: 4px;
	padding: 12px;
}

.nav h3 {
	margin: 0 0 8px 0;
	font-size: 16px;
}

.nav ul {
	list-style: none;
	padding: 0;
	margin: 0;
}

.nav li {
	margin: 6px 0;
}

.nav a {
	color: #333;
	text-decoration: none;
}

.nav a.router-link-active {
	color: #409eff;
	font-weight: 600;
}

.content {
	flex: 1;
	background: #fff;
	border: 1px solid #e6e6e6;
	border-radius: 4px;
	min-height: 360px;
	padding: 16px;
}

.placeholder {
	display: flex;
	flex-direction: column;
	gap: 8px;
	align-items: flex-start;
}
</style>
