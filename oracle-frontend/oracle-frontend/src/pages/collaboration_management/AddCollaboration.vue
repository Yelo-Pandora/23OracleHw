<template>
  <div class="add-collaboration">
    <h2>添加新合作方</h2>
    <form @submit.prevent="submitForm" class="collaboration-form">
      <div class="form-group">
        <label for="collaborationId">合作方ID <span class="required">*</span></label>
        <input
          type="number"
          id="collaborationId"
          v-model="formData.collaborationId"
          required
          min="1"
          :class="{ 'error': errors.collaborationId }"
        >
        <div class="error-message" v-if="errors.collaborationId">{{ errors.collaborationId }}</div>
      </div>

      <div class="form-group">
        <label for="collaborationName">合作方名称 <span class="required">*</span></label>
        <input
          type="text"
          id="collaborationName"
          v-model="formData.collaborationName"
          required
          maxlength="50"
          :class="{ 'error': errors.collaborationName }"
        >
        <div class="error-message" v-if="errors.collaborationName">{{ errors.collaborationName }}</div>
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

// 使用当前时间戳（秒）自动生成默认的合作方 ID
const formData = reactive({
  collaborationId: Math.floor(Date.now()/1000), // 秒级时间戳
  collaborationName: '',
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

  // 验证合作方ID
  if (!formData.collaborationId || formData.collaborationId <= 0) {
    errors.collaborationId = '合作方ID必须大于0';
    isValid = false;
  }

  // 验证合作方名称
  if (!formData.collaborationName.trim()) {
    errors.collaborationName = '合作方名称是必填项';
    isValid = false;
  } else if (formData.collaborationName.length > 50) {
    errors.collaborationName = '名称长度不能超过50个字符';
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
      CollaborationId: formData.collaborationId,
      CollaborationName: formData.collaborationName,
      Contactor: formData.contactor,
      PhoneNumber: formData.phoneNumber,
      Email: formData.email
    };

    // operatorAccountId 通过查询参数传递（userStore.token 即为操作账号 ID）
    const operator = encodeURIComponent(userStore.token);
    const url = `/api/Collaboration?operatorAccountId=${operator}`;

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
      console.error('添加合作方错误:', error);
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
.add-collaboration {
  max-width: 600px;
  margin: 0 auto;
}

.collaboration-form {
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
