
<template>
    <div v-if="show" class="add-staff-modal-mask">
        <div class="add-staff-modal">
            <div class="add-staff-modal-header">
                <span>添加新员工</span>
                <button class="add-staff-close-btn" @click="$emit('close')">×</button>
            </div>
            <div class="add-staff-modal-body">
                <form @submit.prevent="onSubmit">
                    <div class="add-staff-form-row">
                        <label>姓名：</label>
                        <input v-model="form.STAFF_NAME" maxlength="50" required />
                    </div>
                    <div class="add-staff-form-row">
                        <label>性别：</label>
                        <input v-model="form.STAFF_SEX" maxlength="10" />
                    </div>
                    <div class="add-staff-form-row">
                        <label>部门：</label>
                        <input v-model="form.STAFF_APARTMENT" maxlength="50" required />
                    </div>
                    <div class="add-staff-form-row">
                        <label>职位：</label>
                        <input v-model="form.STAFF_POSITION" maxlength="50" required />
                    </div>
                    <div class="add-staff-form-row">
                        <label>底薪：</label>
                        <input v-model.number="form.STAFF_SALARY" type="number" min="0" step="0.01" required />
                    </div>
                    <div class="add-staff-modal-footer">
                        <button type="submit" class="add-staff-confirm-btn">确认</button>
                        <button type="button" class="add-staff-cancel-btn" @click="$emit('close')">取消</button>
                    </div>
                </form>
                <div v-if="errorMsg" class="add-staff-error">{{ errorMsg }}</div>
            </div>
        </div>
    </div>
</template>


<script setup>
import { ref, watch } from 'vue';
import axios from 'axios';
const props = defineProps({
    show: Boolean,
    operatorAccount: String
});
const emit = defineEmits(['close']);

const form = ref({
    STAFF_NAME: '',
    STAFF_SEX: '',
    STAFF_APARTMENT: '',
    STAFF_POSITION: '',
    STAFF_SALARY: ''
});
const errorMsg = ref('');

watch(() => props.show, (val) => {
    if (val) {
        form.value = {
            STAFF_NAME: '',
            STAFF_SEX: '',
            STAFF_APARTMENT: '',
            STAFF_POSITION: '',
            STAFF_SALARY: ''
        };
        errorMsg.value = '';
    }
});

async function onSubmit() {
    errorMsg.value = '';
    try {
        // 使用父组件传递的操作员账号
        const res = await axios.post('/api/Staff/AddStaff', form.value, {
            params: { operatorAccount: props.operatorAccount }
        });
        if (res.status === 200) {
            emit('close');
            window.location.reload(); // 可根据实际情况刷新或更新员工列表
        } else {
            errorMsg.value = res.data || '添加失败';
        }
    } catch (e) {
        errorMsg.value = e?.response?.data || '添加失败';
    }
}
</script>


<style scoped>
.add-staff-modal-mask {
    position: fixed;
    left: 0; top: 0; right: 0; bottom: 0;
    background: rgba(0,0,0,0.18);
    z-index: 1200;
    display: flex;
    align-items: center;
    justify-content: center;
}
.add-staff-modal {
    background: #fff;
    border-radius: 16px;
    min-width: 380px;
    max-width: 420px;
    box-shadow: 0 4px 24px #0002;
    padding: 0 0 18px 0;
    animation: modalIn 0.22s;
}
@keyframes modalIn {
    from { transform: scale(0.8); opacity: 0; }
    to { transform: scale(1); opacity: 1; }
}
.add-staff-modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 20px;
    font-weight: bold;
    padding: 18px 24px 8px 24px;
}
.add-staff-close-btn {
    background: none;
    border: none;
    font-size: 26px;
    color: #888;
    cursor: pointer;
    transition: color 0.18s, transform 0.18s;
}
.add-staff-close-btn:hover {
    color: #f55;
    transform: scale(1.18);
}
.add-staff-modal-body {
    padding: 0 24px;
}
.add-staff-form-row {
    display: flex;
    align-items: center;
    margin: 14px 0;
}
.add-staff-form-row label {
    width: 80px;
    font-size: 15px;
    color: #555;
}
.add-staff-form-row input {
    flex: 1;
    padding: 7px 12px;
    font-size: 15px;
    border: 1px solid #b6e0fa;
    border-radius: 6px;
    outline: none;
    background: #f8fbfd;
}
.add-staff-form-row input:focus {
    border-color: #9cd1f6;
}
.add-staff-modal-footer {
    display: flex;
    justify-content: flex-end;
    gap: 18px;
    margin-top: 18px;
}
.add-staff-confirm-btn {
    padding: 7px 22px;
    background: #499ffb;
    color: #fff;
    border: none;
    border-radius: 8px;
    font-size: 15px;
    font-weight: bold;
    cursor: pointer;
    transition: background 0.18s, transform 0.18s;
}
.add-staff-confirm-btn:hover {
    background: #67b6ff;
    transform: scale(1.08);
}
.add-staff-cancel-btn {
    padding: 7px 22px;
    background: #eee;
    color: #555;
    border: none;
    border-radius: 8px;
    font-size: 15px;
    font-weight: bold;
    cursor: pointer;
    transition: background 0.18s, color 0.18s, transform 0.18s;
}
.add-staff-cancel-btn:hover {
    background: #f5f5f5;
    color: #f55;
    transform: scale(1.08);
}
.add-staff-error {
    color: #f55;
    font-size: 14px;
    margin-top: 10px;
    text-align: center;
}
</style>