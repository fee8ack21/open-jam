import { ref, computed } from 'vue';
import { defineStore } from 'pinia';

/** 平台會員角色。 */
export type MemberRole = 'User' | 'Admin';
/** 會員帳號狀態。 */
export type MemberStatus = 'Active' | 'Suspended';

/**
 * 平台會員（Auth 服務使用者）的列表 DTO。
 *
 * NOTE(mock): Auth 服務尚未對外提供「列出 / 查詢使用者」的 REST 端點
 * （目前僅 MVC 登入 / 註冊流程），故此型別暫定義於前端，端點補上後改由
 * swagger-typescript-api 自 OpenAPI 產生並移除此定義。
 */
export interface MemberDto {
  id: string;
  email: string;
  displayName: string | null;
  role: MemberRole;
  /** Email 是否已驗證。 */
  emailVerified: boolean;
  status: MemberStatus;
  /** 是否已開店（賣家）。 */
  hasStore: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

// TODO(mock): Auth 服務尚無使用者列表端點。以下為展示用假資料，
// 端點補上後移除 mock 區塊，並啟用 load() 中註解掉的真實 API 呼叫。
const MOCK_MEMBERS: MemberDto[] = [
  {
    id: '00000000-0000-0000-0000-000000000401',
    email: 'admin@openjam.co',
    displayName: '平台管理員',
    role: 'Admin',
    emailVerified: true,
    status: 'Active',
    hasStore: false,
    createdAt: '2026-01-05T02:00:00Z',
    lastLoginAt: '2026-06-24T22:10:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000402',
    email: 'xiaoming@example.com',
    displayName: '小明',
    role: 'User',
    emailVerified: true,
    status: 'Active',
    hasStore: true,
    createdAt: '2026-05-02T07:55:00Z',
    lastLoginAt: '2026-06-23T13:42:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000403',
    email: 'aria@example.com',
    displayName: 'Aria',
    role: 'User',
    emailVerified: true,
    status: 'Active',
    hasStore: true,
    createdAt: '2026-04-18T03:10:00Z',
    lastLoginAt: '2026-06-20T09:05:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000404',
    email: 'nightowl@example.com',
    displayName: '夜貓子',
    role: 'User',
    emailVerified: true,
    status: 'Suspended',
    hasStore: true,
    createdAt: '2026-03-09T15:30:00Z',
    lastLoginAt: '2026-06-12T07:00:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000405',
    email: 'buyer.lin@example.com',
    displayName: '林小姐',
    role: 'User',
    emailVerified: true,
    status: 'Active',
    hasStore: false,
    createdAt: '2026-05-28T11:20:00Z',
    lastLoginAt: '2026-06-24T18:30:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000406',
    email: 'newbie@example.com',
    displayName: null,
    role: 'User',
    emailVerified: false,
    status: 'Active',
    hasStore: false,
    createdAt: '2026-06-22T04:15:00Z',
    lastLoginAt: null,
  },
];

/**
 * 平台管理員的會員列表 store：載入並檢視全平台會員。
 * 僅 Admin 使用。
 *
 * NOTE(mock): 後端尚無使用者列表端點，目前以 MOCK_MEMBERS 假資料呈現；
 * 串接時請移除 mock 區塊，並啟用 load() 中註解掉的真實 API 呼叫。
 */
export const useMemberListStore = defineStore('memberList', () => {
  const items = ref<MemberDto[]>([...MOCK_MEMBERS]); // 全平台會員，新到舊
  const totalCount = ref(MOCK_MEMBERS.length);
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 啟用中會員數，供側欄徽章顯示。 */
  const activeCount = computed(
    () => items.value.filter((m) => m.status === 'Active').length,
  );
  /** 平台管理員人數。 */
  const adminCount = computed(() => items.value.filter((m) => m.role === 'Admin').length);
  /** 已開店（賣家）人數。 */
  const sellerCount = computed(() => items.value.filter((m) => m.hasStore).length);

  /** 載入全平台會員列表。 */
  async function load() {
    // TODO(mock): 改回真實呼叫——
    // loading.value = true;
    // error.value = null;
    // try {
    //   const res = await authApi.users.list({ Offset: 0, Limit: 100 });
    //   items.value = res.data.items ?? [];
    //   totalCount.value = res.data.totalCount ?? items.value.length;
    // } catch (err) {
    //   error.value = '載入會員列表失敗。';
    // } finally {
    //   loading.value = false;
    // }
    items.value = [...MOCK_MEMBERS];
    totalCount.value = MOCK_MEMBERS.length;
  }

  return {
    items,
    totalCount,
    loading,
    error,
    activeCount,
    adminCount,
    sellerCount,
    load,
  };
});
