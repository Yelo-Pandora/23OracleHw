import { ref, computed } from 'vue';
import axios from 'axios';

// 该函数模块负责获取当前登录用户的个人信息
export function useCurrentUserProfile(userStore) {
  const StaffInfo = ref(null);
  const StoreInfo = ref(null);
  const isLoading = ref(true);

  const fetchDetails = async () => {
    const account = userStore.userInfo?.account;
    console.log("当前用户账号:", account);
    if (!account) {
      isLoading.value = false;
      return;
    }
    try {
      isLoading.value = true;
      const response = await axios.get(`/api/Accounts/info/detailed/${account}`);
      StaffInfo.value = response.data.StaffInfo;
      StoreInfo.value = response.data.StoreInfo;
    } catch (error) {
      console.error("获取个人详细信息失败:", error);
    } finally {
      isLoading.value = false;
    }
  };

  return {
    currentUserStaffInfo: StaffInfo,
    currentUserStoreInfo: StoreInfo,
    isLoadingProfile: isLoading,
    fetchCurrentUserDetails: fetchDetails
  };
}


// 该函数模块负责系统账户列表的数据获取、筛选和搜索
export function useAccountList() {
  const staffAccounts = ref([]);
  const tenantAccounts = ref([]);
  const searchFilter = ref('Account');
  const searchQuery = ref('');

  // 获取并处理所有账户数据
  const fetchAll = async () => {
    try {
      const response = await axios.get('/api/Accounts/AllAccount/detailed');
      const allAccount = response.data;
      staffAccounts.value = allAccount.filter(acc => acc.Identity === '员工');
      tenantAccounts.value = allAccount.filter(acc => acc.Identity === '商户');
    } catch (error) {
      console.error("获取所有账户列表失败:", error);
      staffAccounts.value = [];
      tenantAccounts.value = [];
    }
  };
  // 根据搜索条件过滤员工和商户账户
  const filteredStaffAccounts = computed(() => {
    if (!searchQuery.value) return staffAccounts.value;
    const query = searchQuery.value.toLowerCase();
    return staffAccounts.value.filter(account => {
        const targetValue = account[searchFilter.value] || account.StaffInfo[searchFilter.value];
        return targetValue && String(targetValue).toLowerCase().includes(query);
    });
  });

  const filteredTenantAccounts = computed(() => {
    if (!searchQuery.value) return tenantAccounts.value;
    const query = searchQuery.value.toLowerCase();
    return tenantAccounts.value.filter(account => {
        const targetValue = account[searchFilter.value] || account.StoreInfo[searchFilter.value];
        return targetValue && String(targetValue).toLowerCase().includes(query);
    });
  });
  return {
    searchFilter,
    searchQuery,
    fetchAndProcessAccounts: fetchAll,
    filteredStaffAccounts,
    filteredTenantAccounts,
  };
}


// 该函数负责列表的勾选交互逻辑，依赖于过滤后的数据
export function useAccountSelection(filteredStaff, filteredTenants) {
  const selectedStaffAccounts = ref(new Set());
  const selectedTenantAccounts = ref(new Set());

  // 计算是否所有当前显示的员工都被选中
  const isAllStaffSelected = computed(() => {
    const displayedAccounts = filteredStaff.value.map(acc => acc.Account);
    return displayedAccounts.length > 0 && displayedAccounts.every(accStr => selectedStaffAccounts.value.has(accStr));
  });

  // 切换员工全选框的选中状态
  const toggleAllStaffSelection = () => {
    if (isAllStaffSelected.value) {
      selectedStaffAccounts.value.clear();
    } else {
      filteredStaff.value.forEach(acc => selectedStaffAccounts.value.add(acc.Account));
    }
  };

  // 计算是否所有当前显示的商户都被选中
  const isAllTenantSelected = computed(() => {
    const displayedAccounts = filteredTenants.value.map(acc => acc.Account);
    return displayedAccounts.length > 0 && displayedAccounts.every(accStr => selectedTenantAccounts.value.has(accStr));
  });

  // 切换商户全选框的选中状态
  const toggleAllTenantSelection = () => {
    if (isAllTenantSelected.value) {
      selectedTenantAccounts.value.clear();
    } else {
      filteredTenants.value.forEach(acc => selectedTenantAccounts.value.add(acc.Account));
    }
  };

  return {
    selectedStaffAccounts: selectedStaffAccounts,
    selectedTenantAccounts: selectedTenantAccounts,
    isAllStaffSelected,
    toggleAllStaffSelection,
    isAllTenantSelected,
    toggleAllTenantSelection
  };
}

// 该函数模块负责所有按钮的操作
export function useAccountActions(userStore, fetchAndProcessAccounts, selectedStaffIds, selectedTenantIds) {
    // 【修改】实现 modifyInfo 的完整逻辑
    const modifyInfo = async () => {
      const currentUser = userStore.userInfo;
      if (!currentUser || !currentUser.account) {
        alert('无法获取当前用户信息，请重新登录。');
        return;
      }

      // 获取用户输入
      const newUsername = prompt("请输入新的用户名:", currentUser.username || '');
      // 如果用户点击 "取消"，prompt 返回 null，我们中止操作
      if (newUsername === null) {
        return;
      }

      // --- 步骤 2: 构造 API 请求 ---
      const accountId = currentUser.account;

      // 构造请求体，只填充需要修改的字段。
      // 其他字段后端会忽略，因为操作员就是用户本身。
      const requestBody = {
        USERNAME: newUsername.trim(),
        // 只有当用户输入了新密码时，才发送 PASSWORD 字段
        PASSWORD: null,
        // 以下字段我们不修改
        ACCOUNT: currentUser.account,
        IDENTITY: null,
        AUTHORITY: null,
      };

      // 构造查询请求
      const url = `/api/Accounts/alter/${accountId}`;
      const params = {
        operatorAccountId: accountId
      };

      // 发送PATCH请求
      try {
        console.log('正在发送修改请求:', { url, params, requestBody });

        await axios.patch(url, requestBody, { params });

        alert('用户名修改成功！');

        const currentToken = userStore.token;
        const currentRole = userStore.role;
        const currentUserInfo = userStore.userInfo;

        // 创建一个新的 userInfo 对象，只更新其中的 username
        const updatedUserInfo = {
          ...currentUserInfo,
          username: newUsername.trim()
        };

        // 使用现有的 login action 来更新userStore和localStorage
        userStore.login(currentToken, currentRole, updatedUserInfo);

      } catch (error) {
        // 失败处理
        console.error("修改信息失败:", error);
        alert(`修改失败: ${error.response?.data?.message || error.message}`);
      }
    };
    const deleteAccount = async () => {
      // 1. 合并所有选中的账号ID
      const allSelectedAccounts = [
        ...selectedStaffIds.value,
        ...selectedTenantIds.value
      ];

      // 2. 检查是否有选中项
      if (allSelectedAccounts.length === 0) {
        alert('请至少选择一个要删除的账号。');
        return;
      }

      // 3. 弹出最终确认对话框
      if (!confirm(`您确定要删除选中的 ${allSelectedAccounts.length} 个账号吗？\n此操作不可逆！`)) {
        return;
      }

      const operatorAccountId = userStore.userInfo?.account;
      if (!operatorAccountId) {
        alert('无法获取操作员信息，请重新登录。');
        return;
      }

      // 4. 遍历所有选中的账号，并为每个账号创建一个删除请求 (Promise)
      const deletePromises = allSelectedAccounts.map(accountId => {
        const url = `/api/Accounts/delete/${accountId}`;
        const params = { operatorAccountId };
        return axios.delete(url, { params });
      });

      // 5. 使用 Promise.allSettled 来等待所有删除请求完成
      //    这能确保即使部分请求失败，其他请求也会继续执行
      try {
        const results = await Promise.allSettled(deletePromises);

        // 6. 分析删除结果，并向用户反馈
        const successes = results.filter(r => r.status === 'fulfilled');
        const failures = results.filter(r => r.status === 'rejected');

        let alertMessage = '';
        if (successes.length > 0) {
          alertMessage += `${successes.length} 个账号删除成功。\n`;
        }
        if (failures.length > 0) {
          alertMessage += `${failures.length} 个账号删除失败。详情请查看控制台日志。`;
          console.error("以下账号删除失败:", failures);
        }

        alert(alertMessage);

      } catch (error) {
        {
          // 理论上 Promise.allSettled 不会进入这个 catch，但作为保险
          console.error("批量删除时发生意外错误:", error);
          alert("执行批量删除时发生意外错误。");
        }

        // 7. 【关键】无论成功或失败，最后都刷新列表
        await fetchAndProcessAccounts();

        // 8. 清空选中状态
        selectedStaffIds.value.clear();
        selectedTenantIds.value.clear();
      };
    }
    const linkAccount = (account) => alert('功能: 打开账号关联界面');
    const unlinkAccount = (account) => alert('功能: 打开取消关联界面');
    const grantTempPermission = (account) => alert('功能: 打开临时权限授予面板');
    const editAccount = async (accountToEdit) => {
      if (!accountToEdit) return;

      // --- 步骤 1: 获取用户输入的新信息 ---
      // 在实际项目中，这里应该弹出一个漂亮的模态框(Modal)组件，并预先填充好旧数据
      // 我们暂时使用 prompt() 来模拟这个过程

      const newUsername = prompt("请输入新的用户名:", accountToEdit.Username);
      if (newUsername === null) return; // 用户取消

      const newIdentity = prompt("请输入新的身份 (员工/商户):", accountToEdit.Identity);
      if (newIdentity === null) return;

      // 只有管理员才能修改权限
      let newAuthority = accountToEdit.Authority;
      if (userStore.userInfo?.authority === 1) {
        const authorityInput = prompt("请输入新的权限等级 (例如 0 或 1):", accountToEdit.Authority);
        if (authorityInput === null) return;
        newAuthority = parseInt(authorityInput, 10); // 将输入转为数字
        if (isNaN(newAuthority)) {
          alert("无效的权限等级，操作已取消。");
          return;
        }
      }

      // --- 步骤 2: 构造 API 请求 ---
      const currAccount = accountToEdit.Account;
      const operatorAccountId = userStore.userInfo.account;

      // 根据 AccountUpdateDto 构造请求体
      const requestBody = {
        ACCOUNT: currAccount,
        USERNAME: newUsername.trim(),
        IDENTITY: newIdentity.trim(),
        AUTHORITY: newAuthority,
        PASSWORD: null, // 我们不在这里修改密码
      };

      const url = `/api/Accounts/alter/${currAccount}`;
      const params = { operatorAccountId };

      // --- 步骤 3: 发送 PATCH 请求 ---
      try {
        await axios.patch(url, requestBody, { params });
        alert(`账号 ${currAccount} 的信息已成功更新！`);

        // --- 步骤 4: 【关键】成功后，调用传入的函数刷新整个列表 ---
        await fetchAndProcessAccounts();

      } catch (error) {
        console.error(`更新账号 ${currAccount} 失败:`, error);
        alert(`更新失败: ${error.response?.data?.message || error.message}`);
      }
    };

    return { modifyInfo, deleteAccount, linkAccount,unlinkAccount, grantTempPermission, editAccount };
}
