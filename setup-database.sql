-- PSP Router Database Setup Script
-- Run this script in your PostgreSQL database to set up the required schema

-- Create the database (run this as superuser if the database doesn't exist)
-- CREATE DATABASE psp_router;

-- Connect to the psp_router database and run the following:

-- Enable the vector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Create the lessons table for vector memory
CREATE TABLE IF NOT EXISTS psp_lessons (
    key TEXT PRIMARY KEY,
    content TEXT NOT NULL,
    meta JSONB NOT NULL,
    embedding vector(3072)
);

-- Create index for vector similarity search
CREATE INDEX IF NOT EXISTS psp_lessons_ivfflat 
ON psp_lessons USING ivfflat (embedding vector_cosine_ops) 
WITH (lists = 100);

-- Create a table for storing bandit statistics (optional, for persistence)
CREATE TABLE IF NOT EXISTS bandit_stats (
    segment_key TEXT NOT NULL,
    arm_name TEXT NOT NULL,
    alpha REAL DEFAULT 1.0,
    beta REAL DEFAULT 1.0,
    sum_rewards REAL DEFAULT 0.0,
    count_pulls INTEGER DEFAULT 0,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (segment_key, arm_name)
);

-- Create a table for transaction outcomes (for analytics)
CREATE TABLE IF NOT EXISTS transaction_outcomes (
    decision_id TEXT PRIMARY KEY,
    psp_name TEXT NOT NULL,
    authorized BOOLEAN NOT NULL,
    transaction_amount DECIMAL(10,2) NOT NULL,
    fee_amount DECIMAL(10,2) NOT NULL,
    processing_time_ms INTEGER NOT NULL,
    risk_score INTEGER NOT NULL,
    processed_at TIMESTAMP NOT NULL,
    error_code TEXT,
    error_message TEXT,
    merchant_id TEXT,
    currency TEXT,
    payment_method TEXT
);

-- Create indexes for analytics queries
CREATE INDEX IF NOT EXISTS idx_transaction_outcomes_psp ON transaction_outcomes(psp_name);
CREATE INDEX IF NOT EXISTS idx_transaction_outcomes_merchant ON transaction_outcomes(merchant_id);
CREATE INDEX IF NOT EXISTS idx_transaction_outcomes_processed_at ON transaction_outcomes(processed_at);

-- Insert some sample data for testing
INSERT INTO psp_lessons (key, content, meta, embedding) VALUES 
('sample_1', 'USD Visa transactions work well with Adyen for low-risk merchants', 
 '{"psp": "Adyen", "currency": "USD", "scheme": "Visa"}', 
 array_fill(0.1, ARRAY[3072])::vector),
('sample_2', 'High-risk transactions may need 3DS with Stripe for better auth rates', 
 '{"psp": "Stripe", "risk": "high", "sca": "required"}', 
 array_fill(0.2, ARRAY[3072])::vector)
ON CONFLICT (key) DO NOTHING;

-- Grant permissions (adjust as needed for your setup)
-- GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO your_app_user;
-- GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO your_app_user;

COMMENT ON TABLE psp_lessons IS 'Vector memory for storing routing lessons and insights';
COMMENT ON TABLE bandit_stats IS 'Multi-armed bandit learning statistics';
COMMENT ON TABLE transaction_outcomes IS 'Transaction outcomes for analytics and learning';
