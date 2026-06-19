import { createUserManager } from './auth';
import { env } from '@/environment';
import { clearLoggedOut } from '@/oidc/cross-domain-logout';

async function load(): Promise<void> {
  const manager = createUserManager();
  try {
    const user = await manager.signinRedirectCallback();
    // 明確登入成功：清除跨子網域登出標記，避免殘留標記誤殺這次新 session。
    clearLoggedOut();
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
    window.location.replace(`${env.AUTH_PAGE_URL.replace(/\/$/, '')}/error`);
  }
}

load();
export {};
