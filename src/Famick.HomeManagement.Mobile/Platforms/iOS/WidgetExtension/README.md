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

1. Open Xcode
2. File > New > Project
3. Choose "Widget Extension" template
4. Configure:
   - Product Name: `QuickConsumeWidgetExtension`
   - Team: Your Apple Developer Team
   - Bundle Identifier: `com.famick.homemanagement.QuickConsumeWidget`
   - Include Configuration App Intent: No
   - Include Live Activity: No
5. Save to this directory (`WidgetExtension/`)
6. Replace the generated Swift files with the ones in `QuickConsumeWidget/`

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
