import { UserManager, WebStorageStateStore, Log } from 'oidc-client-ts';
import type { UserManagerSettings } from 'oidc-client-ts';
import { env } from '@/environment';

Log.setLogger(console);
Log.setLevel(Log.WARN);

const authChannel = 'BroadcastChannel' in window ? new BroadcastChannel('auth') : null;

const config: UserManagerSettings = {
  authority: env.OIDC_AUTHORITY,
  client_id: env.OIDC_CLIENT_ID,
  redirect_uri: new URL(
    'callback.html',
    new URL(import.meta.env.BASE_URL, document.location.origin),
  ).href,
  silent_redirect_uri: new URL(
    'silent-renew.html',
    new URL(import.meta.env.BASE_URL, document.location.origin),
  ).href,
  post_logout_redirect_uri: new URL(import.meta.env.BASE_URL, document.location.origin).href,
  response_type: 'code',
  scope: 'openid profile email offline_access',
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  automaticSilentRenew: true,
  monitorSession: true,
};

export function createUserManager() {
  const manager = new UserManager(config);

  if (authChannel) {
    authChannel.onmessage = (msg) => {
      if (msg.data === 'logout') {
        manager.removeUser().then(() => window.location.replace('/'));
      }
    };
  } else {
    window.addEventListener('storage', (event) => {
      if (event.key === 'logout-event') {
        manager.removeUser().then(() => window.location.replace('/'));
      }
    });
  }

  return manager;
}

export const userManager = createUserManager();

export function login(redirectPath?: string) {
  const target = redirectPath ?? window.location.href;
  userManager.signinRedirect({ state: target }).catch(console.error);
}

export async function logout() {
  const user = await userManager.getUser();
  if (!user) return;

  if (authChannel) {
    authChannel.postMessage('logout');
  } else {
    localStorage.setItem('logout-event', Date.now().toString());
  }

  try {
    await userManager.signoutRedirect().catch(console.error);
  } catch {
    await userManager.removeUser();
  }
}

export async function getUser() {
  return await userManager.getUser();
}

/**
 * 向 Hydra 進行 prompt=none 的 silent check，確認 SSO session 是否仍然存在。
 * 用於偵測「在其他子網域登出」的情況：本地 token 雖未過期，但 Hydra session 已被銷毀。
 */
export async function validateSession() {
  try {
    return await userManager.signinSilent();
  } catch {
    await userManager.removeUser();
    return null;
  }
}
