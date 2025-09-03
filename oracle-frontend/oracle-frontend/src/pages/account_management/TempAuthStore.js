// src/stores/tempAuthStore.js
import { defineStore } from 'pinia';
import axios from 'axios';
import { useUserStore } from '@/user/user'; // 假设 user store 在这里

export const useTempAuthStore = defineStore('tempAuth', {
  state: () => ({
    targetAccount: null,      // 正在被编辑的账号
    allEvents: [],            // 所有活动列表
    tempAuthorities: [],      // 所有临时权限项
    isLoading: false,
    error: null,
  }),

  getters: {
    // 计算出已授权的活动列表
    grantedEvents: (state) => {
      if (!state.targetAccount || state.tempAuthorities.length === 0) return [];
      const grantedEventIds = new Set(
        state.tempAuthorities
          .filter(auth => auth.ACCOUNT === state.targetAccount)
          .map(auth => auth.EVENT_ID)
      );
      return state.allEvents.filter(event => grantedEventIds.has(event.EVENT_ID));
    },

    // 计算出未授权的活动列表
    ungrantedEvents: (state) => {
      if (!state.targetAccount || state.allEvents.length === 0) return [];
      const grantedEventIds = new Set(state.grantedEvents.map(event => event.EVENT_ID));
      return state.allEvents.filter(event => !grantedEventIds.has(event.EVENT_ID));
    },
  },

  actions: {
    // 初始化页面数据的总入口
    async initialize(accountId) {
      this.isLoading = true;
      this.targetAccount = accountId;
      this.error = null;
      try {
        const [eventsRes, tempAuthsRes] = await Promise.all([
          axios.get('/api/Accounts/events'), // 获取所有活动
          axios.get('/api/Staff/AllTempAuthorities') // 获取所有临时权限
        ]);
        this.allEvents = eventsRes.data;
        this.tempAuthorities = tempAuthsRes.data;
      } catch (err) {
        this.error = "加载数据失败，请重试。";
        console.error(err);
      } finally {
        this.isLoading = false;
      }
    },

    // 建立临时权限
    async grantAuthority(eventId, tempAuthorityValue) {
      const userStore = useUserStore();
      const operatorAccount = userStore.userInfo?.account;
      if (!operatorAccount) return Promise.reject("无法获取操作员信息。");

      const requestBody = {
        account: this.targetAccount,
        eventId: eventId,
        tempAuthority: tempAuthorityValue
      };
      console.log('Granting authority with body:', requestBody);
      await axios.post('/api/Staff/temporary_authority', requestBody, {
        params: { operatorAccount: operatorAccount }
      });
      // 成功后，重新获取所有权限以刷新列表
      await this.initialize(this.targetAccount);
    },

    // 撤销临时权限
    async revokeAuthority(eventId) {
      const userStore = useUserStore();
      const operatorAccount = userStore.userInfo?.account;
      if (!operatorAccount) return Promise.reject("无法获取操作员信息。");

      const params = {
        operatorAccount: operatorAccount,
        staffAccount: this.targetAccount,
        eventId: eventId,
      };
      await axios.delete('/api/Staff/revoke_temporary_authority', { params });
      // 成功后，重新获取所有权限以刷新列表
      await this.initialize(this.targetAccount);
    },

    // 组件卸载时清理状态
    cleanup() {
      this.$reset();
    }
  },
});
