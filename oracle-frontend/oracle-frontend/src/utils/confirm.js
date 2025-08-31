import { createApp, h } from 'vue';
import ConfirmModal from '@/components/ConfirmModal.vue';

export default function confirm(message) {
  return new Promise((resolve) => {
    const container = document.createElement('div');
    document.body.appendChild(container);

    const app = createApp({
      render() {
        return h(ConfirmModal, { message, onClose: this.handleClose });
      },
      methods: {
        handleClose(result) {
          resolve(result);
          setTimeout(() => {
            app.unmount();
            if (container.parentNode) container.parentNode.removeChild(container);
          }, 0);
        }
      }
    });

    app.mount(container);
  });
}
