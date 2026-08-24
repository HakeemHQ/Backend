# Hakeem Backend

Backend API for **Hakeem**, a patient-owned medical history platform that transforms medical documents into structured, traceable medical records and Medical CVs.

## Architecture

Hakeem is implemented as an **ASP.NET Core modular monolith**. The backend is the only trusted gateway between clients and all data stores and external services.

```text
Patient / Doctor / Admin Clients
              │
              ▼
      ASP.NET Core Web API
              │
    ┌─────────┼──────────┐
    │         │          │
    ▼         ▼          ▼
 SQL Server  Document     Qdrant
 EF Core    Storage    Vector DB
    │
    ├── Identity & Access
    ├── Patient Profiles
    ├── Medical Documents
    ├── Extraction & Review
    ├── Medical Records
    ├── Medical CV
    ├── RAG & AI Questions
    └── Administration & Audit
              │
       ┌──────┴───────┐
       ▼              ▼
 OCR / Extraction   AI / LLM
 Services           Services
