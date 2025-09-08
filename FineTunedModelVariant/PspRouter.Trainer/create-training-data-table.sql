-- Create TrainingData table for PspRouter fine-tuning
USE PspRouterTraining;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TrainingData' AND xtype='U')
BEGIN
    CREATE TABLE TrainingData (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SystemPrompt NVARCHAR(MAX) NOT NULL,
        UserInstruction NVARCHAR(MAX) NOT NULL,
        ContextJson NVARCHAR(MAX) NOT NULL,
        ExpectedResponse NVARCHAR(MAX) NOT NULL,
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
    INSERT INTO TrainingData (SystemPrompt, UserInstruction, ContextJson, ExpectedResponse, Source, IsValidated)
    VALUES 
    (
        'You are a payment service provider (PSP) routing assistant. Your job is to analyze transaction context and recommend the best PSP for processing.',
        'Route this transaction: Merchant in US, Buyer in UK, $100 USD, Visa card, 3DS required',
        '{"merchantCountry": "US", "buyerCountry": "UK", "currency": "USD", "amount": 100, "cardScheme": "Visa", "scaRequired": true, "riskScore": 5}',
        '{"recommendedPsp": "Stripe", "reasoning": "Stripe has excellent 3DS support and good rates for US-UK transactions", "alternatives": ["Adyen", "Square"]}',
        'Sample Data',
        1
    ),
    (
        'You are a payment service provider (PSP) routing assistant. Your job is to analyze transaction context and recommend the best PSP for processing.',
        'Route this transaction: Merchant in Germany, Buyer in France, €50 EUR, Mastercard, low risk',
        '{"merchantCountry": "DE", "buyerCountry": "FR", "currency": "EUR", "amount": 50, "cardScheme": "Mastercard", "scaRequired": false, "riskScore": 2}',
        '{"recommendedPsp": "Adyen", "reasoning": "Adyen has strong European presence and competitive rates for EUR transactions", "alternatives": ["Stripe", "Klarna"]}',
        'Sample Data',
        1
    );
    
    PRINT 'Sample training data inserted successfully';
END
ELSE
BEGIN
    PRINT 'Training data already exists';
END
GO
