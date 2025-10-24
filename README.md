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

* **Producer Application (`RabbitMq.Producer`):** A `BackgroundService` that continuously publishes new JSON messages.
* **Consumer Application (`RabbitMq.Consumer`):** A `BackgroundService` that listens for, deserializes, and processes messages.
* **Options Pattern (`IOptions<T>`):** All configuration (host, queue name) is injected into services from `appsettings.json`.
* **Shared Project (`RabbitMq.Shared`):** Contains the common "contracts" – the `RabbitMqSettings` class and the `MessagePayload` model.
* **Robust Error Handling:**
    * **Graceful Shutdown:** Correct `CancellationToken` handling and `IDisposable` implementation in the Consumer prevents race conditions on shutdown.
    * **Poison Messages:** An inner `try-catch` block in the `ReceivedAsync` handler catches deserialization (`JsonException`) or processing errors. A failed message is rejected (`BasicNackAsync` with `requeue: false`), preventing an infinite error loop.

---

## 🚀 Project Evolution (Refactor Steps)

This project wasn't built in its current state all at once. It went through several key refactoring stages based on the commit history:

### 1. Prototype (Initial Code)
* **Description:** Two simple console applications (`Producer` and `Consumer`).
* **Characteristics:**
    * All logic was in the `Program.cs` files.
    * Values (hostname, queue name) were hard-coded.
    * No Dependency Injection (DI) and no robust error handling.
    * The main thread was blocked by `Console.ReadKey()` to keep the app alive.

### 2. Migrating to `IHost` and the Options Pattern (Commit: `Add dedicated classes...`)
* **Description:** Rewriting the applications to follow modern .NET patterns.
* **Changes:**
    * Logic was moved into dedicated `MessageProducer` and `MessageConsumer` classes inheriting from `BackgroundService`.
    * The Generic Host (`IHost`) and Dependency Injection (DI) were introduced.
    * Hard-coded values were replaced with `appsettings.json` files.
    * The **Options Pattern** (`IOptions<RabbitMqSettings>`) was used to inject configuration.
    * `host.Run()` replaced `Console.ReadKey()` as the mechanism to keep the application running.

### 3. Hardening the Consumer (Commit: `Add Consumer logic nad refactor`)
* **Description:** Implementing a "bulletproof" consumer.
* **Changes:**
    * A primary `try-catch(OperationCanceledException)` block was added to handle graceful shutdown (Ctrl+C).
    * Fixed a "race condition" on shutdown by moving `IConnection` and `IChannel` to class fields and implementing `IDisposable`.
    * An **inner `try-catch` block** was added to the `ReceivedAsync` handler to ensure a single bad message wouldn't crash the entire service.
    * Introduced `BasicNackAsync(requeue: false)` for proper rejection of poison messages.

### 4. JSON Serialization (Commit: `Add MessagePayload record...`)
* **Description:** Moving from simple strings to structured data.
* **Changes:**
    * Created the `MessagePayload` (`record` type) in the `Shared` project.
    * The `Producer` now serializes the `MessagePayload` object to JSON (`System.Text.Json`).
    * The `Consumer` now deserializes the JSON back into a `MessagePayload` object.
    * The consumer's inner `try-catch` was expanded to specifically handle `JsonException`.

---

## 🛠️ How to Run

1.  **Run RabbitMQ**
    The easiest way is to use the official Docker image (with the management UI):
    ```bash
    docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
    ```
    The management panel will be available at `http://localhost:15672` (login: `guest`, password: `guest`).

2.  **Configure `appsettings.json`**
    Ensure the `appsettings.json` files in both `RabbitMq.Producer` and `RabbitMq.Consumer` contain the correct section:
    ```json
    "RabbitMq": {
      "HostName": "localhost",
      "QueueName": "Message"
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

* [ ] **Implement Explicit Exchanges** to move from the default exchange to `Fanout` and `Direct` patterns.
* [ ] **Implement a Dead Letter Exchange (DLX)** to automatically route rejected (NACK-ed) messages to a separate "error" queue.
* [ ] **Add `Dockerfile`** for the `Producer` and `Consumer`.
* [ ] **Create a `docker-compose.yml`** file to launch the entire stack (Producer, Consumer, RabbitMQ) with a single command.
