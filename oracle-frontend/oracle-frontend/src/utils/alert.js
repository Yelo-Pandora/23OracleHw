import { createApp, h } from 'vue';
import AlertModal from '@/components/AlertModal.vue';

export default function alert(message) {
  return new Promise((resolve) => {
    const container = document.createElement('div');
    document.body.appendChild(container);

    const app = createApp({
      render() {
        return h(AlertModal, { message, onClose: this.handleClose });
      },
      methods: {
        handleClose() {
          resolve();
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
