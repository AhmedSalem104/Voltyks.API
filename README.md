# ⚡ EV Charging System - ASP.NET Core Web API

This is a full-featured backend system for managing Electric Vehicle (EV) charging services. Built with **ASP.NET Core Web API**, this project provides endpoints for user authentication, charging station management, reservations, payments, and more.

## 🚀 Features

- 🔐 Secure User Authentication with JWT & Refresh Tokens
- 📱 OTP Verification via SMS (with Twilio)
- 🧾 Role-based Authorization (Admin, User, etc.)
- 🗺️ Charging Station Management (location, availability, types)
- 📅 Booking and Reservation System
- 💳 Payment Integration (planned)
- 🛡️ Middleware for Logging, Error Handling, and CORS
- 📦 Redis Integration for Session/Token Management
- 📈 Admin Dashboard Support (via APIs)
- 📊 API Versioning & Documentation with Swagger

---

## 🧱 Tech Stack

| Layer        | Tech                              |
|--------------|-----------------------------------|
| Backend      | ASP.NET Core Web API (.NET 7/8)   |
| Auth         | JWT + Refresh Tokens + OTP        |
| Caching      | Redis                             |
| Database     | SQL Server (EF Core)              |
| API Docs     | Swagger / Swashbuckle             |
| Communication| Twilio API                        |

---

## 📂 Project Structure

