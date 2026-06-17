import { createUserManager } from './auth';
import { env } from '@/environment';

async function load() {
  const manager = createUserManager();
  try {
    const user = await manager.signinRedirectCallback();
    localStorage.setItem('login-event', Date.now().toString());
    if ('BroadcastChannel' in window) {
      const channel = new BroadcastChannel('auth');
      channel.postMessage('login');
      channel.close();
    }
    const redirect =
      user.state && typeof user.state === 'string'
        ? user.state
        : new URL(import.meta.env.BASE_URL, document.location.origin).href;
    window.location.replace(redirect);
  } catch (error) {
    console.error('OIDC callback error:', error);
    window.location.replace(`${env.AUTH_PAGE_URL}/error`);
  }
}

load();
export {};
