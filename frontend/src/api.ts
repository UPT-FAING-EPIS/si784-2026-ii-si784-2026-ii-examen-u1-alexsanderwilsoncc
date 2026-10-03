export interface Wallet {
  id: string;
  customerName: string;
  customerEmail: string;
  balance: number;
  createdAt: string;
}
export interface Movement {
  id: string;
  walletId: string;
  relatedWalletId: string | null;
  amount: number;
  type: "Deposit" | "TransferSent" | "TransferReceived";
  status: "Completed" | "Rejected" | "Failed";
  createdAt: string;
}
const base = (import.meta.env.VITE_API_URL ?? "").replace(/\/$/, "");
export async function api<T>(
  path: string,
  body?: unknown,
  signal?: AbortSignal,
): Promise<T> {
  const result = await fetch(base + path, {
    method: body === undefined ? "GET" : "POST",
    headers:
      body === undefined ? undefined : { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  });
  if (!result.ok) {
    const problem = await result.json().catch(() => null);
    const details = problem?.errors
      ? Object.values(problem.errors).flat().join(" ")
      : "";
    throw new Error(
      details || problem?.title || "No se pudo conectar con la billetera.",
    );
  }
  return result.json() as Promise<T>;
}
