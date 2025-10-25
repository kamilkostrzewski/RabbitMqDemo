# RabbitMqDemo 🐇

A simple demo project for learning and demonstrating **RabbitMQ** within the .NET ecosystem. This project tracks the application's evolution from a simple console script to a robust, fault-tolerant service based on `IHost`, ready for containerization with Docker.

---

## 🎯 Project Goal

The main goal of this repository is to track the evolution of the application and its implementation of best practices:
1.  **From:** A simple script in `Program.cs` using `Console.ReadKey()`...
2.  **To:** A fully managed service (`IHostedService`) running on the .NET Generic Host (`IHost`).
3.  **From:** Hard-coded values (like `localhost`) directly in the code...
4.  **To:** Clean configuration loaded from `appsettings.json` using the **Options Pattern**.
5.  **From:** Sending simple `string` messages...
6.  **To:** Sending strongly-typed POCO objects (`record`) serialized as **JSON**.
7.  **From:** Naive error handling...
8.  **To:** Robust **Fault Isolation**, **Poison Message** handling (`BasicNackAsync`), and **Graceful Shutdown**.

---

## ✨ Current Features

* **Producer Application (`RabbitMq.Producer`):** A `BackgroundService` that publishes JSON messages to an exchange.
* **Consumer Application (`RabbitMq.Consumer`):** A `BackgroundService` that creates a temporary queue, binds it to an exchange, and listens for messages.
* **Options Pattern (`IOptions<T>`):** All configuration (host, exchange name) is injected into services from `appsettings.json`.
* **Publish/Subscribe Pattern:** The app uses a `Fanout` exchange, allowing multiple consumers to receive copies of the same message.
* **Shared Connection:** Connection logic is managed by a singleton `IRabbitMqConnectionProvider`, ensuring thread-safe, async creation and sharing of a single `IConnection`.
* **Shared Project (`RabbitMq.Shared`):** Contains the common "contracts" – the `RabbitMqSettings` class, the `MessagePayload` model, and the connection abstraction.
* **Robust Error Handling:**
    * **Graceful Shutdown:** Correct `CancellationToken` handling and `IDisposable` implementation in the Consumer prevents race conditions on shutdown.
    * **Poison Messages:** An inner `try-catch` block in the `ReceivedAsync` handler catches deserialization (`JsonException`) or processing errors. A failed message is rejected (`BasicNackAsync` with `requeue: false`), preventing an infinite error loop.

---

## 🚀 Project Evolution (Refactor Steps)

This project wasn't built in its current state all at once. It went through several key refactoring stages:

### 1. Prototype (Initial Code)
* **Description:** Two simple console applications (`Producer` and `Consumer`) communicating via a hard-coded queue name.
* **Characteristics:**
    * All logic was in the `Program.cs` files.
    * Values (hostname, queue name) were hard-coded.
    * No Dependency Injection (DI) and no robust error handling.
    * The main thread was blocked by `Console.ReadKey()` to keep the app alive.

### 2. Migrating to `IHost` and the Options Pattern
* **Description:** Rewriting the applications to follow modern .NET patterns.
* **Changes:**
    * Logic was moved into dedicated `MessageProducer` and `MessageConsumer` classes inheriting from `BackgroundService`.
    * The Generic Host (`IHost`) and Dependency Injection (DI) were introduced.
    * The **Options Pattern** (`IOptions<RabbitMqSettings>`) was used to inject configuration from `appsettings.json`.
    * `host.Run()` replaced `Console.ReadKey()`.

### 3. Hardening the Consumer
* **Description:** Implementing a "bulletproof" consumer.
* **Changes:**
    * A primary `try-catch(OperationCanceledException)` block was added to handle graceful shutdown (Ctrl+C).
    * Fixed a "race condition" on shutdown by moving `IConnection` and `IChannel` to class fields and implementing `IDisposable`.
    * An **inner `try-catch` block** was added to the `ReceivedAsync` handler to ensure a single bad message wouldn't crash the entire service.
    * Introduced `BasicNackAsync(requeue: false)` for proper rejection of poison messages.

### 4. JSON Serialization
* **Description:** Moving from simple strings to structured data.
* **Changes:**
    * Created the `MessagePayload` (`record` type) in the `Shared` project.
    * The `Producer` now serializes the `MessagePayload` object to JSON (`System.Text.Json`).
    * The `Consumer` now deserializes the JSON back into a `MessagePayload` object.

### 5. Publish/Subscribe Pattern (`Fanout`)
* **Description:** Changing the architecture from "Work Queue" to "Pub/Sub".
* **Changes:**
    * Instead of sending to a fixed queue, the Producer now sends messages to a `Fanout` Exchange.
    * The Consumer creates its own temporary, non-durable queue (`durable: false`, `exclusive: true`) and binds it (`QueueBind`) to the exchange.
    * This allows multiple consumers to run, each receiving a copy of every message.

### 6. Connection Refactor (Singleton Provider)
* **Description:** Centralizing the RabbitMQ connection logic and fixing concurrency issues.
* **Changes:**
    * Introduced `IRabbitMqConnectionProvider` and registered it as a `Singleton`.
    * The provider manages the lifecycle of a single, shared `IConnection` for the entire application.
    * The provider uses `SemaphoreSlim` for thread-safe, asynchronous initialization.
    * `Producer` and `Consumer` now inject the provider and create their own separate `IChannels`, fixing a critical thread-safety bug.

---

## 🛠️ How to Run

1.  **Run RabbitMQ**
    The easiest way is to use the official Docker image (with the management UI):
    ```bash
    docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
    ```
    The management panel will be available at `http://localhost:15672` (login: `guest`, password: `guest`).

2.  **Configure `appsettings.json`**
    Ensure the `appsettings.json` files in both `RabbitMq.Producer` and `RabbitMq.Consumer` contain the correct section (zauważ zmianę z `QueueName` na `ExchangeName`):
    ```json
    "RabbitMq": {
      "HostName": "localhost",
      "ExchangeName": "demo-fanout-exchange"
    }
    ```

3.  **Run the Applications (Visual Studio)**
    * Right-click the Solution (`Solution 'RabbitMqDemo'`) and select `Set Startup Projects...`.
    * Choose `Multiple startup projects`.
    * Set the `Action` to `Start` for both `RabbitMq.Consumer` and `RabbitMq.Producer`.
    * Press `F5` to run both applications simultaneously.

---

## 🗺️ Next Steps

This project will be developed further. Planned steps:

* [ ] **Implement the `Direct` Exchange Pattern (Routing)** to filter messages by a routing key (e.g., "info", "error").
* [ ] **Implement a Dead Letter Exchange (DLX)** to automatically route rejected (NACK-ed) messages to a separate "error" queue.
* [ ] **Add `Dockerfile`** for the `Producer` and `Consumer`.
* [ ] **Create a `docker-compose.yml`** file to launch the entire stack (Producer, Consumer, RabbitMQ) with a single command.
