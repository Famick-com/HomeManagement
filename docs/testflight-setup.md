# TestFlight Setup Guide

Prerequisites and configuration steps for deploying Famick Home to TestFlight.

## 1. Apple Developer Program

Verify your Apple Developer Program membership is active at [developer.apple.com](https://developer.apple.com).

- **Team ID**: `7A6WPZLCK9`

## 2. Register App IDs

Go to [Certificates, Identifiers & Profiles](https://developer.apple.com/account/resources/identifiers/list) and register two App IDs:

### Main App

- **Type**: App IDs
- **Bundle ID**: `com.famick.homemanagement` (Explicit)
- **Description**: Famick Home
- **Capabilities**:
  - Sign in with Apple
  - Associated Domains
  - App Groups: `group.com.famick.homemanagement`
  - Keychain Sharing

### Widget Extension

- **Type**: App IDs
- **Bundle ID**: `com.famick.homemanagement.QuickConsumeWidgetExtensionExtension` (Explicit)
- **Description**: Famick Home Widget
- **Capabilities**:
  - App Groups: `group.com.famick.homemanagement`

## 3. Distribution Certificate

Check for an existing Apple Distribution certificate at [Certificates](https://developer.apple.com/account/resources/certificates/list).

If none exists:

1. Open **Keychain Access** on your Mac
2. From the menu: Keychain Access > Certificate Assistant > Request a Certificate From a Certificate Authority
3. Enter your email, select "Saved to disk", click Continue
4. Upload the CSR at developer.apple.com > Certificates > Create > Apple Distribution
5. Download and double-click the `.cer` to install it

### Export .p12 for CI

1. Open **Keychain Access**
2. In the left sidebar under **Category**, select **My Certificates** (not "Certificates" — "My Certificates" only shows certs that have an associated private key)
3. Find "Apple Distribution: Mike Therien (7A6WPZLCK9)"
4. Right-click it and choose **Export "Apple Distribution: Mike Therien (7A6WPZLCK9)"...**
5. Choose **Personal Information Exchange (.p12)** as the format
6. Save and set a strong password when prompted
7. Base64-encode for GitHub secrets: `base64 -i certificate.p12 | pbcopy`

> **Note**: If the certificate doesn't appear under "My Certificates", the private key isn't in your keychain. You'll need to create a new certificate signing request (step above) and generate a new distribution certificate.

## 4. Provisioning Profiles

Create two **App Store** provisioning profiles at [Profiles](https://developer.apple.com/account/resources/profiles/list):

### Main App Profile

- **Type**: App Store
- **App ID**: `com.famick.homemanagement`
- **Certificate**: Your Apple Distribution certificate
- **Name**: `Famick Home AppStore`

### Widget Extension Profile

- **Type**: App Store
- **App ID**: `com.famick.homemanagement.QuickConsumeWidgetExtensionExtension`
- **Certificate**: Your Apple Distribution certificate
- **Name**: `Famick Widget AppStore`

Download both `.mobileprovision` files.   Double-click the files to import them into XCode.

For CI, base64-encode them:

```bash
base64 -i "Famick_Home_AppStore.mobileprovision" | pbcopy
# Paste into APPLE_APP_PROVISIONING_PROFILE secret

base64 -i "Famick_Widget_AppStore.mobileprovision" | pbcopy
# Paste into APPLE_WIDGET_PROVISIONING_PROFILE secret
```

## 5. App Store Connect

1. Go to [App Store Connect](https://appstoreconnect.apple.com) > My Apps > New App
2. Fill in:
   - **Platform**: iOS
   - **Name**: Famick Home
   - **Primary Language**: English (U.S.)
   - **Bundle ID**: `com.famick.homemanagement`
   - **SKU**: `famick-home`
3. Save the app record

## 6. App Store Connect API Key (for CI)

1. Go to [App Store Connect > Users and Access > Integrations > App Store Connect API](https://appstoreconnect.apple.com/access/integrations/api)
2. Click "Generate API Key"
3. **Name**: `Famick CI`
4. **Access**: App Manager
5. Download the `.p8` file (you can only download it once)
6. Note the **Key ID** and **Issuer ID** shown on the page

For local builds, place the `.p8` file at:
```
~/private_keys/AuthKey_{KEY_ID}.p8
```

For CI, paste the file contents into the `APP_STORE_CONNECT_PRIVATE_KEY` GitHub secret.

## 7. GitHub Secrets

Configure these secrets in the `HomeManagement` repository settings (Settings > Secrets and variables > Actions):

| Secret | Description | How to get it |
|--------|-------------|---------------|
| `APPLE_CERTIFICATE_P12` | Base64-encoded `.p12` distribution cert | `base64 -i cert.p12 \| pbcopy` |
| `APPLE_CERTIFICATE_PASSWORD` | Password for the `.p12` file | The password you set during export |
| `APPLE_APP_PROVISIONING_PROFILE` | Base64-encoded main app `.mobileprovision` | `base64 -i profile.mobileprovision \| pbcopy` |
| `APPLE_WIDGET_PROVISIONING_PROFILE` | Base64-encoded widget `.mobileprovision` | `base64 -i widget.mobileprovision \| pbcopy` |
| `APP_STORE_CONNECT_ISSUER_ID` | API key Issuer ID | App Store Connect > Integrations |
| `APP_STORE_CONNECT_KEY_ID` | API key Key ID | App Store Connect > Integrations |
| `APP_STORE_CONNECT_PRIVATE_KEY` | Contents of the `.p8` API key file | `cat AuthKey_XXXX.p8 \| pbcopy` |

## 8. Verification

After completing all steps, verify your setup:

```bash
# Check distribution certificate is installed
security find-identity -v -p codesigning | grep "Apple Distribution"

# Check provisioning profiles
ls ~/Library/MobileDevice/Provisioning\ Profiles/

# Test local build (no upload)
./scripts/build-testflight.sh --skip-upload
```

## 9. Add Internal Testers

1. Go to App Store Connect > Your App > TestFlight
2. Click "Internal Testing" > Create Group
3. Add testers by email
4. After the first build processes (~15-30 minutes), testers receive an email invite
5. Testers install via the TestFlight app on their iOS device
