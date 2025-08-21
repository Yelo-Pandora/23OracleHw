<template>
  <!--
    <router-view> 是一个一个占位符。
    根据当前的 URL，渲染匹配到的组件。
    当 URL 是 '/login' 时, 这里渲染的就是 LoginBox.vue。
    当 URL 是 '/' 时, 这里渲染的就是 Home.vue。
  -->
  <router-view></router-view>
</template>

<script setup>
</script>

<style>
    /*
    全局样式，对整个应用生效。
    */
    body, html {
      margin: 0;
      padding: 0;
      height: 100%;
      font-family: 'Microsoft YaHei', 'Helvetica Neue', Arial, sans-serif;
      overflow: hidden;
      background-color: #f4f4f4; /* 背景加载前的底色 */
    }

    #app {
      height: 100%;
    }
</style>
