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
  - `OrderPlacedConsumer`: consome eventos de `OrderPlacedEvent`, processa pagamento (80% aprovação, 20% rejeição) e publica `PaymentProcessedEvent`.
- `Infrastructure`:
  - `Data`: `PaymentsDbContext` (EF Core + SQL Server) e `Migrations`.
  - `Repositories`: `IPaymentRepository` e `PaymentRepository` (implementação com EF Core).
- `Controllers`: Endpoints de API (Swagger habilitado em desenvolvimento).
- `Shared`: Eventos compartilhados.
  - `OrderPlacedEvent`: evento de pedido realizado.
  - `PaymentProcessedEvent`: evento de pagamento processado.
- `Program.cs`: Bootstrap do serviço, DI, MassTransit/RabbitMQ, EF Core e Swagger.

## Fluxo principal
1. O serviço inicia, aplica migrations (`db.Database.Migrate()`) e se conecta ao RabbitMQ.
2. `OrderPlacedConsumer` é registrado na fila configurada (padrão: `payment.created`).
3. Ao receber um evento `OrderPlacedEvent`:
   - Cria um `Payment` com ID único.
   - Simula processamento com 80% de chance de aprovação.
   - Define `Status` para `Approved` ou `Rejected`.
   - Persiste via `PaymentRepository` no SQL Server.
   - Publica `PaymentProcessedEvent` para outros serviços consumirem.

## Integração com RabbitMQ (MassTransit)
- Configuração via variáveis de ambiente (recomendado) ou appsettings.json.
- **Variáveis de ambiente:**
  - `RABBITMQ_HOST`: Host do RabbitMQ (padrão: `rabbitmq`).
  - `RABBITMQ_USERNAME`: Usuário do RabbitMQ (padrão: `fiap`).
  - `RABBITMQ_PASSWORD`: Senha do RabbitMQ (padrão: `fiap123`).
  - `RABBITMQ_VHOST`: Virtual host do RabbitMQ (padrão: `/`).
  - `RABBITMQ_QUEUE_PAYMENT_CREATED`: Nome da fila para consumir (padrão: `payment.created`).

### Estrutura dos Eventos

**OrderPlacedEvent (Input):**
```csharp
public record OrderPlacedEvent(Guid OrderId, Guid UserId, Guid GameId, decimal Price);
```

**PaymentProcessedEvent (Output):**
```csharp
public record PaymentProcessedEvent(Guid OrderId, Guid UserId, Guid GameId, decimal Price, string Status);
```

### Envio de mensagem para testes

#### Opção 1: Via RabbitMQ Management UI
1. Acesse `http://localhost:15672` (user: `fiap`, password: `fiap123`)
2. Vá para **Queues** ? `payment.created` ? **Publish message**
3. Use o payload com envelope MassTransit:

```json
{
  "messageId": "00000000-0000-0000-0000-000000000001",
  "conversationId": "00000000-0000-0000-0000-000000000002",
  "sourceAddress": "rabbitmq://rabbitmq/test",
  "destinationAddress": "rabbitmq://rabbitmq/payment.created",
  "messageType": [
    "urn:message:Shared.Events:OrderPlacedEvent"
  ],
  "message": {
    "orderId": "a3d5c6e7-8f9a-4b1c-9d2e-3f4a5b6c7d8e",
    "userId": "b4e6d7f8-9a0b-5c2d-0e3f-4a5b6c7d8e9f",
    "gameId": "c5f7e8g9-0b1c-6d3e-1f4a-5b6c7d8e9f0a",
    "price": 59.99
  },
  "sentTime": "2025-01-19T10:30:00Z"
}
```

**Headers necessários:**
- `Content-Type`: `application/json`

#### Opção 2: Payloads de Teste Rápido

**Teste 1 - Jogo AAA ($299.99):**
```json
{
  "orderId": "11111111-1111-1111-1111-111111111111",
  "userId": "22222222-2222-2222-2222-222222222222",
  "gameId": "33333333-3333-3333-3333-333333333333",
  "price": 299.99
}
```

**Teste 2 - Jogo Indie ($19.99):**
```json
{
  "orderId": "44444444-4444-4444-4444-444444444444",
  "userId": "55555555-5555-5555-5555-555555555555",
  "gameId": "66666666-6666-6666-6666-666666666666",
  "price": 19.99
}
```

**Teste 3 - DLC ($9.99):**
```json
{
  "orderId": "77777777-7777-7777-7777-777777777777",
  "userId": "88888888-8888-8888-8888-888888888888",
  "gameId": "99999999-9999-9999-9999-999999999999",
  "price": 9.99
}
```

## Persistência (SQL Server)
- Connection string lida de `ConnectionStrings__PaymentsDb` (variável de ambiente) ou `ConnectionStrings:PaymentsDb` (appsettings).
- Migrations são aplicadas automaticamente no startup (`db.Database.Migrate()`).
- Tabela principal: `dbo.Payments` com índice único em `OrderId`.

### Schema da Tabela Payments
```sql
CREATE TABLE [dbo].[Payments] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [OrderId] UNIQUEIDENTIFIER NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [Status] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    CONSTRAINT [IX_Payments_OrderId] UNIQUE ([OrderId])
);
```

## Endpoints HTTP
- `GET /api/payments/by-order/{orderId}`: retorna o pagamento pelo `OrderId`.
- Swagger habilitado em desenvolvimento em `/swagger`.

## Configuração

### Variáveis de Ambiente (Recomendado para Kubernetes)
```bash
# RabbitMQ
RABBITMQ_HOST=rabbitmq
RABBITMQ_USERNAME=fiap
RABBITMQ_PASSWORD=fiap123
RABBITMQ_VHOST=/
RABBITMQ_QUEUE_PAYMENT_CREATED=payment.created

# SQL Server
ConnectionStrings__PaymentsDb="Server=sql-payments,1433;Database=PaymentsDb;User Id=sa;Password=StrongPassword!123;TrustServerCertificate=True;"
```

### appsettings.json (Opcional para desenvolvimento local)
```json
{
  "ConnectionStrings": {
    "PaymentsDb": "Server=localhost;Database=PaymentsDb;User Id=sa;Password=Your_strong_password_123;TrustServerCertificate=True;"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

## Executando localmente
1. **Pré-requisitos:**
   - .NET 8 SDK
   - SQL Server
   - RabbitMQ

2. **Build:**
   ```bash
   dotnet build
   ```

3. **Aplicar migrations (opcional, já é feito no startup):**
   ```bash
   dotnet ef database update --project PaymentsAPI
   ```

4. **Executar:**
   ```bash
   dotnet run --project PaymentsAPI
   ```

5. **Verificar logs:**
   ```
   Configuring RabbitMQ: Host=localhost, User=guest, VHost=/, Queue=payment.created
   Bus started: rabbitmq://localhost/
   Now listening on: http://[::]:8080
   Application started.
   ```

## Teste ponta a ponta
1. Inicie a API.
2. Publique o evento `OrderPlacedEvent` na fila `payment.created` (usar payloads de teste acima).
3. Verifique:
   - **Logs da aplicação**: `dotnet run` ou `kubectl logs -l app=paymentsapi --follow`
   - **Banco de dados**: `SELECT * FROM Payments ORDER BY CreatedAt DESC;`
   - **RabbitMQ Management UI**: Verifique se a mensagem foi consumida e o evento `PaymentProcessedEvent` foi publicado.

## Troubleshooting
- `Invalid object name 'Payments'`/`'dbo.Payments'`: banco/tabela não existem no alvo. Use a mesma connection string da API e aplique migrations com `dotnet ef database update`. Confirme permissões do usuário.
- Falha de desserialização JSON: se publicar direto no RabbitMQ, use o envelope MassTransit e o content-type `application/vnd.masstransit+json`. Alternativamente, publique via um producer MassTransit .NET.

## Docker
- Build: `docker build -t paymentsapi:latest -f PaymentsAPI/Dockerfile .`
- Run: `docker run -p 8080:80 --env RabbitMQ__Host=<host> --env RabbitMQ__Username=<user> --env RabbitMQ__Password=<pass> --env ConnectionStrings__PaymentsDb="<conn>" paymentsapi:latest`

## Kubernetes

### Estrutura dos Manifestos
```
k8s/
??? configmap.yml          # Configurações do RabbitMQ
??? secret.yaml            # Credenciais sensíveis
??? deployment.yaml        # Deployment da API
??? service.yaml           # Service da API (se existir)
??? rabbitmq-payments-deployment.yml  # RabbitMQ
```

### Deploy Completo

1. **Aplicar ConfigMap:**
   ```bash
   kubectl apply -f k8s/configmap.yml
   ```

2. **Aplicar Secrets:**
   ```bash
   kubectl apply -f k8s/secret.yaml
   ```

3. **Deploy RabbitMQ:**
   ```bash
   kubectl apply -f k8s/rabbitmq-payments-deployment.yml
   ```

4. **Deploy PaymentsAPI:**
   ```bash
   kubectl apply -f k8s/deployment.yaml
   ```

5. **Verificar Pods:**
   ```bash
   kubectl get pods
   kubectl logs -l app=paymentsapi --follow
   ```

6. **Verificar Conexão:**
   ```bash
   # Deve aparecer:
   # Configuring RabbitMQ: Host=rabbitmq, User=fiap, VHost=/, Queue=payment.created
   # Bus started: rabbitmq://rabbitmq/
   ```

### Port-Forward para Testes
```bash
# RabbitMQ Management UI
kubectl port-forward svc/rabbitmq 15672:15672

# PaymentsAPI (se houver service)
kubectl port-forward svc/paymentsapi 8080:80
```

### Troubleshooting Kubernetes

**Problema: Pod mostrando "Connection Failed: rabbitmq://localhost/"**

**Solução:** 
- Verifique se as variáveis de ambiente estão configuradas corretamente:
  ```bash
  kubectl describe pod -l app=paymentsapi | grep -A 10 "Environment:"
  ```
- Reconstrua a imagem Docker sem cache:
  ```bash
  docker build --no-cache -t paymentsapi:latest .
  ```
- Force o Kubernetes a usar a nova imagem:
  ```bash
  kubectl delete deployment paymentsapi
  kubectl apply -f k8s/deployment.yaml
  ```

**Problema: "ErrImageNeverPull"**

**Solução:**
- Mude `imagePullPolicy` para `IfNotPresent` no deployment.yaml
- Ou use uma tag diferente e reconstrua a imagem

**Problema: MassTransit Configuration Exception**

**Solução:**
- Certifique-se de que `e.Durable` e `e.AutoDelete` são configurados **antes** de `e.ConfigureConsumer` no Program.cs

## Tecnologias
- **.NET 8**, C# 12
- **MassTransit** 8.x (RabbitMQ Transport)
- **RabbitMQ** 3.x (com Management Plugin)
- **Entity Framework Core 8** (SQL Server Provider)
- **Swagger/OpenAPI**
- **Docker**, **Kubernetes**

## Comportamento do Processamento
O `OrderPlacedConsumer` implementa uma lógica de aprovação probabilística:
- **80% de chance** ? `PaymentStatus.Approved`
- **20% de chance** ? `PaymentStatus.Rejected`

Teste múltiplas mensagens para observar ambos os cenários!

## Monitoramento e Logs

### Verificar Logs da Aplicação
```bash
# Local
dotnet run

# Kubernetes
kubectl logs -l app=paymentsapi --follow
kubectl logs <pod-name> --previous  # Para logs do container anterior
```

### Verificar Mensagens no RabbitMQ
1. Acesse Management UI: `http://localhost:15672`
2. Vá para **Queues** ? `payment.created`
3. Verifique:
   - **Messages**: Mensagens na fila
   - **Message rates**: Taxa de processamento
   - **Consumers**: Deve mostrar 1 consumer ativo

### Verificar Banco de Dados
```sql
-- Ver todos os pagamentos
SELECT Id, OrderId, Amount, Status, CreatedAt 
FROM Payments 
ORDER BY CreatedAt DESC;

-- Estatísticas
SELECT Status, COUNT(*) as Total 
FROM Payments 
GROUP BY Status;
```

## CI/CD e Deployment

### Build Automatizado
```bash
# Build da imagem com tag de versão
docker build -t paymentsapi:v1.0.0 .
docker tag paymentsapi:v1.0.0 paymentsapi:latest
```

### Kubernetes Rolling Update
```bash
# Atualizar imagem no deployment
kubectl set image deployment/paymentsapi paymentsapi=paymentsapi:v1.0.1

# Verificar status do rollout
kubectl rollout status deployment/paymentsapi

# Rollback se necessário
kubectl rollout undo deployment/paymentsapi
```

## Contribuindo
1. Fork o repositório
2. Crie uma branch para sua feature (`git checkout -b feature/nova-funcionalidade`)
3. Commit suas mudanças (`git commit -m 'Adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feature/nova-funcionalidade`)
5. Abra um Pull Request

## Licença
[Adicione a licença apropriada aqui]

## Contato
[Adicione informações de contato ou links relevantes]