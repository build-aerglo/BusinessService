ALTER TABLE business
    ADD COLUMN business_address TEXT,
    ADD COLUMN logo TEXT,
    ADD COLUMN opening_hours JSONB,
    ADD COLUMN business_email VARCHAR(255),
    ADD COLUMN business_phone_number VARCHAR(20),
    ADD COLUMN cac_number VARCHAR(50),
    ADD COLUMN access_username VARCHAR(100),
    ADD COLUMN access_number VARCHAR(50),
    ADD COLUMN social_media_links JSONB,
    ADD COLUMN business_description TEXT,
    ADD COLUMN media TEXT[],
    ADD COLUMN is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN review_link TEXT,
    ADD COLUMN preferred_contact_method VARCHAR(50);

ALTER TABLE business
    ADD CONSTRAINT uk_business_email UNIQUE (business_email),
    ADD CONSTRAINT uk_access_username UNIQUE (access_username);
