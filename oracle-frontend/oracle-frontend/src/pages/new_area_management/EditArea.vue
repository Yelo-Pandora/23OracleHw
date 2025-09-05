<template>
  <div class="edit-area">
    <h2>编辑区域信息</h2>

    <!-- 权限检查：只有 authority 为 1 或 2 的用户可以编辑 -->
    <div v-if="!hasEditAccess" class="no-access">
      <h3>无权访问</h3>
      <p>您没有修改或删除区域的权限。如需操作请联系管理员。</p>
      <div class="no-access-actions">
        <button class="btn-cancel" @click="cancel">返回</button>
      </div>
    </div>

    <div v-else>
      <div v-if="loading" class="loading">加载中...</div>
      <div v-else-if="error" class="error">{{ error }}</div>
      <form v-else @submit.prevent="submitForm" class="area-form">
      <div class="form-group">
        <label>区域ID</label>
        <div class="readonly-field">{{ formData.areaId }}</div>
      </div>

      <div class="form-group">
        <label>区域类别</label>
        <div class="readonly-field">{{ formData.category }}</div>
      </div>

      <div class="form-group">
        <label for="isEmpty">是否空置</label>
        <select id="isEmpty" v-model.number="formData.isEmpty">
          <option :value="0">否</option>
          <option :value="1">是</option>
        </select>
      </div>

      <div class="form-group">
        <label for="areaSize">区域面积 (平方米)</label>
        <input type="number" id="areaSize" v-model.number="formData.areaSize" min="0" step="0.01">
      </div>

      <!-- RETAIL only -->
      <div v-if="formData.category === 'RETAIL'" class="form-group">
        <label for="baseRent">基础租金</label>
        <input type="number" id="baseRent" v-model.number="formData.baseRent" min="0" step="0.01">
      </div>

      <div v-if="formData.category === 'RETAIL'" class="form-group">
        <label for="rentStatus">租赁状态</label>
        <select id="rentStatus" v-model="formData.rentStatus">
          <option value="">未租赁</option>
          <option value="租赁中">租赁中</option>
          <option value="已租赁">已租赁</option>
        </select>
      </div>


      <!-- EVENT only -->
      <div v-if="formData.category === 'EVENT'" class="form-group">
        <label for="areaFee">场地费</label>
        <input type="number" id="areaFee" v-model.number="formData.areaFee" min="0" step="0.01">
      </div>

      <div v-if="formData.category === 'EVENT'" class="form-group">
        <label for="capacity">容量</label>
        <input type="number" id="capacity" v-model.number="formData.capacity" min="0" step="1">
      </div>

      <!-- PARKING only -->
      <div v-if="formData.category === 'PARKING'" class="form-group">
        <label for="parkingFee">停车费</label>
        <input type="number" id="parkingFee" v-model.number="formData.parkingFee" min="0" step="0.01">
      </div>

      <!-- OTHER only -->
      <div v-if="formData.category === 'OTHER'" class="form-group">
        <label for="type">类型(其他)</label>
        <input type="text" id="type" v-model="formData.type">
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
  </div>
</template>

<script setup>
import { reactive, ref, computed, watch } from 'vue';
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

// 将传入的 area 对象映射到本地 formData，字段与后端 AreasController 返回的字段保持一致
const formData = reactive({
  areaId: null,
  category: '',
  isEmpty: 0,
  areaSize: null,
  baseRent: null,
  rentStatus: '',
  areaFee: null,
  capacity: null,
  parkingFee: null,
  type: ''
});

// CATEGORY is read-only in this editor (display-only like areaId)

const errors = reactive({});
const loading = ref(true);
const submitting = ref(false);
const error = ref('');

// 只有 authority 为 1 或 2 的用户可以编辑或删除
const hasEditAccess = computed(() => {
  const auth = Number(userStore.userInfo?.authority);
  return auth === 1 || auth === 2;
});

// 检查登录状态
const checkAuth = () => {
  if (!userStore.token) {
    router.push('/login');
    return false;
  }
  return true;
};

// 使用传入的 area 对象回显，不再发起额外的 GET 请求
const populateFromProp = (a) => {
  if (!a) return;
  formData.areaId = a.AREA_ID ?? null;
  formData.category = a.CATEGORY ?? '';
  formData.isEmpty = (typeof a.ISEMPTY === 'number') ? a.ISEMPTY : 0;
  formData.areaSize = a.AREA_SIZE ?? null;
  formData.baseRent = a.BaseRent ?? null;
  formData.rentStatus = a.RentStatus ?? '';
  formData.areaFee = a.AreaFee ?? null;
  formData.capacity = a.Capacity ?? null;
  formData.parkingFee = a.ParkingFee ?? null;
  formData.type = a.Type ?? '';
  loading.value = false;
  // CATEGORY is display-only; no select sync needed
};

// 初始用传入的 prop 填充
populateFromProp(props.area);

// 当 category 变化时，清空不相关字段（只能编辑与当前类型相关的字段）
watch(() => formData.category, (newCat, oldCat) => {
  // allowed fields per category
  const keep = new Set();
  keep.add('areaId');
  keep.add('category');
  keep.add('isEmpty');
  keep.add('areaSize');

  if (newCat === 'RETAIL') {
    keep.add('baseRent');
    keep.add('rentStatus');
  } else if (newCat === 'EVENT') {
    keep.add('areaFee');
    keep.add('capacity');
  } else if (newCat === 'PARKING') {
    keep.add('parkingFee');
  } else if (newCat === 'OTHER') {
    keep.add('type');
  }

  // fields to potentially clear
  const maybeClear = ['baseRent', 'rentStatus', 'areaFee', 'capacity', 'parkingFee', 'type'];
  for (const f of maybeClear) {
    if (!keep.has(f)) {
      // clear to appropriate empty value
      if (f === 'rentStatus' || f === 'type') formData[f] = '';
      else formData[f] = null;
    }
  }
});

// watch the select-bound category and confirm before applying change
// CATEGORY cannot be changed here, so no watch/confirmation logic is required

const validateForm = () => {
  let isValid = true;

  // 重置错误信息
  Object.keys(errors).forEach(key => delete errors[key]);

  if (!formData.category) {
    errors.category = '区域类别是必填项';
    isValid = false;
  }

  if (formData.areaSize != null && formData.areaSize < 0) {
    errors.areaSize = '面积不能为负数';
    isValid = false;
  }

  // only validate fields relevant to current category
  if (formData.category === 'RETAIL') {
    if (formData.baseRent != null && formData.baseRent < 0) {
      errors.baseRent = '基础租金不能为负数';
      isValid = false;
    }
  }

  if (formData.category === 'EVENT') {
    if (formData.areaFee != null && formData.areaFee < 0) {
      errors.areaFee = '场地费不能为负数';
      isValid = false;
    }
    if (formData.capacity != null && formData.capacity < 0) {
      errors.capacity = '容量不能为负数';
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
  if (!hasEditAccess.value) {
    await alert('您没有权限修改该区域');
    return;
  }
  if (!validateForm()) return;

  submitting.value = true;

  try {
    // AreasController 的 DTO 字段
    const body = {
      AreaId: formData.areaId,
      IsEmpty: formData.isEmpty,
      AreaSize: formData.areaSize,
  Category: formData.category,
  // only include fields which are relevant to the current category; others are sent as null/empty by our watcher
  RentStatus: formData.rentStatus,
  BaseRent: formData.baseRent,
  Capacity: formData.capacity,
  AreaFee: formData.areaFee,
  Type: formData.type,
  ParkingFee: formData.parkingFee
    };

    const operator = encodeURIComponent(userStore.token || '');
    // 后端无接口
    const url = `/api/Areas/${formData.areaId}?operatorAccountId=${operator}`;

    await axios.put(url, body);

    await alert('更新成功！');
    emit('saved');
  } catch (err) {
    if (err.response) {
      if (err.response.status === 401) {
        await alert('登录已过期，请重新登录');
        userStore.logout();
        router.push('/login');
      } else if (err.response.status === 400) {
        await alert(err.response.data || '更新失败，请检查输入数据');
      } else if (err.response.status === 404) {
        await alert('区域不存在');
      } else if (err.response.status === 405) {
        await alert('该区域无法更新，可能存在关联的租赁或其他记录');
      } else {
        await alert('更新失败，' + (err.message || '请稍后重试'));
      }
    } else {
      await alert('更新失败，请检查网络连接');
    }
    console.error('更新区域错误:', err);
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
  if (!hasEditAccess.value) {
    await alert('您没有权限删除该区域');
    return;
  }
  if (!formData.areaId) {
    await alert('无效的区域ID');
    return;
  }

  const ok = await confirm('确定要删除该区域吗？此操作不可恢复。');
  if (!ok) return;

  deleting.value = true;
  try {
    const operator = encodeURIComponent(userStore.token || '');
    const url = `/api/Areas/${formData.areaId}?operatorAccountId=${operator}`;

    await axios.delete(url);

    await alert('删除成功！');
    emit('deleted');
  } catch (err) {
    if (err.response) {
      if (err.response.status === 401) {
        await alert('登录已过期，请重新登录');
        userStore.logout();
        router.push('/login');
      } else if (err.response.status === 400) {
        await alert(err.response.data || '删除失败，请检查请求');
      } else if (err.response.status === 404) {
        await alert('区域不存在');
      } else if (err.response.status === 405) {
        await alert('该区域无法删除，可能存在关联的租赁或其他记录');
      } else {
        await alert('删除失败，请稍后重试');
      }
    } else {
      await alert('删除失败，请检查网络连接');
    }
    console.error('删除区域错误:', err);
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

.form-group select {
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
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

.no-access {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
  text-align: center;
}
.no-access h3 {
  margin-bottom: 8px;
}
.no-access p {
  color: #666;
  margin-bottom: 12px;
}
.no-access-actions .btn-cancel {
  padding: 8px 14px;
}
</style>
