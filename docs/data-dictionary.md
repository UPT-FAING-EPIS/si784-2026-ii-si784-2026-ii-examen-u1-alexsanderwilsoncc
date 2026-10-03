# Diccionario de datos

Generado desde metadatos EF Core. Importes API/C#: decimal; SQLite: INTEGER en centavos. Fechas UTC.

## Transaction (Transactions)

| Campo | C# | SQLite | Nulo | Restricción |
|---|---|---|---|---|
| Id | Guid | TEXT | No | PK |
| Amount | Decimal | INTEGER | No |  |
| CreatedAt | DateTime | TEXT | No |  |
| RelatedWalletId | Guid | TEXT | Sí | FK |
| Status | TransactionStatus | TEXT | No |  Completed, Rejected, Failed |
| Type | TransactionType | TEXT | No |  Deposit, TransferSent, TransferReceived |
| WalletId | Guid | TEXT | No | FK |

Índice : RelatedWalletId.

Índice : WalletId, CreatedAt.

- CK_Transaction_Amount: `Amount > 0 AND Amount <= 100000000000000`

- CK_Transaction_Related: `(Type = 'Deposit' AND RelatedWalletId IS NULL) OR (Type <> 'Deposit' AND RelatedWalletId IS NOT NULL AND RelatedWalletId <> WalletId)`

- CK_Transaction_Status: `Status IN ('Completed','Rejected','Failed')`

- CK_Transaction_Type: `Type IN ('Deposit','TransferSent','TransferReceived')`

## Wallet (Wallets)

| Campo | C# | SQLite | Nulo | Restricción |
|---|---|---|---|---|
| Id | Guid | TEXT | No | PK |
| Balance | Decimal | INTEGER | No |  |
| CreatedAt | DateTime | TEXT | No |  |
| CustomerEmail | String | TEXT | No |  longitud máxima 254 |
| CustomerName | String | TEXT | No |  longitud máxima 100 |
| Version | Guid | TEXT | No | Control de concurrencia |

Índice único: CustomerEmail.

- CK_Wallet_Balance: `Balance >= 0 AND Balance <= 100000000000000`

- CK_Wallet_Email: `length(CustomerEmail) BETWEEN 3 AND 254`

- CK_Wallet_Name: `length(CustomerName) BETWEEN 1 AND 100`
