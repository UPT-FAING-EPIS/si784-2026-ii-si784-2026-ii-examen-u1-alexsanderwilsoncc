import React, { useCallback, useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { api, type Wallet, type Movement } from "./api";
import { validAmount, validTransfer } from "./validation";
import "./style.css";
const money = (value: number) =>
  new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" }).format(
    value,
  );
const labels = {
  Deposit: "Recarga",
  TransferSent: "Transferencia enviada",
  TransferReceived: "Transferencia recibida",
};
const states = {
  Completed: "Completada",
  Rejected: "Rechazada",
  Failed: "Fallida",
};
function App() {
  const [wallets, setWallets] = useState<Wallet[]>([]);
  const [selected, setSelected] = useState("");
  const [movements, setMovements] = useState<Movement[]>([]);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [revision, setRevision] = useState(0);
  const [notice, setNotice] = useState<{ text: string; error: boolean } | null>(
    null,
  );
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [deposit, setDeposit] = useState("");
  const [amount, setAmount] = useState("");
  const [recipient, setRecipient] = useState("");
  const current = wallets.find((w) => w.id === selected);
  const refresh = useCallback(async () => {
    const list = await api<Wallet[]>("/wallets");
    setWallets(list);
    setSelected((previous) =>
      list.some((w) => w.id === previous) ? previous : (list[0]?.id ?? ""),
    );
  }, []);
  useEffect(() => {
    refresh()
      .catch((e) => setNotice({ text: e.message, error: true }))
      .finally(() => setLoading(false));
  }, [refresh]);
  useEffect(() => {
    if (!selected) {
      setMovements([]);
      return;
    }
    const controller = new AbortController();
    setLoading(true);
    setMovements([]);
    async function load() {
      const [wallet, history] = await Promise.all([
        api<Wallet>("/wallets/" + selected, undefined, controller.signal),
        api<Movement[]>(
          "/wallets/" + selected + "/transactions",
          undefined,
          controller.signal,
        ),
      ]);
      if (!controller.signal.aborted) {
        setWallets((list) =>
          list.map((w) => (w.id === wallet.id ? wallet : w)),
        );
        setMovements(history);
      }
    }
    void load()
      .catch((e) => {
        if (!controller.signal.aborted)
          setNotice({ text: e.message, error: true });
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    const timer = window.setInterval(() => void load().catch(() => {}), 10000);
    return () => {
      controller.abort();
      window.clearInterval(timer);
    };
  }, [selected, revision]);
  async function run(action: () => Promise<void>, message: string) {
    setBusy(true);
    setNotice(null);
    try {
      await action();
      await refresh();
      setRevision((value) => value + 1);
      setNotice({ text: message, error: false });
    } catch (e) {
      setNotice({
        text: e instanceof Error ? e.message : "Error inesperado.",
        error: true,
      });
    } finally {
      setBusy(false);
    }
  }
  return (
    <div className="shell">
      <aside>
        <a className="brand" href="/">
          ◈ <span>moneda</span>
        </a>
        <div className="nav-item">▦ &nbsp; Mi billetera</div>
        <p className="aside-note">
          Tu dinero, en movimiento.
          <br />
          Simple. Claro. A tu alcance.
        </p>
        <span className="demo-label">ENTORNO ACADÉMICO · PEN</span>
      </aside>
      <main>
        <header>
          <div>
            <p className="eyebrow">BILLETERA DIGITAL</p>
            <h1>
              Hola, {current?.customerName ?? "bienvenido"}
              <span>.</span>
            </h1>
            <p>Un espacio para tu saldo y tus movimientos.</p>
          </div>
          <button
            className="secondary"
            disabled={busy || loading}
            onClick={() => void run(refresh, "Datos actualizados.")}
          >
            ↻ Actualizar
          </button>
        </header>
        {notice && (
          <div
            role={notice.error ? "alert" : "status"}
            className={"notice " + (notice.error ? "error" : "")}
          >
            {notice.text}
          </div>
        )}
        <div className="top-grid">
          <section className="balance-card">
            <div className="balance-top">
              <span>Saldo disponible</span>
              <span className="coin">S/</span>
            </div>
            <strong>{money(current?.balance ?? 0)}</strong>
            <label htmlFor="wallet">Billetera activa</label>
            <select
              id="wallet"
              value={selected}
              disabled={busy}
              onChange={(e) => {
                setSelected(e.target.value);
                setRecipient("");
              }}
            >
              {!wallets.length && (
                <option value="">Crea tu primera billetera</option>
              )}
              {wallets.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.customerName} · {w.customerEmail}
                </option>
              ))}
            </select>
            <small>
              {current
                ? "ID: " + current.id
                : "Tus fondos aparecerán aquí después de una recarga."}
            </small>
          </section>
          <section className="card">
            <p className="eyebrow">EMPIEZA AQUÍ</p>
            <h2>Crear una billetera</h2>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                void run(async () => {
                  const w = await api<Wallet>("/wallets", {
                    customerName: name.trim(),
                    customerEmail: email.trim(),
                  });
                  setSelected(w.id);
                  setName("");
                  setEmail("");
                }, "Billetera creada. Ya puedes recargar fondos.");
              }}
            >
              <label htmlFor="name">Nombre del cliente</label>
              <input
                id="name"
                required
                maxLength={100}
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Nombre completo"
              />
              <label htmlFor="email">Correo electrónico</label>
              <input
                id="email"
                type="email"
                required
                maxLength={254}
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="nombre@ejemplo.com"
              />
              <button disabled={busy || !name.trim() || !email.trim()}>
                Crear billetera ↗
              </button>
            </form>
          </section>
        </div>
        <div className="actions-grid">
          <section className="card">
            <div className="section-icon">＋</div>
            <h2>Recargar fondos</h2>
            <p>Agrega dinero a la billetera seleccionada.</p>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                if (!validAmount(deposit)) return;
                void run(async () => {
                  await api("/wallets/" + selected + "/deposit", {
                    amount: Number(deposit),
                  });
                  setDeposit("");
                }, "Recarga completada. Tu saldo se actualizó.");
              }}
            >
              <label htmlFor="deposit">Importe en soles</label>
              <input
                id="deposit"
                type="number"
                min="0.01"
                max="1000000000000"
                step="0.01"
                required
                value={deposit}
                onChange={(e) => setDeposit(e.target.value)}
                placeholder="0.00"
              />
              <button
                disabled={busy || loading || !current || !validAmount(deposit)}
              >
                Recargar fondos
              </button>
            </form>
          </section>
          <section className="card">
            <div className="section-icon">↗</div>
            <h2>Transferir dinero</h2>
            <p>Envía fondos a otra billetera, al instante.</p>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                if (
                  !validTransfer(
                    selected,
                    recipient,
                    amount,
                    current?.balance ?? 0,
                  )
                )
                  return;
                void run(async () => {
                  await api("/wallets/transfer", {
                    fromWalletId: selected,
                    toWalletId: recipient,
                    amount: Number(amount),
                  });
                  setAmount("");
                }, "Transferencia completada. Ambos saldos se actualizaron.");
              }}
            >
              <label htmlFor="recipient">Billetera de destino</label>
              <select
                id="recipient"
                required
                value={recipient}
                onChange={(e) => setRecipient(e.target.value)}
              >
                <option value="">Selecciona un destinatario</option>
                {wallets
                  .filter((w) => w.id !== selected)
                  .map((w) => (
                    <option key={w.id} value={w.id}>
                      {w.customerName} · {w.customerEmail}
                    </option>
                  ))}
              </select>
              <label htmlFor="amount">Importe en soles</label>
              <input
                id="amount"
                type="number"
                min="0.01"
                max={current?.balance ?? 0}
                step="0.01"
                required
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="0.00"
              />
              <button
                disabled={
                  busy ||
                  loading ||
                  !validTransfer(
                    selected,
                    recipient,
                    amount,
                    current?.balance ?? 0,
                  )
                }
              >
                Enviar transferencia ↗
              </button>
            </form>
          </section>
        </div>
        <section className="card history">
          <div className="history-heading">
            <div>
              <p className="eyebrow">TU ACTIVIDAD</p>
              <h2>Últimos movimientos</h2>
            </div>
            <span>{movements.length} movimientos</span>
          </div>
          {loading || busy ? (
            <p role="status">Actualizando billetera…</p>
          ) : movements.length ? (
            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>Movimiento</th>
                    <th>Fecha</th>
                    <th>Importe</th>
                    <th>Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {movements.map((t) => (
                    <tr key={t.id}>
                      <td>
                        <b>{labels[t.type]}</b>
                        {t.relatedWalletId && (
                          <small>
                            {wallets.find((w) => w.id === t.relatedWalletId)
                              ?.customerName ?? t.relatedWalletId}
                          </small>
                        )}
                      </td>
                      <td>
                        {new Date(t.createdAt).toLocaleString("es-PE", {
                          timeZone: "America/Lima",
                        })}
                      </td>
                      <td
                        className={t.type === "TransferSent" ? "" : "positive"}
                      >
                        {t.type === "TransferSent" ? "−" : "+"}
                        {money(t.amount)}
                      </td>
                      <td>
                        <span className="badge">{states[t.status]}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="empty">
              ↔<p>Aún no hay movimientos.</p>
              <small>Las recargas y transferencias aparecerán aquí.</small>
            </div>
          )}
        </section>
        <footer>
          Moneda · Calidad y Pruebas de Software{" "}
          <span>Los saldos se consultan cada 10 segundos.</span>
        </footer>
      </main>
    </div>
  );
}
createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
