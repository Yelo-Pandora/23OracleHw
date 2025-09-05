<template>
  <div class="add-area">
    <h2>添加新区域</h2>

    <!-- 无权限提示 -->
    <div v-if="!hasAddAccess" class="no-access">
      <h3>无权访问</h3>
      <p>您没有添加区域的权限。如需操作请联系管理员。</p>
      <div class="no-access-actions">
        <button class="btn-cancel" @click="cancel">返回</button>
      </div>
    </div>

    <!-- 有权限才显示表单 -->
    <form v-else @submit.prevent="submitForm" class="area-form">
        <div class="form-group">
          <label for="areaId">区域ID <span class="required">*</span></label>
          <input type="number" id="areaId" v-model="formData.areaId" required min="1" :class="{ 'error': errors.areaId }">
          <div class="error-message" v-if="errors.areaId">{{ errors.areaId }}</div>
        </div>

        <div class="form-group">
          <label for="category">区域类别 <span class="required">*</span></label>
          <select id="category" v-model="formData.category" :class="{ 'error': errors.category }">
            <option value="">请选择</option>
            <option value="RETAIL">商铺</option>
            <option value="PARKING">停车</option>
            <option value="EVENT">活动</option>
            <option value="OTHER">其他</option>
          </select>
          <div class="error-message" v-if="errors.category">{{ errors.category }}</div>
        </div>

        <div class="form-group">
          <label for="isEmpty">是否空置</label>
          <select id="isEmpty" v-model.number="formData.isEmpty">
            <option :value="1">是</option>
            <option :value="0">否</option>
          </select>
        </div>

        <div class="form-group">
          <label for="areaSize">区域面积 (平米)</label>
          <input type="number" id="areaSize" v-model.number="formData.areaSize" min="0" step="0.01" :class="{ 'error': errors.areaSize }">
          <div class="error-message" v-if="errors.areaSize">{{ errors.areaSize }}</div>
        </div>

        <!-- RETAIL fields -->
        <div v-if="formData.category === 'RETAIL'">
          <div class="form-group">
            <label for="rentStatus">租赁状态</label>
            <input type="text" id="rentStatus" v-model="formData.rentStatus" maxlength="50">
          </div>
          <div class="form-group">
            <label for="baseRent">基础租金</label>
            <input type="number" id="baseRent" v-model.number="formData.baseRent" min="0" step="0.01" :class="{ 'error': errors.baseRent }">
            <div class="error-message" v-if="errors.baseRent">{{ errors.baseRent }}</div>
          </div>
        </div>

        <!-- EVENT fields -->
        <div v-if="formData.category === 'EVENT'">
          <div class="form-group">
            <label for="capacity">容量</label>
            <input type="number" id="capacity" v-model.number="formData.capacity" min="0" :class="{ 'error': errors.capacity }">
            <div class="error-message" v-if="errors.capacity">{{ errors.capacity }}</div>
          </div>
          <div class="form-group">
            <label for="areaFee">场地费</label>
            <input type="number" id="areaFee" v-model.number="formData.areaFee" min="0" step="0.01" :class="{ 'error': errors.areaFee }">
            <div class="error-message" v-if="errors.areaFee">{{ errors.areaFee }}</div>
          </div>
        </div>

        <!-- PARKING fields -->
        <div v-if="formData.category === 'PARKING'">
          <div class="form-group">
            <label for="parkingFee">停车费</label>
            <input type="number" id="parkingFee" v-model.number="formData.parkingFee" min="0" step="0.01" :class="{ 'error': errors.parkingFee }">
            <div class="error-message" v-if="errors.parkingFee">{{ errors.parkingFee }}</div>
          </div>
        </div>

        <!-- OTHER fields -->
        <div v-if="formData.category === 'OTHER'">
          <div class="form-group">
            <label for="type">类型(其他)</label>
            <input type="text" id="type" v-model="formData.type" maxlength="50">
          </div>
        </div>

      <div class="form-actions">
        <button type="submit" :disabled="submitting" class="btn-submit">{{ submitting ? '提交中...' : '提交' }}</button>
        <button type="button" @click="cancel" class="btn-cancel">取消</button>
      </div>
    </form>
  </div>
</template>

<script setup>
import { reactive, ref, computed } from 'vue';
import { useUserStore } from '@/user/user';
import { useRouter } from 'vue-router';
import axios from 'axios';
import alert from '@/utils/alert';

const emit = defineEmits(['saved', 'cancel']);
const userStore = useUserStore();
const router = useRouter();

// 只有 authority 为 1 或 2 的用户可以添加区域
const hasAddAccess = computed(() => {
  const auth = Number(userStore.userInfo?.authority);
  return auth === 1 || auth === 2;
});

// 使用当前时间戳（秒）自动生成默认的区域 ID
const formData = reactive({
  areaId: Math.floor(Date.now()/1000), // 秒级时间戳
  isEmpty: 1, // 0 否，1 是
  areaSize: null,
  category: '', // RETAIL, PARKING, EVENT, OTHER
  // category specific
  rentStatus: '',
  baseRent: null,
  capacity: null,
  areaFee: null,
  parkingFee: null,
  type: ''
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
  Object.keys(errors).forEach(k => delete errors[k]);

  if (!formData.areaId || formData.areaId <= 0) {
    errors.areaId = '区域ID必须大于0';
    isValid = false;
  }

  if (!formData.category) {
    errors.category = '请选择区域类别';
    isValid = false;
  }

  if (formData.areaSize != null && formData.areaSize < 0) {
    errors.areaSize = '面积不能为负数';
    isValid = false;
  }

  if (formData.category === 'RETAIL') {
    if (formData.baseRent != null && formData.baseRent < 0) {
      errors.baseRent = '基础租金不能为负数';
      isValid = false;
    }
  }

  if (formData.category === 'EVENT') {
    if (formData.capacity != null && formData.capacity < 0) {
      errors.capacity = '容量不能为负数';
      isValid = false;
    }
    if (formData.areaFee != null && formData.areaFee < 0) {
      errors.areaFee = '场地费不能为负数';
      isValid = false;
    }
  }

  if (formData.category === 'PARKING') {
    if (formData.parkingFee != null && formData.parkingFee < 0) {
      errors.parkingFee = '停车费不能为负数';
      isValid = false;
    }
  }

  return isValid;
};

const submitForm = async () => {
  if (!checkAuth()) return;
  if (!hasAddAccess.value) {
    await alert('您没有权限添加区域');
    return;
  }
  if (!validateForm()) return;

  submitting.value = true;
  try {
    const body = {
      AreaId: Number(formData.areaId),
      IsEmpty: Number(formData.isEmpty),
      AreaSize: formData.areaSize != null ? Number(formData.areaSize) : null,
      Category: formData.category,
      RentStatus: formData.category === 'RETAIL' ? (formData.rentStatus || null) : null,
      BaseRent: formData.category === 'RETAIL' ? (formData.baseRent != null ? Number(formData.baseRent) : null) : null,
      Capacity: formData.category === 'EVENT' ? (formData.capacity != null ? Number(formData.capacity) : null) : null,
      AreaFee: formData.category === 'EVENT' ? (formData.areaFee != null ? Number(formData.areaFee) : null) : null,
      Type: formData.category === 'OTHER' ? (formData.type || null) : null,
      ParkingFee: formData.category === 'PARKING' ? (formData.parkingFee != null ? Number(formData.parkingFee) : null) : null
    };

    const url = '/api/Areas';
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

.form-group select {
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
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
