<template>
    <div v-if="show" class="edit-modal-mask">
        <div class="edit-modal">
            <div class="edit-modal-header">
                <span>编辑员工信息</span>
                <button class="edit-close-btn" @click="$emit('close')">×</button>
            </div>
            <div class="edit-modal-body">
                <form @submit.prevent="onSubmit">
                    <div class="edit-form-row">
                        <label>员工账号：</label>
                        <span class="edit-disabled">{{ employeeInfo?.account }}</span>
                    </div>
                    <div class="edit-form-row">
                        <label>员工昵称：</label>
                        <span class="edit-disabled">{{ employeeInfo?.username }}</span>
                    </div>
                    <div class="edit-form-row">
                        <label>员工ID：</label>
                        <span class="edit-disabled">{{ employeeInfo?.id }}</span>
                    </div>
                    <div class="edit-form-row">
                        <label>姓名：</label>
                        <input v-if="canEdit('STAFF_NAME')" v-model="form.STAFF_NAME" :placeholder="employeeInfo?.name" />
                        <span v-else class="edit-disabled">{{ employeeInfo?.name }}</span>
                    </div>
                    <div class="edit-form-row">
                        <label>性别：</label>
                        <input v-if="canEdit('STAFF_SEX')" v-model="form.STAFF_SEX" :placeholder="employeeInfo?.sex" />
                        <span v-else class="edit-disabled">{{ employeeInfo?.sex }}</span>
                    </div>
                    <div class="edit-form-row">
                        <label>所属部门：</label>
                        <input v-if="canEdit('STAFF_APARTMENT')" v-model="form.STAFF_APARTMENT" :placeholder="employeeInfo?.department" />
                        <span v-else class="edit-disabled">{{ employeeInfo?.department }}</span>
                    </div>
                    <div class="edit-form-row">
                        <label>职位：</label>
                        <input v-if="canEdit('STAFF_POSITION')" v-model="form.STAFF_POSITION" :placeholder="employeeInfo?.position" />
                        <span v-else class="edit-disabled">{{ employeeInfo?.position }}</span>
                    </div>
                    <div class="edit-form-row">
                        <label>员工权限：</label>
                        <input v-if="canEdit('AUTHORITY')" v-model.number="form.AUTHORITY" type="number" min="1" max="5" :placeholder="employeeInfo?.authority" />
                        <span v-else class="edit-disabled">{{ employeeInfo?.authority }}</span>
                    </div>
                    <div class="edit-form-row">
                        <label>底薪：</label>
                        <span class="edit-disabled">{{ employeeInfo?.salary }}</span>
                    </div>
                    <div class="edit-modal-footer">
                        <button type="submit" class="edit-confirm-btn" :disabled="!canSubmit">确认</button>
                        <button type="button" class="edit-cancel-btn" @click="$emit('close')">取消</button>
                    </div>
                </form>
                <div v-if="errorMsg" class="edit-error">{{ errorMsg }}</div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import axios from 'axios';
const props = defineProps({
    show: Boolean,
    employeeInfo: Object,
    operatorAccount: String,
    operatorAuthority: [String, Number]
});
const emit = defineEmits(['close']);

const form = ref({
    STAFF_NAME: '',
    STAFF_SEX: '',
    STAFF_APARTMENT: '',
    STAFF_POSITION: '',
    AUTHORITY: ''
});
const errorMsg = ref('');

watch(() => props.show, (val) => {
    if (val && props.employeeInfo) {
        form.value = {
            STAFF_NAME: props.employeeInfo.name || '',
            STAFF_SEX: props.employeeInfo.sex || '',
            STAFF_APARTMENT: props.employeeInfo.department || '',
            STAFF_POSITION: props.employeeInfo.position || '',
            AUTHORITY: props.employeeInfo.authority || ''
        };
        errorMsg.value = '';
    }
});

// 权限和可编辑项判断
function canEdit(field) {
    if (!props.employeeInfo) return false;
    if (["account","username","id","salary"].includes(field)) return false;
    const myAuth = Number(props.operatorAuthority);
    const targetAuth = Number(props.employeeInfo.authority);
    if (field === 'AUTHORITY') {
        // 只有目标权限比自己低，且不能修改为高于自己权限(数字越小,权限越大)
        if (targetAuth > myAuth && props.employeeInfo.id !== props.operatorAccount) {
            return true;
        }
        return false;
    }
    if (field === 'salary') return false;
    return true;
}

const canSubmit = computed(() => {
    // 只要有可编辑项即可提交
    return Object.keys(form.value).some(k => canEdit(k));
});

async function onSubmit() {
    errorMsg.value = '';
    try {
        const operatorAccount = props.operatorAccount;
        const staffId = props.employeeInfo.id;
        const staffDto = {
            STAFF_NAME: form.value.STAFF_NAME,
            STAFF_SEX: form.value.STAFF_SEX,
            STAFF_APARTMENT: form.value.STAFF_APARTMENT,
            STAFF_POSITION: form.value.STAFF_POSITION,
            STAFF_SALARY: props.employeeInfo.salary
        };
        await axios.patch('/api/Staff/ModifyStaffInfo', staffDto, {
            params: { staffId, operatorAccount }
        });
        // 权限变更且不能超过自己权限
        if (canEdit('AUTHORITY') && form.value.AUTHORITY !== props.employeeInfo.authority) {
            let newAuth = Number(form.value.AUTHORITY);
            const myAuth = Number(props.operatorAuthority);
            if (newAuth < myAuth) newAuth = myAuth; // 不能分配高于自己权限
            await axios.patch('/api/Staff/ModifyStaffAuthority', null, {
                params: {
                    operatorAccount,
                    staffId,
                    newAuthority: newAuth
                }
            });
        }
        emit('close');
        window.location.reload();
    } catch (e) {
        errorMsg.value = e?.response?.data || '修改失败';
    }
}
</script>


<style scoped>
.edit-modal-mask {
    position: fixed;
    left: 0; top: 0; right: 0; bottom: 0;
    background: rgba(0,0,0,0.18);
    z-index: 1200;
    display: flex;
    align-items: center;
    justify-content: center;
}
.edit-modal {
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
.edit-modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 20px;
    font-weight: bold;
    padding: 18px 24px 8px 24px;
}
.edit-close-btn {
    background: none;
    border: none;
    font-size: 26px;
    color: #888;
    cursor: pointer;
    transition: color 0.18s, transform 0.18s;
}
.edit-close-btn:hover {
    color: #f55;
    transform: scale(1.18);
}
.edit-modal-body {
    padding: 0 24px;
}
.edit-form-row {
    display: flex;
    align-items: center;
    margin: 14px 0;
}
.edit-form-row label {
    width: 100px;
    font-size: 15px;
    color: #555;
}
.edit-form-row input {
    flex: 1;
    padding: 7px 12px;
    font-size: 15px;
    border: 1px solid #b6e0fa;
    border-radius: 6px;
    outline: none;
    background: #f8fbfd;
}
.edit-form-row input:focus {
    border-color: #9cd1f6;
}
.edit-disabled {
    flex: 1;
    color: #aaa;
    background: #f5f5f5;
    padding: 7px 12px;
    border-radius: 6px;
    font-size: 15px;
}
.edit-modal-footer {
    display: flex;
    justify-content: flex-end;
    gap: 18px;
    margin-top: 18px;
}
.edit-confirm-btn {
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
.edit-confirm-btn:hover {
    background: #67b6ff;
    transform: scale(1.08);
}
.edit-cancel-btn {
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
.edit-cancel-btn:hover {
    background: #f5f5f5;
    color: #f55;
    transform: scale(1.08);
}
.edit-error {
    color: #f55;
    font-size: 14px;
    margin-top: 10px;
    text-align: center;
}
</style>
