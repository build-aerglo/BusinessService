# 🏢 BusinessService

A modular **.NET 9 (C#)** microservice built using **Domain-Driven Design (DDD)** principles and **Dapper** for lightweight data access.  
The service manages **Businesses**, **Categories**, and their many-to-many relationships.

---

## 📚 Overview

The `BusinessService` system allows you to:
- Manage **categories** and subcategories (self-referencing hierarchy)
- Register **businesses** and their parent branches
- Associate businesses with one or more categories
- Compute and update business ratings (aggregate logic)

This service follows **DDD layering** with clear separation of concerns:

BusinessService/
│
├── BusinessService.Api/ → API Layer (Controllers, Swagger)
├── BusinessService.Application/ → Application Layer (DTOs, Services, Interfaces)
├── BusinessService.Domain/ → Domain Layer (Entities, Exceptions, Contracts)
├── BusinessService.Infrastructure/ → Infrastructure Layer (Dapper Repositories, Persistence)
└── tests/
├── Api.Tests/ → NUnit tests for controllers
├── Application.Tests/ → NUnit tests for business logic
└── Infrastructure.Tests/ → NUnit integration tests (Dapper + Postgres)



---

## 🏗️ Architecture Layout

### 🧩 Domain Layer
Contains **core business entities** and **rules**:
- `Business`
- `Category`
- Domain Exceptions (e.g., `BusinessAlreadyExistsException`, `CategoryNotFoundException`)

### ⚙️ Application Layer
Contains **use case logic** and **DTOs**:
- Services: `BusinessService`, `CategoryService`
- DTOs: `BusinessDto`, `CategoryDto`, `CreateBusinessRequest`, `CreateCategoryRequest`
- Interfaces: `IBusinessService`, `ICategoryService`

### 🧱 Infrastructure Layer
Handles **persistence** using **Dapper** and **PostgreSQL**:
- `DapperContext` for DB connections
- `BusinessRepository`, `CategoryRepository`
- Implements repository interfaces defined in Domain layer

### 🌐 API Layer
Contains **Controllers** and API routes:
- `/api/business`
- `/api/category`
- Uses standard controllers (not minimal APIs)
- Configured with **Swagger / OpenAPI**

### 🧪 Tests
All layers are unit/integration tested using **NUnit** + **Moq** + **FluentAssertions**.  
Infrastructure tests use **Testcontainers** to spin up real Postgres for end-to-end validation.

---

## 🧰 Tech Stack

| Component | Technology |
|------------|-------------|
| Framework | .NET 9 (C# 13) |
| Data Access | Dapper |
| Database | PostgreSQL |
| Architecture | Domain-Driven Design (DDD) |
| Testing | NUnit, Moq, FluentAssertions, Testcontainers |
| API Docs | Swagger / OpenAPI |

---

## 🗄️ Database Schema

Below is the PostgreSQL schema used by the service.

```sql
-- Category table
CREATE TABLE category (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    parent_category_id UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
    CONSTRAINT fk_parent_category FOREIGN KEY (parent_category_id)
        REFERENCES category (id) ON DELETE SET NULL
);

-- Business table
CREATE TABLE business (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL UNIQUE,
    parent_business_id UUID,
    is_branch BOOLEAN NOT NULL DEFAULT FALSE,
    website VARCHAR(255),
    avg_rating NUMERIC(3, 2) NOT NULL DEFAULT 0.00,
    review_count BIGINT NOT NULL DEFAULT 0,
    business_address TEXT,
    logo TEXT,
    opening_hours JSONB,
    business_email VARCHAR(255) UNIQUE,
    business_phone_number VARCHAR(20),
    cac_number VARCHAR(50),
    access_username VARCHAR(100) UNIQUE,
    access_number VARCHAR(50),
    social_media_links JSONB,
    business_description TEXT,
    sector VARCHAR(100),
    media TEXT[],
    is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    review_link TEXT,
    preferred_contact_method VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
    CONSTRAINT fk_parent_business FOREIGN KEY (parent_business_id)
        REFERENCES business (id) ON DELETE SET NULL
);

-- Join table (Many-to-Many)
CREATE TABLE business_category (
    business_id UUID NOT NULL,
    category_id UUID NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
    CONSTRAINT fk_business_category_business FOREIGN KEY (business_id)
        REFERENCES business (id) ON DELETE CASCADE,
    CONSTRAINT fk_business_category_category FOREIGN KEY (category_id)
        REFERENCES category (id) ON DELETE CASCADE,
    CONSTRAINT uk_business_category UNIQUE (business_id, category_id)
);



🌱 Sample Seed Data
-- Insert sample categories
INSERT INTO category (id, name) VALUES
('0199d4ef-ca22-7970-a8d2-579518a5030d', 'Finance'),
('0199d4ef-ca22-7970-a8d2-57965e57ebb3', 'Retail'),
('0199d4ef-ca22-7970-a8d2-57977439b317', 'Tourism');

-- Insert subcategories
INSERT INTO category (id, name, parent_category_id) VALUES
('0199d4ef-ca22-7970-a8d2-5798afd24081', 'Bank', '0199d4ef-ca22-7970-a8d2-579518a5030d'),
('0199d4ef-ca22-7970-a8d2-57992bb164a8', 'E-commerce', '0199d4ef-ca22-7970-a8d2-57965e57ebb3');

-- Insert businesses
INSERT INTO business (id, name, website) VALUES
('0199d4ef-ca22-7970-a8d2-57945c1f4673', 'Shoprite', 'https://shoprite.com'),
('0199d4ef-ca22-7970-a8d2-579a4e225266', 'Paga', 'https://paga.com'),
('0199d4ef-ca22-7970-a8d2-579b94abdc68', 'KFC', 'https://kfc.com');

-- Associate businesses with categories
INSERT INTO business_category (business_id, category_id) VALUES
('0199d4ef-ca22-7970-a8d2-57945c1f4673', '0199d4ef-ca22-7970-a8d2-57992bb164a8'), -- Shoprite: E-commerce
('0199d4ef-ca22-7970-a8d2-57945c1f4673', '0199d4ef-ca22-7970-a8d2-57977439b317'), -- Shoprite: Tourism
('0199d4ef-ca22-7970-a8d2-579a4e225266', '0199d4ef-ca22-7970-a8d2-579518a5030d'), -- Paga: Finance
('0199d4ef-ca22-7970-a8d2-579b94abdc68', '0199d4ef-ca22-7970-a8d2-57977439b317'); -- KFC: Tourism
