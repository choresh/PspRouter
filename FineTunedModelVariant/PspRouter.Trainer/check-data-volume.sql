-- Check training data volume and diversity
-- Run this query to assess your current data quality

-- 1. Total transaction count (last 6 months)
SELECT 
    COUNT(*) as TotalTransactions,
    COUNT(DISTINCT PaymentGatewayId) as UniquePSPs,
    COUNT(DISTINCT CountryId) as UniqueCountries,
    COUNT(DISTINCT PaymentMethodId) as UniquePaymentMethods,
    COUNT(DISTINCT CurrencyId) as UniqueCurrencies
FROM PaymentTransactions 
WHERE DateCreated >= DATEADD(MONTH, -6, GETDATE())
    AND PaymentGatewayId IS NOT NULL;

-- 2. Success vs Failure breakdown
SELECT 
    PaymentTransactionStatusId,
    COUNT(*) as TransactionCount,
    CASE 
        WHEN PaymentTransactionStatusId IN (5, 7, 9) THEN 'Success'
        WHEN PaymentTransactionStatusId IN (11, 15, 17, 22) THEN 'Failure'
        ELSE 'Other'
    END as Category
FROM PaymentTransactions 
WHERE DateCreated >= DATEADD(MONTH, -6, GETDATE())
    AND PaymentGatewayId IS NOT NULL
GROUP BY PaymentTransactionStatusId
ORDER BY TransactionCount DESC;

-- 3. PSP distribution
SELECT 
    PaymentGatewayId,
    COUNT(*) as TransactionCount,
    SUM(CASE WHEN PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END) as SuccessfulTransactions,
    CAST(SUM(CASE WHEN PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) as SuccessRate
FROM PaymentTransactions 
WHERE DateCreated >= DATEADD(MONTH, -6, GETDATE())
    AND PaymentGatewayId IS NOT NULL
GROUP BY PaymentGatewayId
ORDER BY TransactionCount DESC;

-- 4. Geographic distribution
SELECT 
    CountryId,
    COUNT(*) as TransactionCount,
    COUNT(DISTINCT PaymentGatewayId) as PSPsUsed
FROM PaymentTransactions 
WHERE DateCreated >= DATEADD(MONTH, -6, GETDATE())
    AND PaymentGatewayId IS NOT NULL
    AND CountryId IS NOT NULL
GROUP BY CountryId
ORDER BY TransactionCount DESC;

-- 5. Rerouting analysis
SELECT 
    IsReroutedFlag,
    COUNT(*) as TransactionCount,
    COUNT(DISTINCT PaymentGatewayId) as UniquePSPs
FROM PaymentTransactions 
WHERE DateCreated >= DATEADD(MONTH, -6, GETDATE())
    AND PaymentGatewayId IS NOT NULL
GROUP BY IsReroutedFlag;
