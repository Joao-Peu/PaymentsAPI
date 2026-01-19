# PaymentsAPI

Microserviço responsável pelo processamento de pagamentos de pedidos.

## Visão geral
- Processa eventos de pedido via RabbitMQ (MassTransit) e simula aprovação/rejeição de pagamentos.
- Expõe endpoints HTTP para consulta de pagamentos.
- Persiste os pagamentos em SQL Server via Entity Framework Core.
- Pronto para execução em Docker e implantação em Kubernetes.

## Arquitetura
- `Core`: Entidades e enums de domínio.
  - `Payment`: representa um pagamento com `Id`, `OrderId`, `Amount`, `Status`, `CreatedAt`.
  - `PaymentStatus`: `Pending`, `Approved`, `Rejected`.
- `Application`: Consumers do MassTransit.
  - `OrderPlacedConsumer`: consome eventos de pedido, cria/processa um `Payment` e persiste via repositório.
- `Infrastructure`:
  - `Data`: `PaymentsDbContext` (EF Core + SQL Server) e `Migrations`.
  - `Repositories`: `IPaymentRepository` e `EfPaymentRepository` (implementação com EF Core).
- `Controllers`: Endpoints de API (Swagger habilitado em desenvolvimento).
- `Program.cs`: Bootstrap do serviço, DI, MassTransit/RabbitMQ, EF Core e Swagger.

## Fluxo principal
1. O serviço inicia, aplica migrations (`db.Database.Migrate()`) e se conecta ao RabbitMQ.
2. `OrderPlacedConsumer` é registrado no endpoint `order-placed-queue`.
3. Ao receber um evento de pedido:
   - Cria um `Payment`.
   - Simula processamento e define `Status` para `Approved` ou `Rejected`.
   - Persiste via `EfPaymentRepository` no SQL Server.

## Integração com RabbitMQ (MassTransit)
- Configuração via `RabbitMQ:Host`, `Username`, `Password` (appsettings ou variáveis de ambiente).
- Fila: `order-placed-queue`.
- `OrderPlacedConsumer` configurado em `Program.cs` com `ConfigureConsumer`.

### Envio de mensagem para testes (sem producer MassTransit)
- Content-Type: `application/vnd.masstransit+json`.
- Publicar na fila `order-placed-queue` com o envelope:

```
{
  "messageType": ["urn:message:PaymentsAPI.Core.Events:OrderPlacedEvent"],
  "message": {
    "orderId": "11111111-1111-1111-1111-111111111111",
    "userId": "22222222-2222-2222-2222-222222222222",
    "gameId": "33333333-3333-3333-3333-333333333333",
    "price": 149.90
  }
}
```

## Persistência (SQL Server)
- Connection string lida de `ConnectionStrings:PaymentsDb`.
- Migrations são aplicadas automaticamente no startup (`db.Database.Migrate()`).
- Opcional: aplicar via CLI antes de iniciar a API:
  - `dotnet ef database update` (no projeto `PaymentsAPI`).
- Tabela principal: `dbo.Payments`.

## Endpoints HTTP
- `GET /api/payments/by-order/{orderId}`: retorna o pagamento pelo `OrderId`.
- Swagger habilitado em desenvolvimento.

## Configuração
- `appsettings.json` (opcional):
  - `ConnectionStrings:PaymentsDb` – ex.: `Server=localhost;Database=PaymentsDb;User Id=sa;Password=Your_strong_password_123;TrustServerCertificate=True;`.
  - `RabbitMQ:Host`, `RabbitMQ:Username`, `RabbitMQ:Password`.
- Variáveis de ambiente (exemplos):
  - `ConnectionStrings__PaymentsDb`
  - `RabbitMQ__Host`, `RabbitMQ__Username`, `RabbitMQ__Password`

## Executando localmente
- `dotnet build`
- `dotnet run` no projeto `PaymentsAPI`.
- Aguarde a aplicação aplicar as migrations e conectar no RabbitMQ.

## Teste ponta a ponta
1. Inicie a API.
2. Publique o evento `OrderPlacedEvent` na fila `order-placed-queue` (usar envelope acima).
3. Verifique o registro na tabela `dbo.Payments` ou consulte `GET /api/payments/by-order/{orderId}`.

## Troubleshooting
- `Invalid object name 'Payments'`/`'dbo.Payments'`: banco/tabela não existem no alvo. Use a mesma connection string da API e aplique migrations com `dotnet ef database update`. Confirme permissões do usuário.
- Falha de desserialização JSON: se publicar direto no RabbitMQ, use o envelope MassTransit e o content-type `application/vnd.masstransit+json`. Alternativamente, publique via um producer MassTransit .NET.

## Docker
- Build: `docker build -t paymentsapi:latest -f PaymentsAPI/Dockerfile .`
- Run: `docker run -p 8080:80 --env RabbitMQ__Host=<host> --env RabbitMQ__Username=<user> --env RabbitMQ__Password=<pass> --env ConnectionStrings__PaymentsDb="<conn>" paymentsapi:latest`

## Kubernetes
- Manifests em `PaymentsAPI/k8s`.
- Configure secrets para credenciais do RabbitMQ e connection string.
- Deploy: `kubectl apply -f PaymentsAPI/k8s`.

## Tecnologias
- .NET 8, C# 12
- MassTransit
- RabbitMQ
- Entity Framework Core (SQL Server)
- Swagger/OpenAPI
- Docker, Kubernetes