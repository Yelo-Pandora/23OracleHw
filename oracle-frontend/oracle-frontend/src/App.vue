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
    在这里定义全局 CSS 变量（:root），保证组件中使用的 var(--primary-color) 等能正常解析。
    如果把 :root 放到带 scoped 的组件样式中，会被编译成带属性选择器的规则，从而无法匹配真正的根元素，导致变量无效
   （进而令 background-color: var(...) 解析为空，按钮在未 hover 时背景透明）。
    */
    :root {
      --primary-color: #1abc9c;
      --success-color: #2ecc71;
      --warning-color: #f39c12;
      --danger-color: #e74c3c;
      --card-bg: #ffffff;
      --card-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
      --border-radius: 12px;
      --input-border-color: #dee2e6;
    }

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
