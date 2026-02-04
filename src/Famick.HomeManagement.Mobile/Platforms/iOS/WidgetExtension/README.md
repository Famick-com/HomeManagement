# QuickConsumeWidget - iOS Widget Extension

This directory contains the Swift/SwiftUI source files for the Quick Consume iOS widget.

## Overview

The widget displays:
- A barcode scanner icon for quick access
- Count of expired items (red badge)
- Count of items expiring soon (orange badge)

Tapping the widget opens the Famick Home app directly to the Quick Consume page.

## Prerequisites

- Xcode 15.0 or later
- iOS 17.0+ deployment target
- Apple Developer account with App Groups capability

## Creating the Xcode Project

Since the `.xcodeproj` file is not included in version control, you need to create it in Xcode:

### Option A: Create a New App Project with Widget Extension

1. Open Xcode
2. File > New > Project
3. Choose **iOS > App** template
4. Configure the app:
   - Product Name: `QuickConsumeWidgetHost`
   - Team: Your Apple Developer Team
   - Organization Identifier: `com.famick.homemanagement`
   - Interface: SwiftUI
   - Language: Swift
5. Save to this directory (`WidgetExtension/`)
6. With the project selected, go to **File > New > Target**
7. Choose **iOS > Widget Extension**
8. Configure the widget:
   - Product Name: `QuickConsumeWidgetExtension`
   - Bundle Identifier: `com.famick.homemanagement.QuickConsumeWidget`
   - Include Configuration App Intent: No
   - Include Live Activity: No
9. Delete the auto-generated Swift files in the widget target
10. Drag the files from `QuickConsumeWidget/` folder into the widget target in Xcode
11. Delete the host app target (we only need the widget extension)

### Option B: Simpler Alternative - Single Target Project

1. Open Xcode
2. File > New > Project
3. Choose **macOS > Command Line Tool** (as a minimal host)
4. Product Name: `QuickConsumeWidget`
5. Save to this directory
6. File > New > Target > **iOS > Widget Extension**
7. Configure as above and replace generated files with `QuickConsumeWidget/` contents

### After Project Creation

1. Select the widget extension target
2. Go to **Signing & Capabilities**
3. Add **App Groups** capability
4. Add group: `group.com.famick.homemanagement`
5. Ensure the `QuickConsumeWidget.entitlements` file is used

## App Group Configuration

The widget uses an App Group to share data with the main app:
- **App Group ID:** `group.com.famick.homemanagement`

This must be configured in:
1. The main app's Entitlements.plist (already done)
2. The widget extension's entitlements (QuickConsumeWidget.entitlements)
3. Apple Developer Portal for both app IDs

## Building

After creating the Xcode project:

```bash
chmod +x build.sh
./build.sh
```

This will build the widget for both device and simulator, placing outputs in `XReleases/`.

## Data Sharing

The main app writes widget data to the shared App Group container:

```swift
// Keys used by the widget
- "ExpiringItemCount" (Int): Number of expired items
- "DueSoonItemCount" (Int): Number of items expiring soon
```

The main app updates these values after:
- User login
- Dashboard load
- Consume action
- App backgrounding

## Files

| File | Description |
|------|-------------|
| `QuickConsumeWidget.swift` | Main widget definition and views |
| `QuickConsumeProvider.swift` | Timeline provider that reads shared data |
| `QuickConsumeEntry.swift` | Timeline entry model |
| `QuickConsumeWidget.entitlements` | App Group capability |
| `Info.plist` | Widget extension configuration |
| `build.sh` | Build script for both platforms |

## Supported Widget Sizes

- **Small**: Icon + title + badges
- **Medium**: Icon + title + detailed counts

## Deep Link

The widget opens the main app using: `famick://quick-consume`
