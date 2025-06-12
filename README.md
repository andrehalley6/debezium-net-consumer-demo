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
git clone https://github.com/your-username/debezium-consumer-demo.git
cd debezium-consumer-demo
```

### 2. Start services with Docker Compose

```bash
docker-compose -p net-docker-debezium -f docker/docker-debezium.yaml up -d
```

This will run all services in Docker

### 3. Create Table and Enable Logical Replication on PostgreSQL

#### Create table products and add row data

```bash
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255),
    price DECIMAL
);
INSERT INTO products (name, price) VALUES ('Product A', 100.00);
```

```bash
docker exec -it postgres-debezium bash
psql -U admin -d debezium-demo
```

#### Inside the PostgreSQL terminal, run:

```bash
ALTER SYSTEM SET wal_level = logical;
SELECT pg_reload_conf();
```

#### Restart PostgreSQL container
```bash
docker restart postgres-debezium
```

### 4. Register the Debezium Connector

```bash
curl -X POST http://localhost:8083/connectors \
  -H "Content-Type: application/json" \
  -d '{
    "name": "postgres-products-connector",
    "config": {
      "connector.class": "io.debezium.connector.postgresql.PostgresConnector",
      "database.hostname": "postgres-debezium",
      "database.port": "5432",
      "database.user": "admin",
      "database.password": "password",
      "database.dbname": "debezium-demo",
      "database.server.name": "pgdemo",
      "plugin.name": "pgoutput",
      "table.include.list": "public.products",
      "slot.name": "products_slot",
      "topic.prefix": "pgdemo"
    }
  }'
```

Once successful, you'll receive a JSON confirmation of the connector.

### 5. Verify Kafka Topics

```bash
docker exec kafka kafka-topics --bootstrap-server localhost:9092 --list
```

Search for:
```bash
pgdemo.public.products
```

### 6. Test .NET Consumer

In this step, you can try adding data, update data, or delete row in table products to check it's working

### 7. Cleanup

```bash
docker-compose down -v
```