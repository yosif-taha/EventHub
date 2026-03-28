---

## 🌟 Project Overview

EventHub is a robust, production-grade Web API designed to streamline event organization and attendance. From hosting large-scale conferences to small workshops, EventHub handles user roles (Admin, Organizer, Attendee), event lifecycles, real-time notifications, and secure payment processing.

---

## 🏗️ Architectural Excellence

The project is engineered using Clean Architecture principles to ensure high maintainability, testability, and separation of concerns:

Domain: Pure business logic and entities.

Application: CQRS patterns with MediatR for lean and scalable command/query handling.

Infrastructure: External integrations (Stripe, SendGrid, Identity).

Persistence: EF Core with SQL Server using a Code-First approach.

API: RESTful endpoints with JWT Authentication and Swagger documentation.

---

## 🛠️ Tech Stack

Framework: .NET 9.0 (ASP.NET Core Web API)

Database: SQL Server + Entity Framework Core

Security: ASP.NET Core Identity + JWT Bearer Tokens

Patterns: CQRS, Mediator, Repository 

Tools: AutoMapper, FluentValidation, MediatR

Integrations: Stripe/PayPal API (Payments), SendGrid (Email Notifications)

---

## 🚀 Key Features

✅ User Management: Role-based access control (RBAC).

✅ Event Engine: Create, manage, and browse events with category filtering.

✅ Registration System: Secure booking with capacity tracking.

✅ Payment Gateway: Seamless transaction handling for paid events.

✅ Real-time Notifications: Automated email updates and reminders.
