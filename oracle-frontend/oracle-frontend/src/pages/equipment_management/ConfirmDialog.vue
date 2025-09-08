<template>
  <div class="modal-overlay">
    <div class="modal-content confirm-dialog">
      <div class="dialog-header">
        <h2>{{ title }}</h2>
      </div>

      <div class="dialog-body">
        <p>{{ message }}</p>
      </div>

      <div class="dialog-actions">
        <button class="btn btn-secondary" @click="$emit('cancel')">
          取消
        </button>
        <button class="btn btn-danger" @click="$emit('confirm')">
          确认
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  title: {
    type: String,
    default: '确认操作'
  },
  message: {
    type: String,
    required: true
  }
})

defineEmits(['confirm', 'cancel'])
</script>

<style scoped>
  .modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(0, 0, 0, 0.5);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 1000;
  }

  .confirm-dialog {
    background: white;
    border-radius: 8px;
    width: 400px;
    max-width: 90%;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  }

  .dialog-header {
    padding: 20px 20px 0;
  }

    .dialog-header h2 {
      margin: 0;
      font-size: 1.25rem;
      color: #333;
    }

  .dialog-body {
    padding: 20px;
  }

  .dialog-body p {
    margin: 0;
    color: #666;
    line-height: 1.5;
  }

  .dialog-actions {
    display: flex;
    justify-content: flex-end;
    gap: 10px;
    padding: 0 20px 20px;
  }

  .btn {
    padding: 8px 16px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-weight: 500;
  }

  .btn-secondary {
    background-color: #6c757d;
    color: white;
  }

    .btn-secondary:hover {
      background-color: #5a6268;
    }

  .btn-danger {
    background-color: #dc3545;
    color: white;
  }

    .btn-danger:hover {
      background-color: #c82333;
    }
</style>
