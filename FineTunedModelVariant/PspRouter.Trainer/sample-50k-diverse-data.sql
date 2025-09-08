-- Sample 50,000 records with excellent diversity for training
-- This query ensures good distribution across PSPs, countries, and payment methods

WITH RankedTransactions AS (
    SELECT 
        pt.PaymentTransactionId,
        pt.OrderId,
        pt.Amount,
        pt.PaymentGatewayId,
        pt.PaymentMethodId,
        pt.CurrencyId,
        pt.CountryId,
        pt.PaymentCardBin,
        pt.ThreeDSTypeId,
        pt.IsTokenized,
        pt.PaymentTransactionStatusId,
        pt.GatewayStatusReason,
        pt.GatewayResponseCode,
        pt.IsReroutedFlag,
        pt.PaymentRoutingRuleId,
        pt.DateCreated,
        pt.DateStatusLastUpdated,
        pt.PspReference,
        -- Create diversity score for sampling
        ROW_NUMBER() OVER (
            PARTITION BY 
                pt.PaymentGatewayId, 
                pt.CountryId, 
                pt.PaymentMethodId,
                CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 'Success' ELSE 'Failure' END
            ORDER BY pt.DateCreated DESC
        ) as DiversityRank
    FROM PaymentTransactions pt
    WHERE pt.DateCreated >= DATEADD(MONTH, -6, GETDATE())  -- Last 6 months
        AND pt.PaymentGatewayId IS NOT NULL
        AND pt.PaymentTransactionStatusId IN (5, 7, 9, 11, 15, 17, 22)  -- Include both success and failure
        AND pt.CountryId IS NOT NULL  -- Ensure we have country data
        AND pt.PaymentMethodId IS NOT NULL  -- Ensure we have payment method data
),
DiverseSample AS (
    SELECT *
    FROM RankedTransactions
    WHERE DiversityRank <= 50  -- Take up to 50 records per PSP/Country/Method/Status combination
)
SELECT TOP 50000
    PaymentTransactionId,
    OrderId,
    Amount,
    PaymentGatewayId,
    PaymentMethodId,
    CurrencyId,
    CountryId,
    PaymentCardBin,
    ThreeDSTypeId,
    IsTokenized,
    PaymentTransactionStatusId,
    GatewayStatusReason,
    GatewayResponseCode,
    IsReroutedFlag,
    PaymentRoutingRuleId,
    DateCreated,
    DateStatusLastUpdated,
    PspReference
FROM DiverseSample
ORDER BY 
    NEWID()  -- Randomize the final selection
    -- Alternative: ORDER BY DateCreated DESC  -- If you prefer recent data
;

-- Verify the diversity of the sample
SELECT 
    'Sample Diversity Check' as Analysis,
    COUNT(*) as TotalRecords,
    COUNT(DISTINCT PaymentGatewayId) as UniquePSPs,
    COUNT(DISTINCT CountryId) as UniqueCountries,
    COUNT(DISTINCT PaymentMethodId) as UniquePaymentMethods,
    COUNT(DISTINCT CurrencyId) as UniqueCurrencies,
    SUM(CASE WHEN PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END) as SuccessfulTransactions,
    SUM(CASE WHEN PaymentTransactionStatusId IN (11, 15, 17, 22) THEN 1 ELSE 0 END) as FailedTransactions,
    SUM(CASE WHEN IsReroutedFlag = 1 THEN 1 ELSE 0 END) as ReroutedTransactions
FROM (
    -- Repeat the same query here to check diversity
    WITH RankedTransactions AS (
        SELECT 
            pt.PaymentTransactionId,
            pt.PaymentGatewayId,
            pt.CountryId,
            pt.PaymentMethodId,
            pt.CurrencyId,
            pt.PaymentTransactionStatusId,
            pt.IsReroutedFlag,
            ROW_NUMBER() OVER (
                PARTITION BY 
                    pt.PaymentGatewayId, 
                    pt.CountryId, 
                    pt.PaymentMethodId,
                    CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 'Success' ELSE 'Failure' END
                ORDER BY pt.DateCreated DESC
            ) as DiversityRank
        FROM PaymentTransactions pt
        WHERE pt.DateCreated >= DATEADD(MONTH, -6, GETDATE())
            AND pt.PaymentGatewayId IS NOT NULL
            AND pt.PaymentTransactionStatusId IN (5, 7, 9, 11, 15, 17, 22)
            AND pt.CountryId IS NOT NULL
            AND pt.PaymentMethodId IS NOT NULL
    ),
    DiverseSample AS (
        SELECT *
        FROM RankedTransactions
        WHERE DiversityRank <= 50
    )
    SELECT TOP 50000 *
    FROM DiverseSample
    ORDER BY NEWID()
) as SampleData;
