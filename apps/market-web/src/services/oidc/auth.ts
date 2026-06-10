import { UserManager, WebStorageStateStore, Log, type User } from 'oidc-client-ts';
import { env } from '@/environment.js';

Log.setLogger(console);
Log.setLevel(Log.WARN);

const authChannel: BroadcastChannel | null =
  'BroadcastChannel' in window ? new BroadcastChannel('auth') : null;

const config = {
  authority: env.HYDRA_PUBLIC_URL,
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

export function createUserManager(): UserManager {
  const manager = new UserManager(config);

  if (authChannel) {
    authChannel.onmessage = (msg: MessageEvent<string>) => {
      if (msg.data === 'logout') {
        manager.removeUser().then(() => window.location.replace('/'));
      }
    };
  } else {
    window.addEventListener('storage', (event: StorageEvent) => {
      if (event.key === 'logout-event') {
        manager.removeUser().then(() => window.location.replace('/'));
      }
    });
  }

  return manager;
}

export const userManager = createUserManager();

export function login(redirectPath?: string): void {
  const target = redirectPath ?? window.location.href;
  userManager.signinRedirect({ state: target }).catch(console.error);
}

export async function logout(): Promise<void> {
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

export async function getUser(): Promise<User | null> {
  return await userManager.getUser();
}
