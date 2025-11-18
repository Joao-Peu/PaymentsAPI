PaymentsAPI - Microservice responsible for processing payments.

Structure:
- Core: Entities, Events, ValueObjects
- Application: Consumers (MassTransit)
- Infrastructure: Repositories
- Controllers: API endpoints
- k8s: Kubernetes deployment and secret manifests

How to run:
- dotnet build
- Docker build -t paymentsapi:latest -f PaymentsAPI/Dockerfile .
- Deploy k8s manifests in `PaymentsAPI/k8s`.

Note: This is a simplified implementation for a tech challenge. Payments are simulated.