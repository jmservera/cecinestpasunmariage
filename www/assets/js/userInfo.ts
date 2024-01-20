export type ClientPrincipal = {
  userDetails: string;
  userId: string;
};

export async function getUserInfo(): Promise<ClientPrincipal> {
  const response = await fetch("/.auth/me");
  const payload = await response.json();
  const { clientPrincipal } = payload;
  return clientPrincipal;
}
