import { describe, expect, it } from "vitest";
import { validAmount, validTransfer } from "./validation";
describe("money validation", () => {
  it.each(["0", "-1", "NaN", "Infinity", "1.001", "1e3", "1000000000001", ""])(
    "rejects %s",
    (value) => expect(validAmount(value)).toBe(false),
  );
  it.each(["0.01", "100", "10.25"])("accepts %s", (value) =>
    expect(validAmount(value)).toBe(true),
  );
  it("rejects self transfer, missing recipient and insufficient funds", () => {
    expect(validTransfer("a", "a", "1", 10)).toBe(false);
    expect(validTransfer("a", "", "1", 10)).toBe(false);
    expect(validTransfer("a", "b", "11", 10)).toBe(false);
    expect(validTransfer("a", "b", "10", 10)).toBe(true);
  });
});
