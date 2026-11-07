export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
}

export interface RegisterResponse {
  userId: string;
  userName: string;
  email: string;
}

export interface AuthenticationResponse {
  accessToken: string;
  userId: string;
}
