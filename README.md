# Bank-kafka-implementation

POST /webhooks/transactions — Kafka-Based Event Processing Design
Overview
This system processes transaction webhooks using an event-driven architecture powered by Kafka. Instead of persisting transactions directly in the API layer, incoming events are published to a Kafka topic and processed asynchronously by a dedicated consumer service.
This design improves scalability, resilience, and fault isolation between ingestion and persistence layers.

Architecture Flow
Webhook/API
↓
Kafka Producer
↓
Kafka Topic: transactions
↓
Kafka Consumer (Background Service)
↓
PostgreSQL Database

Core Design Principles
1. Event-Driven Decoupling
The API layer does not directly interact with the database. Instead, it publishes a TransactionEvent to Kafka. This decouples request handling from processing and improves throughput under high load.

2. Idempotent Processing
Since Kafka guarantees at-least-once delivery, duplicates are expected.
Idempotency is enforced at the database level using:
Unique constraint on TransactionId
Pre-insert existence check in consumer
This ensures the system safely handles retries and duplicate events.

3. Derived Computation
Business logic is applied in the consumer:
Processing fee = Amount × 1.5%
Net amount = Amount − Fee
These values are persisted to avoid recalculation during reads or analytics.

4. Durable Event Storage
Each transaction stores:
Structured transaction fields
Derived values (fee, net amount)
Full raw payload 
This ensures auditability and replay capability.

Kafka Producer Responsibility
The producer publishes a lightweight event:
{
"TransactionId": "TXN-123"
}
Key is set to TransactionId to preserve ordering per transaction.

Kafka Consumer Responsibility
The consumer runs as a .NET Background Service and performs:
Message consumption from transactions topic
Deserialization into domain event
Idempotency check
Derived computation (fee + net amount)
Persistence to PostgreSQL
Offset commit after successful processing

Data Integrity Strategy
To ensure correctness under distributed conditions:
PostgreSQL Constraint
ALTER TABLE transactions
ADD CONSTRAINT TransactionId UNIQUE (external_reference);
Why this matters:
Even if:
Consumer restarts
Kafka replays messages
Network failures occur
The database remains the final guard against duplicates.

Failure Handling
If processing fails mid-flow:
Kafka does not commit the offset
Message is retried automatically
If persistent failure occurs → can be routed to a Dead Letter Topic (DLT)
This ensures no message loss.

Assumptions
ExternalReference is globally unique per transaction event
Fee structure is fixed at 2%
Kafka is configured for at-least-once delivery mode

Key Design Decisions
1. Kafka for Asynchronous Processing
Kafka was chosen to decouple ingestion from processing, allowing the system to handle spikes in transaction volume without blocking API requests.
2. Database as Source of Truth for Idempotency
Instead of relying on cache-based deduplication, PostgreSQL enforces uniqueness, ensuring strong consistency even in distributed failure scenarios.

Rejected Alternative
A synchronous webhook-to-database approach was rejected because it tightly couples API latency to database performance and does not scale well under high traffic or external dependency delays.

Failure Scenario
If Kafka delivers the same message twice due to consumer restart:
Both messages are processed
First insert succeed

