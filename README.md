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
                          created_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
                          updated_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
                          CONSTRAINT fk_parent_business FOREIGN KEY (parent_business_id)
                              REFERENCES business (id) ON DELETE SET NULL
);

-- Join table
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


INSERT INTO businesssvc.category (id, name) VALUES
                                                ('0199d4ef-ca22-7970-a8d2-579518a5030d', 'Finance'),
                                                ('0199d4ef-ca22-7970-a8d2-57965e57ebb3', 'Retail'),
                                                ('0199d4ef-ca22-7970-a8d2-57977439b317', 'Tourism');

INSERT INTO businesssvc.category (id, name, parent_category_id) VALUES
                                                                    ('0199d4ef-ca22-7970-a8d2-5798afd24081', 'Bank', '0199d4ef-ca22-7970-a8d2-579518a5030d'),
                                                                    ('0199d4ef-ca22-7970-a8d2-57992bb164a8', 'E-commerce', '0199d4ef-ca22-7970-a8d2-57965e57ebb3');

INSERT INTO businesssvc.business (id, name, website) VALUES
                                                         ('0199d4ef-ca22-7970-a8d2-57945c1f4673', 'Shoprite', 'https://shoprite.com'),
                                                         ('0199d4ef-ca22-7970-a8d2-579a4e225266', 'Paga', 'https://paga.com'),
                                                         ('0199d4ef-ca22-7970-a8d2-579b94abdc68', 'KFC', 'https://kfc.com');

INSERT INTO businesssvc.business_category (business_id, category_id) VALUES
-- Shoprite falls under 2 categories E-commerce & Tourism
('0199d4ef-ca22-7970-a8d2-57945c1f4673', '0199d4ef-ca22-7970-a8d2-57992bb164a8'),
('0199d4ef-ca22-7970-a8d2-57945c1f4673', '0199d4ef-ca22-7970-a8d2-57977439b317'),
('0199d4ef-ca22-7970-a8d2-579a4e225266', '0199d4ef-ca22-7970-a8d2-579518a5030d'),
('0199d4ef-ca22-7970-a8d2-579b94abdc68', '0199d4ef-ca22-7970-a8d2-57977439b317');
