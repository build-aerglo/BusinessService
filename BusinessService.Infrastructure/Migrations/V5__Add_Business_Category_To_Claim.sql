-- V5: Add business_category column to business_claim_request table
ALTER TABLE business_claim_request
    ADD COLUMN IF NOT EXISTS business_category UUID;
