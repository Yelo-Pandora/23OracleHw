<template>
  <div class="event-management">
    <!-- 活动类型切换 -->
    <div class="event-type-switch">
      <button 
        class="type-btn" 
        :class="{ active: activeTab === 'sale' }"
        @click="switchTab('sale')">
        促销活动管理
      </button>
      <button 
        class="type-btn" 
        :class="{ active: activeTab === 'venue' }"
        @click="switchTab('venue')">
        场地活动管理
      </button>
      <button 
        class="type-btn" 
        :class="{ active: activeTab === 'collaboration' }"
        @click="switchTab('collaboration')">
        合作方管理
      </button>
    </div>

    <!-- 促销活动管理 -->
    <div v-if="activeTab === 'sale'" class="tab-content">
      <SaleEventManagement />
    </div>

    <!-- 场地活动管理 -->
    <div v-if="activeTab === 'venue'" class="tab-content">
      <VenueEventManagement />
    </div>

    <!-- 合作方管理 -->
    <div v-if="activeTab === 'collaboration'" class="tab-content">
      <CollaborationManagement />
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import SaleEventManagement from './components/SaleEventManagement.vue'
import VenueEventManagement from './components/VenueEventManagement.vue'
import CollaborationManagement from './components/CollaborationManagement.vue'

// 当前激活的标签页
const activeTab = ref('sale')

// 切换标签页
const switchTab = (tab) => {
  activeTab.value = tab
}
</script>

<style scoped>
.event-management {
  padding: 20px;
}

.event-type-switch {
  display: flex;
  gap: 8px;
  margin-bottom: 24px;
  background: #f5f7fa;
  padding: 4px;
  border-radius: 8px;
  width: fit-content;
}

.type-btn {
  padding: 12px 24px;
  border: none;
  background: transparent;
  color: #606266;
  font-size: 16px;
  font-weight: 500;
  cursor: pointer;
  border-radius: 6px;
  transition: all 0.3s ease;
  white-space: nowrap;
}

.type-btn.active {
  background: #409eff;
  color: #ffffff;
  box-shadow: 0 2px 4px rgba(64, 158, 255, 0.3);
}

.type-btn:hover:not(.active) {
  background: #ecf5ff;
  color: #409eff;
}

.tab-content {
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
  min-height: 600px;
}
</style>