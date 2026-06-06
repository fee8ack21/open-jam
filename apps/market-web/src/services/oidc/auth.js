import { UserManager, WebStorageStateStore, Log } from 'oidc-client-ts';

Log.setLogger(console);
Log.setLevel(Log.WARN);

const authChannel = 'BroadcastChannel' in window ? new BroadcastChannel('auth') : null;

const config = {
  authority: import.meta.env.VITE_HYDRA_PUBLIC_URL ?? 'http://localhost:4444',
  client_id: import.meta.env.VITE_OIDC_CLIENT_ID ?? 'open-jam-web',
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

export function login(redirectPath) {
  const target = redirectPath ?? import.meta.env.VITE_WORKSPACE_URL ?? window.location.origin;
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
