# Aprovisionamiento Azure

App Service Linux B1 (una instancia), ACR Basic sin contraseña administrativa, identidad administrada con AcrPull y SQLite persistente en /home/data. El contenedor incluye React y API bajo el mismo origen. **Los recursos generan costos.** No se ha aprovisionado cloud sin credenciales.

## Configuración exacta

Secrets de GitHub: AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID. Son identificadores del principal OIDC; no se necesita contraseña de Azure.

Variables de GitHub:

| Variable | Valor requerido |
|---|---|
| AZURE_ENABLED | true solamente después de completar configuración |
| AZURE_WEBAPP_NAME | nombre globalmente único, 3-50 letras minúsculas/números/guiones |
| AZURE_REGISTRY_NAME | nombre ACR único, 5-50 alfanuméricos minúsculos |
| AZURE_RESOURCE_GROUP | AZURE_WEBAPP_NAME seguido de -rg |
| TF_STATE_ACCOUNT | cuenta de Storage existente para tfstate |
| TF_STATE_CONTAINER | contenedor Blob existente para tfstate |
| TF_STATE_RESOURCE_GROUP | grupo de esa cuenta de Storage |

Cree previamente la cuenta de Storage/Blob para el estado remoto, active versionado y restrinja acceso. El principal requiere Contributor y Role Based Access Control Administrator sobre el grupo de recursos (o alcance de suscripción limitado para poder crear el grupo); también Storage Blob Data Contributor sobre la cuenta de estado y AcrPush sobre ACR para deploy. Use privilegios mínimos en cuanto estén creados los recursos.

Configure federación OIDC para los environments GitHub **infrastructure** y **production** con sujetos:

- repo:UPT-FAING-EPIS/si784-2026-ii-si784-2026-ii-examen-u1-alexsanderwilsoncc:environment:infrastructure
- repo:UPT-FAING-EPIS/si784-2026-ii-si784-2026-ii-examen-u1-alexsanderwilsoncc:environment:production

Restrinja production/infrastructure a main y configure revisores para infrastructure. No comparta tfstate ni archivos plan: pueden contener datos sensibles.

1. Sin credenciales: terraform fmt -check, terraform init -backend=false y terraform validate funcionan. El workflow omite el job plan/apply con AZURE_ENABLED sin configurar.
2. Con credenciales: ejecutar infra.yml manualmente con apply=false para revisar terraform plan.
3. Ejecutar manualmente con apply=true: genera y aplica un plan de esa ejecución, sujeto al environment infrastructure.
4. La imagen inicial wallet:initial aún no existe; App Service empieza a funcionar después de deploy.yml. Ejecutar deploy.yml manualmente una vez provisionado ACR/App Service.
5. La URL real se obtiene con terraform output application_url; deploy verifica /health y publica la URL en el resumen de Actions.

Para usar CLI: configure ARM_CLIENT_ID, ARM_TENANT_ID, ARM_SUBSCRIPTION_ID y OIDC (o Azure CLI local autenticado), TF_VAR_name y TF_VAR_registry_name. Ejecute terraform init con los backend-config indicados en infra.yml, luego plan/apply.

SQLite exige una sola instancia. No configure escalado horizontal. /home debe ser escribible por UID 1654 (app) en el montaje persistente de App Service; verifique permisos antes de usar el servicio. Para un entorno real multiusuario use una base administrada y autenticación individual. EnsureCreated crea la primera versión del esquema; un cambio posterior requiere migraciones antes de actualizar una base existente.
