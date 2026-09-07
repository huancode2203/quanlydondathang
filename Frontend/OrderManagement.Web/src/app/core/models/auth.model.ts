export interface UserSession {
  token: string;
  expiresAt: string;
  employeeId: number;
  username: string;
  fullName: string;
  roleName: string;
  permissions: string[];
}
