# App Store Launch Checklist

## iOS App Store Requirements

### App Information

- [ ] App name finalized and available
- [ ] App subtitle (30 characters max)
- [ ] App description (4000 characters max)
- [ ] Keywords for App Store search optimization
- [ ] Primary and secondary category selected
- [ ] Privacy policy URL hosted and accessible
- [ ] Support URL configured
- [ ] Marketing URL (optional)

### Visual Assets

- [ ] App icon (1024x1024, no alpha channel, no rounded corners)
- [ ] Screenshots for required device sizes:
  - [ ] iPhone 6.7" (1290x2796)
  - [ ] iPhone 6.5" (1284x2778)
  - [ ] iPad Pro 12.9" (2048x2732) - if universal app
- [ ] App preview videos (optional, 15-30 seconds)

### Build and Signing

- [ ] Production signing certificate configured
- [ ] App ID registered in Apple Developer Portal
- [ ] Provisioning profile for distribution
- [ ] Build uploaded via Xcode or CI/CD pipeline
- [ ] Build processing completed in App Store Connect
- [ ] Version number and build number set correctly

### App Review Preparation

- [ ] Demo account credentials for App Review team
- [ ] Review notes explaining app functionality
- [ ] Contact information for reviewer questions
- [ ] IDFA declaration (Advertising Identifier usage)
- [ ] App Tracking Transparency implementation (if tracking)
- [ ] Content rights documentation (if applicable)

### Privacy and Compliance

- [ ] App privacy nutrition labels completed
- [ ] Data collection disclosures accurate
- [ ] GDPR compliance verified
- [ ] Age rating questionnaire completed
- [ ] Export compliance documentation (encryption)
- [ ] App Tracking Transparency prompt (iOS 14.5+)

### Technical Requirements

- [ ] Minimum iOS version set appropriately
- [ ] Universal purchase configured (if applicable)
- [ ] In-App Purchase products created and approved
- [ ] Push notification entitlement configured
- [ ] Background modes declared (if used)
- [ ] App Transport Security exceptions documented

### Pre-Submission Testing

- [ ] TestFlight beta testing completed
- [ ] Crash-free rate > 99.5%
- [ ] All critical user flows verified
- [ ] Performance profiled (memory, CPU, battery)
- [ ] Network error handling verified
- [ ] Offline mode behavior verified
- [ ] Deep link handling tested

## Google Play Store Requirements

### Store Listing

- [ ] App title (50 characters max)
- [ ] Short description (80 characters max)
- [ ] Full description (4000 characters max)
- [ ] App category selected
- [ ] Contact email configured
- [ ] Privacy policy URL
- [ ] App access instructions for review

### Visual Assets

- [ ] App icon (512x512, 32-bit PNG)
- [ ] Feature graphic (1024x500)
- [ ] Screenshots (minimum 2, maximum 8 per device type):
  - [ ] Phone screenshots (16:9 or 9:16)
  - [ ] Tablet screenshots (if targeting tablets)
- [ ] Promotional video (YouTube URL, optional)

### Build and Signing

- [ ] App signing by Google Play configured (recommended)
- [ ] Upload key created and secured
- [ ] AAB (Android App Bundle) uploaded
- [ ] Target SDK version meets minimum requirements
- [ ] 64-bit support included
- [ ] Version code and version name set correctly

### Content Rating

- [ ] IARC content rating questionnaire completed
- [ ] Rating applied to all regions

### Privacy and Compliance

- [ ] Data safety section completed
- [ ] Data collection and sharing disclosures
- [ ] Data deletion request mechanism
- [ ] Permissions declared with justifications
- [ ] Families policy compliance (if applicable)

### Financial Features

- [ ] Google Play Billing Library integrated for subscriptions
- [ ] In-app products created in Play Console
- [ ] Subscription pricing configured for all markets
- [ ] Grace period and account hold configured
- [ ] Subscription offer details displayed correctly

### Pre-Submission Testing

- [ ] Internal testing track validated
- [ ] Closed testing with external testers completed
- [ ] Android vitals baseline established (ANR rate, crash rate)
- [ ] Crash-free rate > 99.5%
- [ ] All critical user flows verified
- [ ] ProGuard/R8 configuration verified
- [ ] Accessibility compliance checked

## Blue-Green Deployment

### Pre-Deployment

- [ ] Green environment provisioned and configured
- [ ] Database migrations applied to green environment
- [ ] Configuration and secrets verified in green environment
- [ ] Health checks passing on green environment
- [ ] Smoke tests passing on green environment

### Deployment

- [ ] Traffic routed to green environment
- [ ] Blue environment kept running as fallback
- [ ] Monitoring dashboards reviewed for anomalies
- [ ] Error rates compared between blue and green

### Post-Deployment

- [ ] Green environment stable for monitoring period (30 minutes)
- [ ] No increase in error rates or latency
- [ ] Blue environment decommissioned after stability period
- [ ] Deployment logged and documented

### Rollback

- [ ] Route traffic back to blue environment
- [ ] Investigate issues on green environment
- [ ] Restore database if migration rollback needed
- [ ] Post-mortem on failed deployment
