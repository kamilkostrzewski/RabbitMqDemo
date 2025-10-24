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
* **
