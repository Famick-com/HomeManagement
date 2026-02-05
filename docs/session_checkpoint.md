# Session Checkpoint - iOS Lock Screen Widget

## Date
2026-02-04

## Status
Implementation complete. All builds passing.

## What Was Built
Interactive iOS Lock Screen widget support (accessoryRectangular + accessoryCircular) with an interactive consume button that calls the FEFO quick-consume API directly from the Lock Screen, without opening the app.

## Architecture Decisions

### Keychain Access Groups for JWT Sharing
- Widget extension runs in separate process, can't access MAUI SecureStorage
- Solution: Shared keychain group `com.famick.homemanagement.shared`
- `SecAccessible.AfterFirstUnlock` so widget works while device is locked
- C# SharedKeychainService writes tokens; Swift KeychainHelper reads them

### Product Data via App Group UserDefaults
- Top 5 expiring products serialized as JSON to `WidgetExpiringProducts` key
- Expired items first, then due-soon, sorted by nextDueDate
- Backward compatible - existing integer keys (ExpiringItemCount, DueSoonItemCount) preserved

### Interactive Consume via AppIntent (iOS 17+)
- ConsumeNextExpiringIntent calls POST /api/v1/stock/quick-consume
- Amount = 1 per tap (safe default for Lock Screen)
- Optimistic local update for instant UI feedback
- Silent failure on network/auth errors (stale data until app refreshes)
- No token refresh from widget (v1) - relies on app keeping tokens fresh

### Lock Screen Widget Families
- **accessoryRectangular** (~155x58pt): Product name + expiry info + interactive consume button
- **accessoryCircular** (~50x50pt): Urgent item count badge, tap opens app

## Files Changed (15 files)

### New Files
- `Platforms/iOS/SharedKeychainService.cs` - C# keychain writer
- `WidgetExtension/.../WidgetProductItem.swift` - Codable product model
- `WidgetExtension/.../KeychainHelper.swift` - Swift keychain reader
- `WidgetExtension/.../ConsumeIntent.swift` - AppIntent for consume action

### Modified Files
- `QuickConsumeWidgetExtensionExtension.entitlements` - Fix typo + keychain-access-groups
- `QuickConsumeWidget.entitlements` - keychain-access-groups
- `Platforms/iOS/Entitlements.plist` - keychain-access-groups
- `Services/TokenStorage.cs` - Dual-write to shared keychain
- `App.xaml.cs` - TokenStorage constructor fix
- `Platforms/iOS/WidgetDataService.cs` - Product data sharing method
- `Models/ApiModels.cs` - StockOverviewItemDto
- `Services/ShoppingApiClient.cs` - Fetch expiring products for widget
- `WidgetExtension/.../QuickConsumeEntry.swift` - Added nextExpiringProduct
- `WidgetExtension/.../QuickConsumeProvider.swift` - Parse product JSON
- `WidgetExtension/.../QuickConsumeWidget.swift` - Lock Screen UI + interactive button
- `QuickConsumeWidget.xcodeproj/project.pbxproj` - Added new Swift files to Xcode project

## Commits
- `53d6840` - feat(mobile): add interactive iOS Lock Screen widget with quick-consume
- `1776932` - build: add new Swift files to Xcode widget extension project

## Next Steps
1. Manual test on device/simulator (login, verify keychain, add Lock Screen widget, test consume)
2. Test edge cases: no stock, all fresh, network unavailable, expired token, rapid taps
3. Consider adding token refresh from widget in future version
4. Team ID in KeychainHelper.swift may need hardcoding for production builds
