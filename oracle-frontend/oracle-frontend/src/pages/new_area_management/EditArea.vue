<template>
  <div class="edit-area">
    <h2>编辑区域信息</h2>
    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <form v-else @submit.prevent="submitForm" class="area-form">
      <div class="form-group">
        <label>区域ID</label>
        <div class="readonly-field">{{ formData.areaId }}</div>
      </div>

      <div class="form-group">
        <label for="areaName">区域名称 <span class="required">*</span></label>
        <input
          type="text"
          id="areaName"
          v-model="formData.areaName"
          required
          maxlength="50"
          :class="{ 'error': errors.areaName }"
        >
        <div class="error-message" v-if="errors.areaName">{{ errors.areaName }}</div>
      </div>

      <div class="form-group">
        <label for="contactor">负责人</label>
        <input
          type="text"
          id="contactor"
          v-model="formData.contactor"
          maxlength="50"
          :class="{ 'error': errors.contactor }"
        >
        <div class="error-message" v-if="errors.contactor">{{ errors.contactor }}</div>
      </div>

      <div class="form-group">
        <label for="phoneNumber">联系电话</label>
        <input
          type="tel"
          id="phoneNumber"
          v-model="formData.phoneNumber"
          maxlength="20"
          :class="{ 'error': errors.phoneNumber }"
        >
        <div class="error-message" v-if="errors.phoneNumber">{{ errors.phoneNumber }}</div>
      </div>

      <div class="form-group">
        <label for="email">邮箱</label>
        <input
          type="email"
          id="email"
          v-model="formData.email"
          maxlength="50"
          :class="{ 'error': errors.email }"
        >
        <div class="error-message" v-if="errors.email">{{ errors.email }}</div>
      </div>

      <div class="form-actions">
        <button type="submit" :disabled="submitting" class="btn-submit">
          {{ submitting ? '提交中...' : '提交' }}
        </button>
        <button type="button" @click="cancel" class="btn-cancel">取消</button>
        <button
          type="button"
          class="btn-delete"
          @click="deletearea"
          :disabled="deleting"
        >
          {{ deleting ? '删除中...' : '删除' }}
        </button>
      </div>
    </form>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue';
import { useUserStore } from '@/user/user';
import { useRouter } from 'vue-router';
import axios from 'axios';
import confirm from '@/utils/confirm';
import alert from '@/utils/alert';

const userStore = useUserStore();
const router = useRouter();
const props = defineProps({
  // 接收来自列表的整个区域对象
  area: {
    type: Object,
    required: true
  }
});

// 将传入的 area 对象映射到本地 formData
const formData = reactive({
  areaId: null,
  areaName: '',
  contactor: '',
  phoneNumber: '',
  email: ''
});

const errors = reactive({});
const loading = ref(true);
const submitting = ref(false);
const error = ref('');

// 检查登录状态
const checkAuth = () => {
  if (!userStore.token) {
    router.push('/login');
    return false;
  }
  return true;
};

// 使用传入的 area 对象回显，不再发起额外的 GET 请求
const populateFromProp = (collab) => {
  if (!collab) return;
  formData.areaId = collab.area_ID || null;
  formData.areaName = collab.area_NAME || '';
  formData.contactor = collab.CONTACTOR || '';
  formData.phoneNumber = collab.PHONE_NUMBER || '';
  formData.email = collab.EMAIL || '';
  loading.value = false;
};

// 初始用传入的 prop 填充
populateFromProp(props.area);

const validateForm = () => {
  let isValid = true;

  // 重置错误信息
  Object.keys(errors).forEach(key => delete errors[key]);

  // 验证区域名称
  if (!formData.areaName.trim()) {
    errors.areaName = '区域名称是必填项';
    isValid = false;
  } else if (formData.areaName.length > 50) {
    errors.areaName = '名称长度不能超过50个字符';
    isValid = false;
  }

  // 验证负责人
  if (formData.contactor && formData.contactor.length > 50) {
    errors.contactor = '联系人姓名长度不能超过50个字符';
    isValid = false;
  }

  // 验证电话号码
  if (formData.phoneNumber) {
    const phoneRegex = /^1[3-9]\d{9}$/; // 简单的手机号验证
    if (!phoneRegex.test(formData.phoneNumber)) {
      errors.phoneNumber = '无效的电话号码格式';
      isValid = false;
    }
  }

  // 验证邮箱
  if (formData.email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(formData.email)) {
      errors.email = '无效的电子邮件格式';
      isValid = false;
    }
  }

  return isValid;
};

const submitForm = async () => {
  if (!checkAuth()) return;
  if (!validateForm()) return;

  submitting.value = true;

  try {
    const body = {
      areaName: formData.areaName,
      Contactor: formData.contactor,
      PhoneNumber: formData.phoneNumber,
      Email: formData.email
    };

    // operatorAccountId 通过查询参数传递（userStore.token 即为操作账号 ID）
    const operator = encodeURIComponent(userStore.token);
    const url = `/api/area/${formData.areaId}?operatorAccountId=${operator}`;

  await axios.put(url, body);

  await alert('更新成功！');
    emit('saved');
  } catch (error) {
    if (error.response) {
      if (error.response.status === 401) {
        await alert('登录已过期，请重新登录');
        userStore.logout();
        router.push('/login');
      } else if (error.response.status === 400) {
        await alert(error.response.data || '更新失败，请检查输入数据');
      } else if (error.response.status === 404) {
        await alert('区域不存在');
      } else {
        await alert('更新失败，' + (error || '，请稍后重试'));
      }
    } else {
      await alert('更新失败，请检查网络连接');
    }
    console.error('更新区域错误:', error);
  } finally {
    submitting.value = false;
  }
};

const cancel = () => {
  emit('cancel');
};

const deleting = ref(false);

const deletearea = async () => {
  if (!checkAuth()) return;
    if (!formData.areaId) {
    await alert('无效的区域ID');
    return;
  }

  const ok = await confirm('确定要删除该区域吗？此操作不可恢复。');
  if (!ok) return;

  deleting.value = true;
  try {
    const operator = encodeURIComponent(userStore.token);
    const url = `/api/area/${formData.areaId}?operatorAccountId=${operator}`;

  await axios.delete(url);

  await alert('删除成功！');
  emit('deleted');
  } catch (error) {
    if (error.response) {
      if (error.response.status === 401) {
        await alert('登录已过期，请重新登录');
        userStore.logout();
        router.push('/login');
      } else if (error.response.status === 400) {
        await alert(error.response.data || '删除失败，请检查请求');
      } else if (error.response.status === 404) {
        await alert('区域不存在');
      } else {
        await alert('删除失败，请稍后重试');
      }
    } else {
      await alert('删除失败，请检查网络连接');
    }
    console.error('删除区域错误:', error);
  } finally {
    deleting.value = false;
  }
};

const emit = defineEmits(['saved', 'cancel', 'deleted']);
</script>

<style scoped>
.edit-area {
  max-width: 600px;
  margin: 0 auto;
}

.area-form {
  margin-top: 20px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: bold;
}

.form-group input {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  box-sizing: border-box;
}

.readonly-field {
  padding: 10px;
  background-color: #f8f9fa;
  border-radius: 4px;
  border: 1px solid #ddd;
}

.form-group input.error {
  border-color: #dc3545;
}

.error-message {
  color: #dc3545;
  font-size: 14px;
  margin-top: 5px;
}

.required {
  color: #dc3545;
}

.form-actions {
  display: flex;
  gap: 10px;
  margin-top: 30px;
}

.btn-submit, .btn-cancel , .btn-delete {
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.btn-submit {
  background-color: #28a745;
  color: white;
}

.btn-submit:disabled {
  background-color: #6c757d;
  cursor: not-allowed;
}

.btn-cancel {
  background-color: #6c757d;
  color: white;
}

.btn-delete {
  background-color: #dc3545;
  color: white;
}

.loading, .error {
  text-align: center;
  padding: 40px;
  font-size: 18px;
}

.error {
  color: #dc3545;
}
</style>
