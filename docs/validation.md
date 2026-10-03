# Evidencia de validación

Validaciones realizadas localmente el 2 de octubre de 2026 (America/Lima):

| Control | Resultado |
|---|---|
| dotnet test backend/Wallet.sln, SQLite real | 25 casos aprobados, 0 fallos |
| Cobertura | Archivos OpenCover y Cobertura generados |
| Cobertura backend medida | 95.20% de líneas y 62.50% de ramas |
| npm test | 12 casos aprobados |
| npm run build | TypeScript y Vite, compilación correcta |
| npm audit, incluidas dependencias de desarrollo | 0 vulnerabilidades después de actualizar Vitest |
| dotnet list package --vulnerable --include-transitive | Sin vulnerabilidades conocidas después de actualizar SQLite y paquetes de pruebas |
| Terraform fmt -check | Correcto |
| Terraform init -backend=false | Correcto, provider AzureRM firmado y lockfile generado |
| Terraform validate | Configuración válida |
| actionlint | Los seis workflows pasan el análisis sintáctico y de expresiones |
| docker compose config --quiet | Configuración válida |
| React + API en navegador | Crear dos billeteras, recargar 25.50, transferir 5.10: origen 20.40, destino 5.10 y ambos movimientos Completed |

La prueba de rollback inyecta un trigger que falla al insertar TransferReceived: conserva ambos saldos y no deja TransferSent parcial. La prueba de concurrencia usa dos contextos con versiones distintas para impedir una sobrescritura obsoleta.

Limitaciones verificadas del entorno:

- Docker Desktop está instalado pero su motor Linux no responde. La construcción/ejecución de contenedores no está validada localmente; CI hace build y smoke test en Ubuntu.
- No hay credenciales Azure: no se ejecutó plan/apply autenticado ni despliegue público.
- No hay SONAR_TOKEN/variables de proyecto: no se atribuye un resultado Sonar o URL inexistente.
- No hay SNYK_TOKEN: los análisis Snyk quedan listos para ejecución con token. Semgrep está configurado como job independiente.
- git push no pudo autenticarse por HTTPS; no existe sesión SSH utilizable (Permission denied, publickey). La conexión GitHub permite consultar metadatos pero rechazó la creación de README con HTTP 403: Resource not accessible by integration. El repositorio remoto permanece vacío y no se ejecutaron sus workflows.
- gh local no tiene autenticación ni extensión student instalada: gh student devuelve unknown command. No se ejecutó gh student submit, porque requiere publicación exitosa, extensión y autenticación.

Los resultados de pipelines remotos se consultan en la pestaña Actions del repositorio. Estar configurado no equivale a haber obtenido una evaluación de seguridad externa o un despliegue exitoso.
