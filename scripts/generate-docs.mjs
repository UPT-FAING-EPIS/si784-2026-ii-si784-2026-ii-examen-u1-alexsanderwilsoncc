import { execFileSync } from 'node:child_process';
import { mkdirSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const docs = path.join(root, 'docs');
mkdirSync(docs, { recursive: true });
execFileSync('dotnet', ['run', '--configuration', 'Release', '--project', path.join(root, 'backend/Wallet.Docs'), '--', docs], { stdio: 'inherit' });
const write = (name, title, diagram) => writeFileSync(path.join(docs, name), '# ' + title + '\n\nGenerado automáticamente. Arquitectura declarada por el proyecto.\n\n```mermaid\n' + diagram + '\n```\n');
write('component-diagram.md', 'Diagrama de componentes', `flowchart LR
    User[Cliente] --> React[React + TypeScript]
    React -->|HTTP JSON| Controller[WalletsController]
    Controller --> Validation[DTOs y validación]
    Controller --> Service[WalletService]
    Service --> EF[EF Core]
    EF --> SQLite[(SQLite)]
    Middleware[ErrorMiddleware] --> Controller
    Tests[xUnit + WebApplicationFactory] --> Controller
    Tests --> Service`);
write('deployment-diagram.md', 'Diagrama de despliegue', `flowchart TB
    Browser[Navegador] -->|HTTPS| App[Azure App Service Linux: una instancia]
    subgraph Container[Contenedor no root]
        Frontend[React estático] --> API[ASP.NET Core 8]
    end
    App --> Container
    API --> DB[(SQLite: /home/data/wallet.db)]
    DB --> Volume[Almacenamiento persistente App Service]
    Actions[GitHub Actions] -->|OIDC| Azure[Azure Resource Manager]
    Terraform[Terraform + estado remoto] --> Azure
    Azure --> App
    Actions --> Registry[Azure Container Registry]
    Registry -->|Managed Identity AcrPull| App
    State[(Azure Blob: tfstate)] --- Terraform`);
console.log('Generated all five documentation files.');
