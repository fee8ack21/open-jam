import { createUserManager } from './auth.js';

async function load() {
  const manager = createUserManager();
  try {
    const user = await manager.signinRedirectCallback();
    const workspaceUrl = import.meta.env.VITE_WORKSPACE_URL ?? window.location.origin;
    const redirect =
      user.state && typeof user.state === 'string' ? user.state : workspaceUrl;
    window.location.replace(redirect);
  } catch (error) {
    console.error('OIDC callback error:', error);
    window.location.replace('/');
  }
}

load();
export {};
