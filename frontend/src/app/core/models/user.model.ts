export interface User {
  id: string;
  email: string;
  name: string;
  picture: string;
  givenName?: string;
  familyName?: string;
}

export interface GoogleAuthResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  scope: string;
}
