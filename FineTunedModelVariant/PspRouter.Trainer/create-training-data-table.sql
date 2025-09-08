-- Create TrainingData table for PspRouter fine-tuning
USE PspRouterTraining;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TrainingData' AND xtype='U')
BEGIN
    CREATE TABLE TrainingData (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TransactionId NVARCHAR(50) NOT NULL,
        SystemPrompt NVARCHAR(MAX) NOT NULL,
        UserInstruction NVARCHAR(MAX) NOT NULL,
        ContextJson NVARCHAR(MAX) NOT NULL,
        ExpectedResponse NVARCHAR(MAX) NOT NULL,
        ActualPspUsed NVARCHAR(100) NOT NULL,
        WasSuccessful BIT NOT NULL,
        TransactionAmount DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(3) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Source NVARCHAR(255) NULL,
        IsValidated BIT NOT NULL DEFAULT 0
    );
    
    -- Create index for better query performance
    CREATE INDEX IX_TrainingData_IsValidated_CreatedAt 
    ON TrainingData (IsValidated, CreatedAt DESC);
    
    PRINT 'TrainingData table created successfully';
END
ELSE
BEGIN
    PRINT 'TrainingData table already exists';
END
GO

-- Insert sample data for testing
IF NOT EXISTS (SELECT 1 FROM TrainingData)
BEGIN
    INSERT INTO TrainingData (TransactionId, SystemPrompt, UserInstruction, ContextJson, ExpectedResponse, ActualPspUsed, WasSuccessful, TransactionAmount, Currency, Source, IsValidated)
    VALUES 
    (
        'TXN-001',
        'You are a payment service provider (PSP) routing assistant. Your job is to analyze transaction context and recommend the best PSP for processing.',
        'Route this transaction: Merchant in US, Buyer in UK, $100 USD, Visa card, 3DS required',
        '{"merchantCountry": "US", "buyerCountry": "UK", "currency": "USD", "amount": 100, "cardScheme": "Visa", "scaRequired": true, "riskScore": 5, "merchantId": "MERCHANT_001"}',
        '{"recommendedPsp": "Stripe", "reasoning": "Stripe has excellent 3DS support and good rates for US-UK transactions", "alternatives": ["Adyen", "Square"]}',
        'Stripe',
        1,
        100.00,
        'USD',
        'Production Transaction',
        1
    ),
    (
        'TXN-002',
        'You are a payment service provider (PSP) routing assistant. Your job is to analyze transaction context and recommend the best PSP for processing.',
        'Route this transaction: Merchant in Germany, Buyer in France, €50 EUR, Mastercard, low risk',
        '{"merchantCountry": "DE", "buyerCountry": "FR", "currency": "EUR", "amount": 50, "cardScheme": "Mastercard", "scaRequired": false, "riskScore": 2, "merchantId": "MERCHANT_002"}',
        '{"recommendedPsp": "Adyen", "reasoning": "Adyen has strong European presence and competitive rates for EUR transactions", "alternatives": ["Stripe", "Klarna"]}',
        'Adyen',
        1,
        50.00,
        'EUR',
        'Production Transaction',
        1
    ),
    (
        'TXN-003',
        'You are a payment service provider (PSP) routing assistant. Your job is to analyze transaction context and recommend the best PSP for processing.',
        'Route this transaction: Merchant in UK, Buyer in US, £75 GBP, Amex card, high risk',
        '{"merchantCountry": "GB", "buyerCountry": "US", "currency": "GBP", "amount": 75, "cardScheme": "Amex", "scaRequired": true, "riskScore": 8, "merchantId": "MERCHANT_003"}',
        '{"recommendedPsp": "Square", "reasoning": "Square has good Amex support and handles high-risk transactions well", "alternatives": ["Stripe", "Adyen"]}',
        'Square',
        1,
        75.00,
        'GBP',
        'Production Transaction',
        1
    );
    
    PRINT 'Sample training data inserted successfully';
END
ELSE
BEGIN
    PRINT 'Training data already exists';
END
GO
