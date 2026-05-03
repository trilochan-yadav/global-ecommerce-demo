export interface LoginResponse {
  success: boolean;
  token: string;
}

export interface DecodedToken {
  role?: string;
  exp?: number;
  [key: string]: unknown;
}
