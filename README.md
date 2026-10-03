# Moneda · Billetera digital

Examen U1 de Calidad y Pruebas de Software. API .NET 8 + EF Core + SQLite, React/TypeScript/Vite, xUnit, Docker y Terraform Azure. Una solución pequeña con operaciones monetarias atómicas.

## Ejecutar

Requisitos: SDK .NET 8 y Node 22. Dos terminales desde la raíz:

```powershell
dotnet run --project backend/Wallet.Api --no-launch-profile --urls http://localhost:5000
```

```powershell
cd frontend
npm ci
npm run dev
```

Abrir http://localhost:5173. Swagger: http://localhost:5000/swagger. Vite incluye un proxy hacia la API local; frontend/.env.example documenta VITE_API_URL si se necesita otro origen. En producción la imagen combinada sirve React y API desde el mismo origen.

Flujo de validación: crear dos billeteras con distintos correos, seleccionar la primera, recargar 100, transferir 25 a la segunda y consultar ambos historiales/saldos. La UI refresca el saldo e historial cada 10 segundos y después de cada operación, presenta estados de carga, éxito y error.

## API

| Método | Ruta | Resultado |
|---|---|---|
| POST | /wallets | Crea billetera, 201 y Location |
| GET | /wallets | Lista real de billeteras |
| GET | /wallets/{id} | Datos y saldo |
| GET | /wallets/{id}/balance | walletId, balance |
| POST | /wallets/{id}/deposit | Recarga y movimiento Completed |
| POST | /wallets/transfer | Débito/crédito y dos movimientos atómicos |
| GET | /wallets/{id}/transactions | Historial descendente por fecha UTC |
| GET | /health | Comprobación HTTP |

```json
{"customerName":"Alex","customerEmail":"alex@example.com"}
```
```json
{"amount":100}
```
```json
{"fromWalletId":"GUID_ORIGEN","toWalletId":"GUID_DESTINO","amount":25}
```

Errores ProblemDetails: 400 validación/saldo insuficiente, 404 billetera inexistente, 409 correo duplicado o conflicto concurrente, 500 mensaje genérico sin stack trace. No reintente automáticamente una operación monetaria ante una respuesta perdida: esta versión no implementa claves de idempotencia.

Importes decimal en C#, nunca float/double. SQLite almacena INTEGER en centavos mediante conversión EF para conservar exactitud. Se permiten hasta dos decimales y saldo/importe máximo de 1 billón de soles. NaN/infinito y valores fuera de rango no se deserializan como decimal válido. Correos normalizados con índice único, PK/FK, CHECK e índices de historial. Version actúa como token de concurrencia; depósitos y transferencias utilizan transacciones SQLite. Una excepción revierte saldos y movimientos.

Los estados Completed, Rejected y Failed están definidos y restringidos en base de datos. Solo las operaciones completadas se persisten; rechazos y fallos se devuelven como errores HTTP y no crean movimientos parciales. EnsureCreated inicializa automáticamente una base nueva; no aplica cambios a esquemas existentes.

## Pruebas y controles

```powershell
dotnet test backend/Wallet.sln --settings backend/coverage.runsettings --collect "XPlat Code Coverage"
cd frontend
npm test
npm run build
npm audit
```

También puede ejecutar dotnet test desde backend/. La solución contiene 25 casos xUnit (servicio con SQLite en memoria y endpoints WebApplicationFactory con SQLite temporal), incluyendo fallo inyectado con trigger para demostrar rollback y actualización obsoleta para demostrar concurrencia. El frontend incluye 12 casos de validación. Cobertura OpenCover y Cobertura generada para CI y Sonar. Ver [evidencia de validación](docs/validation.md).

## Contenedores

```powershell
docker build -t wallet-api ./backend/Wallet.Api
docker build -t wallet-app .
docker compose up --build
```

Compose sirve frontend en http://localhost:8080 y backend en http://localhost:5000, con volumen wallet-data persistente. La imagen combinada wallet-app contiene ambos. Las imágenes de ejecución usan usuario no root. Docker requiere su motor Linux activo. CI construye ambas imágenes y prueba HTTP, recarga y persistencia tras reiniciar el contenedor.

## Automatizaciones

| Workflow | Responsabilidad |
|---|---|
| ci.yml | Pruebas, cobertura, compilación, documentación y smoke test Docker |
| infra.yml | fmt/init/validate siempre; plan con OIDC configurado; apply manual explícito |
| sonar.yml | restore/build/test/cobertura; SonarCloud si existe configuración |
| snyk-semgrep.yml | Semgrep independiente; Snyk de código, dependencias e imagen con token |
| deploy.yml | Pruebas y builds; imagen como artifact; ACR y App Service cuando Azure está habilitado |
| generase-documentation.yml | Genera cinco documentos, artifact y commit automático en main |

Secrets requeridos: SONAR_TOKEN, SNYK_TOKEN, AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID. Variables Sonar: SONAR_ORGANIZATION, SONAR_PROJECT_KEY. Consulte [terraform/README.md](terraform/README.md) para variables Azure, estado remoto, OIDC y permisos exactos. Los jobs externos explican la configuración ausente sin impedir pruebas, compilación o Semgrep.

## Documentación automática

```powershell
node scripts/generate-docs.mjs
```

Wallet.Docs lee los metadatos EF Core: propiedades, tipos relacionales, nulabilidad, claves, índices y restricciones; refleja métodos públicos para el diagrama de clases. El script añade diagramas de componentes/despliegue desde plantillas que declaran la arquitectura del proyecto. Sin fechas de generación para producir resultados reproducibles.

- [Diccionario de datos](docs/data-dictionary.md)
- [Entidad-relación](docs/er-diagram.md)
- [Clases](docs/class-diagram.md)
- [Componentes](docs/component-diagram.md)
- [Despliegue](docs/deployment-diagram.md)


