import { ref, computed } from 'vue';
import axios from 'axios';

// 获取当前登录用户的个人信息
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


// 负责系统账户列表的数据获取、筛选和搜索
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
      staffAccounts.value = allAccount.filter(acc => acc.Identity === '员工' && acc.StaffInfo);
      tenantAccounts.value = allAccount.filter(acc => acc.Identity === '商户' && acc.StoreInfo);
    } catch (error) {
      console.error("获取所有账户列表失败:", error);
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
  const selectedStaffIds = ref(new Set());
  const selectedTenantIds = ref(new Set());

  // 计算是否所有当前显示的员工都被选中
  const isAllStaffSelected = computed(() => {
    const displayedIds = filteredStaff.value.map(acc => acc.StaffInfo.StaffId);
    return displayedIds.length > 0 && displayedIds.every(id => selectedStaffIds.value.has(id));
  });

  // 切换员工全选框的选中状态
  const toggleAllStaffSelection = () => {
    if (isAllStaffSelected.value) {
      selectedStaffIds.value.clear();
    } else {
      filteredStaff.value.forEach(acc => selectedStaffIds.value.add(acc.StaffInfo.StaffId));
    }
  };

  // 计算是否所有当前显示的商户都被选中
  const isAllTenantSelected = computed(() => {
    const displayedIds = filteredTenants.value.map(acc => acc.StoreInfo.StoreId);
    return displayedIds.length > 0 && displayedIds.every(id => selectedTenantIds.value.has(id));
  });

  // 切换商户全选框的选中状态
  const toggleAllTenantSelection = () => {
    if (isAllTenantSelected.value) {
      selectedTenantIds.value.clear();
    } else {
      filteredTenants.value.forEach(acc => selectedTenantIds.value.add(acc.StoreInfo.StoreId));
    }
  };

  return {
    selectedStaffIds,
    selectedTenantIds,
    isAllStaffSelected,
    toggleAllStaffSelection,
    isAllTenantSelected,
    toggleAllTenantSelection
  };
}

// 该函数负责所有按钮的操作
export function useAccountActions(userStore, selectedStaffIds, selectedTenantIds) {
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
    const addAccount = () => alert('功能: 打开添加账号表单');
    const deleteAccount = () => {
        if (confirm('您确定要删除选中的账号吗？')) {
            alert(`功能: 批量删除员工ID: ${[...selectedStaffIds.value].join(', ')} 和 商户ID: ${[...selectedTenantIds.value].join(', ')}`);
        }
    };
    const linkAccount = () => alert('功能: 打开账号关联界面');
    const grantTempPermission = () => alert('功能: 打开临时权限授予面板');

    return { modifyInfo, addAccount, deleteAccount, linkAccount, grantTempPermission };
}
