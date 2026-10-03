export function validAmount(value: string): boolean {
  return (
    /^\d+(\.\d{1,2})?$/.test(value) &&
    Number(value) > 0 &&
    Number(value) <= 1_000_000_000_000
  );
}
export function validTransfer(
  from: string,
  to: string,
  amount: string,
  balance: number,
): boolean {
  return (
    !!from &&
    !!to &&
    from !== to &&
    validAmount(amount) &&
    Number(amount) <= balance
  );
}
