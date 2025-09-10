# PSP Router - ML Enhanced Routing Postman Collection

This Postman collection demonstrates the complete ML-enhanced PSP routing system with realistic routing calls and feedback loops, similar to the integration test flow.

## 📋 Collection Overview

The collection contains **18 sequential API calls** that demonstrate:

1. **System Health Check** - Verify API is running
2. **PSP Discovery** - Get available PSPs with performance metrics
3. **Routing Scenarios** - 6 different transaction types with varying complexity
4. **Feedback Processing** - Process outcomes for each transaction
5. **ML Model Management** - Check status and trigger retraining
6. **Analytics & Monitoring** - View routing performance and system status

## 🚀 Getting Started

### Prerequisites

1. **PSP Router API** running on `https://localhost:7001`
2. **Database** configured with PSP and transaction data
3. **ML Models** trained and available
4. **Postman** application installed

### Setup Instructions

1. **Import Collection & Environment**:
   - Import `PspRouter-ML-Enhanced-Routing.postman_collection.json`
   - Import `PspRouter-Environment.postman_environment.json`

2. **Configure Environment**:
   - Update `base_url` if your API runs on a different port
   - Verify database connection settings

3. **Run Collection**:
   - Execute requests sequentially (1-18)
   - Or use Postman's "Run Collection" feature for automated execution

## 📊 Test Scenarios

### 1. Standard Card Payment (Calls 3-4)
- **Transaction**: Low-risk US domestic card payment
- **Expected**: Fast routing to high-performing PSP
- **Learning**: Successful transaction improves PSP metrics

### 2. High-Risk Payment (Calls 5-6)
- **Transaction**: High-risk international payment requiring SCA
- **Expected**: Routing to PSP with strong fraud protection
- **Learning**: Failed transaction provides negative feedback

### 3. Tokenized Payment (Calls 7-8)
- **Transaction**: Tokenized card payment in UK
- **Expected**: Routing to PSP with tokenization support
- **Learning**: Fast processing time improves PSP ranking

### 4. Alternative Payment Method (Calls 9-10)
- **Transaction**: Klarna payment in Germany
- **Expected**: Routing to PSP supporting alternative payments
- **Learning**: Successful alternative payment expands PSP capabilities

### 5. Cross-Border High Value (Calls 11-12)
- **Transaction**: High-value Japan-to-US payment
- **Expected**: Routing to PSP with international capabilities
- **Learning**: Complex transaction success improves PSP confidence

### 6. Final Learning Test (Calls 16-17)
- **Transaction**: Canadian payment to test ML learning
- **Expected**: Improved routing based on previous feedback
- **Learning**: Demonstrates continuous ML model improvement

## 🔄 Feedback Loop Demonstration

The collection demonstrates the complete ML feedback loop:

1. **Route Transaction** → Get PSP recommendation
2. **Process Feedback** → Update PSP performance metrics
3. **ML Learning** → Models learn from transaction outcomes
4. **Improved Routing** → Future transactions benefit from learning

## 📈 ML Model Features

### Automatic Learning
- **Success Rate Prediction**: Models learn from transaction outcomes
- **Processing Time Prediction**: Models optimize for speed
- **Health Status Prediction**: Models track PSP reliability

### Retraining Triggers
- **Feedback Threshold**: Retrain after sufficient new data
- **Time-based**: Retrain every 24 hours
- **Performance Degradation**: Retrain when accuracy drops

## 🛠️ Environment Variables

The collection uses dynamic variables for:

- **Decision IDs**: Automatically extracted from routing responses
- **Selected PSPs**: Captured for feedback processing
- **Timestamps**: Dynamic timestamps for realistic data
- **Currency Codes**: Standardized currency identifiers

## 📋 Request Flow

```
1. Health Check
2. Get Available PSPs
3. Route Transaction → 4. Process Feedback
5. Route Transaction → 6. Process Feedback
7. Route Transaction → 8. Process Feedback
9. Route Transaction → 10. Process Feedback
11. Route Transaction → 12. Process Feedback
13. Check ML Model Status
14. Trigger ML Model Retraining
15. Get Routing Analytics
16. Route Transaction → 17. Process Feedback
18. Get Final System Status
```

## 🔍 Monitoring & Analytics

### Key Metrics Tracked
- **Routing Accuracy**: Success rate of PSP selections
- **Processing Time**: Average transaction processing time
- **PSP Performance**: Individual PSP success rates and health
- **ML Model Status**: Model loading and retraining status

### Real-time Monitoring
- **Live PSP Health**: Real-time PSP status updates
- **ML Model Performance**: Model accuracy and confidence scores
- **System Load**: API performance and response times

## 🚨 Error Handling

The collection includes error handling for:
- **API Unavailability**: Health check failures
- **Database Connection**: Connection string issues
- **ML Model Loading**: Model initialization failures
- **Invalid Requests**: Malformed request data

## 📝 Customization

### Adding New Scenarios
1. Copy existing routing request
2. Modify transaction parameters
3. Add corresponding feedback request
4. Update environment variables

### Modifying Test Data
- Update currency codes in environment
- Modify payment method IDs
- Adjust risk scores and amounts
- Change country combinations

## 🔧 Troubleshooting

### Common Issues
1. **Connection Refused**: Check API is running on correct port
2. **Database Errors**: Verify connection string in .env file
3. **ML Model Errors**: Ensure models are trained and available
4. **Variable Not Set**: Check environment variable configuration

### Debug Tips
- Enable Postman console for detailed logging
- Check API logs for server-side errors
- Verify database connectivity
- Test individual requests before running full collection

## 📚 Related Documentation

- [Integration Tests](../PspRouter.Tests/IntegrationTests.cs)
- [API Documentation](./README.md)
- [ML Model Training](../PspRouter.Trainer/README.md)
- [Database Schema](../docs/database-schema.md)

---

**Note**: This collection is designed to work with the ML-enhanced PSP routing system. Ensure all components are properly configured before running the tests.
