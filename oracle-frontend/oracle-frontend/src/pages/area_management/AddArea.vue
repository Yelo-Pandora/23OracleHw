<template>
  <div class="add-area">
    <h2>添加新区域</h2>
    <form @submit.prevent="submitForm" class="area-form">
      <div class="form-group">
        <label for="areaId">区域ID <span class="required">*</span></label>
        <input
          type="number"
          id="areaId"
          v-model="formData.areaId"
          required
          min="1"
          :class="{ 'error': errors.areaId }"
        >
        <div class="error-message" v-if="errors.areaId">{{ errors.areaId }}</div>
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
      </div>
    </form>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue';
import { useUserStore } from '@/user/user';
import { useRouter } from 'vue-router';
import axios from 'axios';
import alert from '@/utils/alert';

const userStore = useUserStore();
const router = useRouter();

// 使用当前时间戳（秒）自动生成默认的区域 ID
const formData = reactive({
  areaId: Math.floor(Date.now()/1000), // 秒级时间戳
  areaName: '',
  contactor: '',
  phoneNumber: '',
  email: ''
});

const errors = reactive({});
const submitting = ref(false);

// 检查登录状态
const checkAuth = () => {
  if (!userStore.token) {
    router.push('/login');
    return false;
  }
  return true;
};

const validateForm = () => {
  let isValid = true;

  // 重置错误信息
  Object.keys(errors).forEach(key => delete errors[key]);

  // 验证区域ID
  if (!formData.areaId || formData.areaId <= 0) {
    errors.areaId = '区域ID必须大于0';
    isValid = false;
  }

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
      areaId: formData.areaId,
      areaName: formData.areaName,
      Contactor: formData.contactor,
      PhoneNumber: formData.phoneNumber,
      Email: formData.email
    };

    // operatorAccountId 通过查询参数传递（userStore.token 即为操作账号 ID）
    const operator = encodeURIComponent(userStore.token);
    const url = `/api/area?operatorAccountId=${operator}`;

    await axios.post(url, body);

    await alert('添加成功！');
    emit('saved');
  } catch (error) {
    if (error.response && error.response.status === 401) {
      await alert('登录已过期，请重新登录');
      userStore.logout();
      router.push('/login');
    } else if (error.response && error.response.status === 400) {
      await alert(error.response.data || '添加失败，请检查输入数据');
    } else {
      await alert('添加失败，' + (error || '，请稍后重试'));
      console.error('添加区域错误:', error);
    }
  } finally {
    submitting.value = false;
  }
};

const cancel = () => {
  emit('cancel');
};

const emit = defineEmits(['saved', 'cancel']);
</script>

<style scoped>
.add-area {
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

.btn-submit, .btn-cancel {
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
</style>
