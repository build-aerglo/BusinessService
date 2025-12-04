ALTER TABLE business
    ADD COLUMN highlights TEXT[],
    ADD COLUMN tags TEXT[],
    ADD COLUMN average_response_time VARCHAR(50),
    ADD COLUMN profile_clicks BIGINT NOT NULL DEFAULT 0,
    ADD COLUMN faqs JSONB;
