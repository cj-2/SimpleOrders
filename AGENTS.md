# AGENTS.md

## Project Snapshot
- `SimpleOrders.sln` has 4 projects: `SimpleOrders.Api` (minimal API), `SimpleOrders.Notify` (Kafka worker), `SimpleOrders.DumbRabbit` (Rabbit worker), `SimpleOrders.Shared` (settings/models/messaging services).
- The repository is a messaging playground: persist orders in RavenDB, then publish/consume via Kafka and RabbitMQ (`README.md`).
- Runtime dependencies are orchestrated in `compose.yaml`: `raven`, `broker` (Kafka KRaft), `rabbit`, plus `api`, `notify`, `dumb-rabbit` containers.

## Architecture and Boundaries
- API composition root: `SimpleOrders.Api/Program.cs` wires `KafkaService`, `RabbitMqService`, `RavenDbService`, `OrderService` as singletons.
- Shared infra lives in `SimpleOrders.Shared/Services/`:
  - `KafkaService` creates topics from config and exposes producer/consumer helpers.
  - `RabbitMqService` configures queues/exchanges, retries connection, and buffers unsent messages in-memory (`MessagesToRecovery`).
- Persistence is RavenDB-only in API (`SimpleOrders.Api/Services/OrderService.cs` + `RavenDbService.cs`).

## End-to-End Flows (Use These as Ground Truth)
- Kafka flow: `POST /k/order` -> `OrderService.Create` -> publish to topic `tp-create-orders` -> consumed by `SimpleOrders.Notify/Worker.cs` (group `gp-notify`).
- Rabbit hello flow: `POST /r/hello` -> publish queue `general.first.contact` -> consumed by `SimpleOrders.DumbRabbit/HelloWorker.cs`.
- Rabbit order flow: `POST /r/order` publishes `create.orders` (exchange `orders`), `PUT /r/order/{id}` publishes `update.orders`; both consumed by `SimpleOrders.DumbRabbit/OrdersWorker.cs`.
- Rabbit consumers manually ack/nack (`BasicAckAsync` / `BasicNackAsync`) and route by `RoutingKey`.

## Config and Environment Rules
- Config objects are strongly bound via `IOptions` to sections: `Kafka`, `RabbitMQ`, `RavenDB` (`SimpleOrders.Shared/*.cs`).
- Canonical dev config examples:
  - API: `SimpleOrders.Api/appsettings.Development.json` (includes Raven + Kafka + Rabbit).
  - Kafka worker: `SimpleOrders.Notify/appsettings.Development.json`.
  - Rabbit worker: `SimpleOrders.DumbRabbit/appsettings.Development.json`.
- Docker overrides use double-underscore env names in `compose.yaml` (e.g., `Kafka__BootstrapServer`, `RavenDB__Urls__0`).

## Developer Workflow (Practical)
- Start full stack with compose from repo root (brokers + DB + API + workers) using `compose.yaml`.
- API OpenAPI/Scalar endpoints are enabled only in Development (`SimpleOrders.Api/Program.cs`).
- No test projects exist in the current solution; behavior validation is currently integration-style via API calls and worker logs.
- All projects target `net10.0` (see each `.csproj`).

## Project-Specific Conventions to Preserve
- Keep transport contract keys/topic names consistent with existing literals: `tp-create-orders`, `general.first.contact`, `create.orders`, `update.orders`, `orders`.
- Keep business-domain types in `SimpleOrders.Shared` (`Dtos`, `Entities`) and reuse from all apps rather than duplicating per project.
- `OrderService.Update` expects Raven id suffix and loads `orders/{id}`; keep route/persistence id conventions aligned.
- Current resilience approach is Polly-based retries + async background reconfiguration loops in shared services; extend this pattern instead of introducing parallel custom retry styles.
