<template>
    <DashboardLayout>
        <div class="area-management">

            <section class="content">
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
import { useRoute, useRouter } from 'vue-router';
import DashboardLayout from '@/components/BoardLayout.vue';
import { useUserStore } from '@/stores/user';

const userStore = useUserStore();
const role = computed(() => userStore.role || '游客');

// const navItems = computed(() => {
//     if (role.value === '员工') {
//         return [
//             { label: '首页', path: '/' },
//             { label: '商场管理', path: '/area/mall-management' },
//             { label: '停车场管理', path: '/area/parking-management' },
//             { label: '活动管理（含合作方）', path: '/area/event-management' },
//             { label: '设备管理', path: '/area/equipment-management' },
//         ];
//     }
//     if (role.value === '商户') {
//         return [
//             { label: '首页', path: '/' },
//             { label: '车位查询', path: '/area/parking-query' },
//             { label: '活动查询', path: '/area/event-query' },
//         ];
//     }
//     return [
//         { label: '首页', path: '/' },
//         { label: '车位查询', path: '/area/parking-query' },
//         { label: '活动查询', path: '/area/event-query' },
//     ];
// });

const route = useRoute();
const router = useRouter();
const hasSubRoute = computed(() => !/^\/area(?:\/)?$/.test(route.path));
const welcomeTitle = computed(() => {
    if (role.value === '员工') return '员工 — 区域管理入口';
    if (role.value === '商户') return '商户 — 区域入口';
    return '游客 — 区域查看入口';
});

const goTo = (path) => {
    router.push(path);
};

const isActive = (path) => route.path === path;
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
    text-align: center;
}

.nav-btn-group {
    display: flex;
    flex-direction: column;
    gap: 12px;
    align-items: center;
}

.nav-btn {
    width: 180px;
    padding: 10px 0;
    border: none;
    border-radius: 6px;
    background: #f5f7fa;
    color: #333;
    font-size: 15px;
    cursor: pointer;
    transition: box-shadow 0.2s, transform 0.2s, background 0.2s;
    text-align: center;
    outline: none;
}

.nav-btn-text {
    display: block;
    width: 100%;
    text-align: center;
}

.nav-btn:hover {
    background: #e3f0fc;
    box-shadow: 0 2px 12px #409eff44;
    transform: translateY(-2px) scale(1.04);
}

.nav-btn:active {
    background: #cbe6ff;
    box-shadow: 0 2px 12px #409eff88;
    transform: scale(0.98);
}

.nav-btn.active {
    background: #409eff;
    color: #fff;
    font-weight: bold;
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