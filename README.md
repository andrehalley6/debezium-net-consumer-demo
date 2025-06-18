# Debezium Consumer Demo (.NET + PostgreSQL + Kafka)

This project demonstrates how to consume PostgreSQL change events using **Debezium**, **Kafka**, and a **.NET 8 consumer**.

---

## 🧱 Tech Stack

- .NET 8 Console App (Confluent.Kafka)
- Kafka (via Apache Kafka)
- Debezium PostgreSQL Connector
- PostgreSQL (with logical replication enabled)
- Docker & Docker Compose

---

## 🛠️ Prerequisites

- [Docker](https://www.docker.com/)
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)

---

## 🚀 Quick Start

### 1. Clone the repository

```bash
git clone https://github.com/andrehalley6/debezium-net-consumer-demo.git
cd debezium-net-consumer-demo
```

### 2. Start services with Docker Compose

#### Run in background

```bash
docker-compose -p debezium-net-demo up -d
```

#### Run in terminal

```bash
docker-compose -p debezium-net-demo up --build
```

This will run all services in Docker

### 3. Register the Debezium Connector

```bash
curl -X POST http://localhost:8083/connectors \
  -H "Content-Type: application/json" \
  -d '{
    "name": "postgres-products-connector",
    "config": {
      "connector.class": "io.debezium.connector.postgresql.PostgresConnector",
      "database.hostname": "postgres",
      "database.port": "5432",
      "database.user": "admin",
      "database.password": "password",
      "database.dbname": "inventory",
      "database.server.name": "pgdemo",
      "plugin.name": "pgoutput",
      "table.include.list": "public.products",
      "slot.name": "products_slot",
      "topic.prefix": "pgdemo"
    }
  }'
```

Once successful, you'll receive a JSON confirmation of the connector.

### 4. Verify Kafka Topics

```bash
docker exec kafka kafka-topics --bootstrap-server localhost:9092 --list
```

Search for:
```bash
pgdemo.public.products
```

### 5. Test .NET Consumer

In this step, make sure your .NET consumer is running,then you can try adding data, update data, or delete row in table products to check it's working.

If your .NET consumer is not working please restart/start it

```bash
docker-compose -p debezium-net-demo -f up -d consumer
```

### 7. Cleanup

```bash
docker-compose down -v
```