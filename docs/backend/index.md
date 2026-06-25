---
type: index
title: Backend Documentation Index
description: Index of backend system design, layers, and implementation patterns.
timestamp: 2026-06-24T07:23:16-03:00
tags: [backend, index]
---

# Backend Documentation Index

This directory contains specifications and documentation regarding the Galvão backend services and database.

*   [Backend Architecture Details](./architecture.md) - Explains layer structure (Domain, Application, Infrastructure, Presentation), custom CQRS patterns, Identity configuration, error handling details, and MySQL database configuration.
*   [Database Schema & Mappings](./database.md) - Detailed schema mapping and relationship configurations of the Galvão MySQL database (`type: database_table`).
*   [Critical API Endpoints](./endpoints.md) - Isolates and documents critical endpoints (Register, Update Preferences, Purge Account) (`type: api_endpoint`).
*   [Resend CRM Fallback Rules](./resend_fallback.md) - Details synchronization retry logic and inline fallbacks for downstream API failures (`type: business_rule`).
