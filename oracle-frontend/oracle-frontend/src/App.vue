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
import { onMounted } from 'vue';

onMounted(() => {
  // 2. 鼠标点击波纹效果
  document.addEventListener('click', function (e) {
    let ripple = document.createElement('div');
    ripple.className = 'ripple';
    document.body.appendChild(ripple);

    ripple.style.left = `${e.clientX}px`;
    ripple.style.top = `${e.clientY}px`;

    ripple.addEventListener('animationend', () => {
      document.body.removeChild(ripple);
    });
  });
});
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

    /* 1. 动态渐变背景 */
    body {
        background: linear-gradient(45deg, #f5f7fa, #c3cfe2, #f5f7fa, #c3cfe2);
        background-size: 400% 400%;
        animation: gradientBG 15s ease infinite;
    }

    @keyframes gradientBG {
        0% { background-position: 0% 50%; }
        50% { background-position: 100% 50%; }
        100% { background-position: 0% 50%; }
    }

    /* 2. 鼠标点击波纹 */
    .ripple {
        position: fixed;
        border-radius: 50%;
        pointer-events: none;
        transform: translate(-50%, -50%);
        animation: ripple-animation 0.7s linear;
        border: 2px solid var(--primary-color);
    }

    @keyframes ripple-animation {
        0% {
            width: 0;
            height: 0;
            opacity: 0.5;
        }
        100% {
            width: 100px;
            height: 100px;
            opacity: 0;
        }
    }

    /* 3. 按钮悬停放大 & 4. 按钮点击效果 */
    button, .el-button {
        transition: all 0.2s ease-in-out;
    }

    button:hover, .el-button:hover {
        transform: scale(1.05);
        box-shadow: 0 6px 12px rgba(0, 0, 0, 0.1);
    }

    button:active, .el-button:active {
        transform: scale(0.98);
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.08);
    }

    /* 5. 输入框聚焦边框 & 6. 输入框聚焦阴影 */
    input[type="text"], input[type="password"], input[type="email"], textarea, .el-input__inner {
        transition: border-color 0.3s ease, box-shadow 0.3s ease;
    }

    input[type="text"]:focus, input[type="password"]:focus, input[type="email"]:focus, textarea:focus, .el-input__inner:focus {
        border-color: var(--primary-color) !important;
        box-shadow: 0 0 8px rgba(26, 188, 156, 0.3) !important;
    }

    /* 7. 链接悬停下划线 */
    a {
        position: relative;
        text-decoration: none;
        color: inherit;
        transition: color 0.2s;
    }

    a:hover {
        color: var(--primary-color);
    }

    a::after {
        content: '';
        position: absolute;
        width: 100%;
        height: 2px;
        bottom: -4px;
        left: 0;
        background-color: var(--primary-color);
        transform: scaleX(0);
        transform-origin: center;
        transition: transform 0.3s ease-out;
    }

    a:hover::after {
        transform: scaleX(1);
    }

    /* 8. 卡片悬停浮动 */
    .el-card, .board-layout { /* 假设 .el-card 和 .board-layout 是卡片类 */
        transition: transform 0.3s ease, box-shadow 0.3s ease;
    }

    .el-card:hover, .board-layout:hover {
        transform: translateY(-5px);
        box-shadow: var(--card-shadow), 0 8px 20px rgba(0, 0, 0, 0.1);
    }

    /* 9. 图片悬停灰度 */
    img {
        transition: filter 0.3s ease;
    }

    img:hover {
        filter: grayscale(80%);
    }

    /* 10. 通知图标脉冲 */
    .notification-icon {
        animation: pulse 2s infinite;
    }

    @keyframes pulse {
        0% { box-shadow: 0 0 0 0 rgba(26, 188, 156, 0.4); }
        70% { box-shadow: 0 0 0 10px rgba(26, 188, 156, 0); }
        100% { box-shadow: 0 0 0 0 rgba(26, 188, 156, 0); }
    }

    /* 11. 元素淡入效果 */
    @keyframes fadeIn {
        from { opacity: 0; transform: translateY(10px); }
        to { opacity: 1; transform: translateY(0); }
    }

    .fade-in {
        animation: fadeIn 0.5s ease-out forwards;
    }

    /* 12. 自定义滚动条 */
    ::-webkit-scrollbar {
        width: 8px;
        height: 8px;
    }

    ::-webkit-scrollbar-track {
        background: #f1f1f1;
    }

    ::-webkit-scrollbar-thumb {
        background: #ccc;
        border-radius: 4px;
    }

    ::-webkit-scrollbar-thumb:hover {
        background: #aaa;
    }

    #app {
      height: 100%;
    }
</style>
