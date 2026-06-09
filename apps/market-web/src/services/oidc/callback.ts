import { createUserManager } from './auth.js';
import { env } from '@/environment.js';

async function load(): Promise<void> {
  const manager = createUserManager();
  try {
    const user = await manager.signinRedirectCallback();
    const workspaceUrl = env.WORKSPACE_URL;
    const redirect =
      user.state && typeof user.state === 'string' ? user.state : workspaceUrl;
    window.location.replace(redirect);
  } catch (error) {
    console.error('OIDC callback error:', error);
    window.location.replace(`${env.AUTH_PAGE_URL}error`);
  }
}

load();
export {};
