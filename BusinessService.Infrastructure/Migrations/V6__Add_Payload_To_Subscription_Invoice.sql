ALTER TABLE subscription_invoice
    ADD COLUMN IF NOT EXISTS payload JSONB;
