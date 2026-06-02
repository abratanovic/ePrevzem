const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5070";

export type InitiateAuthResponse = {
  attemptId: string;
  qrCodeImage: string;
};

export type CheckPendingResponse = {
  status: "pending";
};
export type CheckCompleteResponse = {
  status: "complete";
  redirectUrl: string;
};

export type CheckAuthResponse = CheckPendingResponse | CheckCompleteResponse;

function buildUrl(path: string) {
  return `${API_BASE_URL}${path}`;
}
export async function initiateAuth(
  redirectUrl: string,
): Promise<InitiateAuthResponse> {
  const url = new URL(buildUrl("/api/auth/initiate"), window.location.origin);
  url.searchParams.set("redirectUrl", redirectUrl);

  const response = await fetch(url.toString(), {
    method: "POST",
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => null);
    throw new Error(errorBody?.error ?? "Failed to initiate authentication");
  }

  return response.json();
}

export async function checkAuth(attemptId: string): Promise<CheckAuthResponse> {
  const response = await fetch(buildUrl(`/api/auth/check/${attemptId}`));

  if (!response.ok) {
    const errorBody = await response.json().catch(() => null);
    throw new Error(errorBody?.error ?? "Failed to check authentication");
  }

  return response.json();
}

export function normalizeQrCodeImage(qrCodeImage: string) {
  if (qrCodeImage.startsWith("data:image")) {
    return qrCodeImage;
  }

  return `data:image/png;base64,${qrCodeImage}`;
}
