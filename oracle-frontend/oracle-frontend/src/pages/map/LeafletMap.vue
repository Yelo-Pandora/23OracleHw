<template>
  <BoardLayout>
    <div class="placeholder">
      <h2>Leaflet 地图暂时未启用</h2>
      <p>请使用“可编辑地图”页面进行体验与编辑。</p>
      <router-link to="/editable_map">前往可编辑地图</router-link>
    </div>
  </BoardLayout>
  
</template>

<script setup>
import BoardLayout from '@/components/BoardLayout.vue'
</script>

<style scoped>
.placeholder { padding: 24px; border: 1px dashed #ddd; background: #fafafa; }
.placeholder h2 { margin: 0 0 8px; font-size: 18px; }
</style>


