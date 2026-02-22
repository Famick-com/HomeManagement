# Google Play Store Setup Guide

Prerequisites and configuration steps for deploying Famick Home to Google Play Internal Testing.

## 1. Google Play Developer Account

Create a Google Play Developer account at [play.google.com/console](https://play.google.com/console).

- **One-time fee**: $25
- **Account type**: Individual or Organization
- Use the same Google account you want to manage the app with

## 2. Create App Listing

In Google Play Console:

1. Click **Create app**
2. Fill in:
   - **App name**: Famick Home
   - **Default language**: English (United States)
   - **App or game**: App
   - **Free or paid**: Free
3. Accept the declarations and click **Create app**

You'll need to complete the initial setup checklist (privacy policy, app access, content rating, target audience) before you can publish to any track. Internal testing has minimal requirements — you can fill in placeholders initially.

## 3. Generate Upload Keystore

The upload keystore signs your AAB before uploading to Google Play. Google re-signs with their own key for distribution (App Signing by Google Play).

Generate a keystore (one-time, **keep this file secure**):

```bash
keytool -genkey -v \
  -keystore famick-upload.keystore \
  -alias famick \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000
```

When prompted:
- **Keystore password**: Choose a strong password
- **Key password**: Can be the same as keystore password
- **First and last name**: Mike Therien (or your name)
- **Organizational unit**: (optional)
- **Organization**: Famick
- **City/State/Country**: Your info

Store the keystore file somewhere safe (NOT in the git repo — it's in `.gitignore`). Back it up securely.

## 4. Enable App Signing by Google Play

In Google Play Console:

1. Go to your app > **Setup** > **App signing**
2. App Signing by Google Play should be enabled by default for new apps
3. This means Google manages the distribution key — you only need the upload key (your keystore)

## 5. Service Account for CI Uploads

To automate uploads from GitHub Actions, create a Google Cloud service account:

### Create Service Account

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a new project (or use existing): e.g., "Famick Play Store CI"
3. Go to **IAM & Admin** > **Service Accounts**
4. Click **Create Service Account**
   - **Name**: `famick-play-store-ci`
   - **Description**: CI/CD uploads to Play Store
5. Skip the optional permissions steps
6. Click **Done**
7. Click on the newly created service account
8. Go to **Keys** tab > **Add Key** > **Create new key** > **JSON**
9. Download the JSON key file (keep secure, do NOT commit)

### Link to Play Console

1. Go to [Google Play Console](https://play.google.com/console) > **Setup** > **API access**
2. If prompted, link to the Google Cloud project you created above
3. Find your service account in the list and click **Manage permissions**
4. Grant the **Release manager** role (or at minimum: **Manage production releases** and **Manage testing track releases**)
5. Set app-level permissions: Select **Famick Home** (or apply to all apps)
6. Click **Invite user** / **Save**

## 6. GitHub Secrets

Configure these secrets in the `HomeManagement` repository (Settings > Secrets and variables > Actions):

| Secret | Description | How to get it |
|--------|-------------|---------------|
| `ANDROID_KEYSTORE` | Base64-encoded upload keystore | `base64 -i famick-upload.keystore \| pbcopy` |
| `ANDROID_KEYSTORE_PASSWORD` | Keystore password | The password you set during generation |
| `ANDROID_KEY_ALIAS` | Key alias | `famick` (or whatever you chose) |
| `ANDROID_KEY_PASSWORD` | Key password | The password you set during generation |
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` | Full contents of the service account JSON | `cat service-account.json \| pbcopy` |

## 7. Local Build Setup

1. Copy `scripts/.env.example` to `scripts/.env` (if not already done)
2. Fill in the Android variables:
   ```bash
   ANDROID_KEYSTORE_PATH="/path/to/famick-upload.keystore"
   ANDROID_KEY_ALIAS="famick"
   ANDROID_KEYSTORE_PASSWORD="your-keystore-password"
   ANDROID_KEY_PASSWORD="your-key-password"
   ```
3. Ensure Java 17 is installed: `brew install openjdk@17`
4. Ensure MAUI Android workload is installed: `dotnet workload install maui-android`

## 8. Set Up Internal Testing Track

1. Go to Google Play Console > Your App > **Testing** > **Internal testing**
2. Click **Create new release** (you'll need to upload your first AAB here)
3. Go to the **Testers** tab
4. Click **Create email list**
5. Add tester email addresses (must be Google accounts)
6. Testers will receive an opt-in link — they accept and can then install via the Play Store

Internal testing characteristics:
- Up to **100 testers**
- **No review required** — available within minutes of upload
- Testers must opt in via the link you share
- App appears in testers' Play Store (may take a few minutes to propagate)

## 9. Verification

After completing all steps, verify your setup:

```bash
# Check Java version
java -version

# Check .NET and workloads
dotnet --version
dotnet workload list

# Verify keystore
keytool -list -v -keystore /path/to/famick-upload.keystore

# Test local build
./scripts/build-play-store.sh --verbose
```

## 10. First Upload

For the very first release, you must upload the AAB manually through the Play Console:

1. Build locally: `./scripts/build-play-store.sh`
2. Go to Play Console > Internal testing > Create new release
3. Upload the AAB from the path shown in build output
4. Add release notes (e.g., "Initial beta release")
5. Review and roll out to internal testers

After this first manual upload, the GitHub Actions workflow will handle subsequent uploads automatically when you push a `v*` tag.

## Troubleshooting

### Build fails: "Java SDK not found"

Ensure Java 17 is installed and the path in the `.csproj` is correct:
```xml
<JavaSdkDirectory Condition="...osx...">
  /opt/homebrew/opt/openjdk@17/libexec/openjdk.jdk/Contents/Home
</JavaSdkDirectory>
```

### Upload fails: "Package name not found"

The app must be created in Play Console first, and the package name (`com.famick.homemanagement`) must match exactly.

### Upload fails: "APK or AAB must be signed"

Ensure keystore properties are being passed correctly. Verify with:
```bash
jarsigner -verify -verbose path/to/app.aab
```

### Testers can't find the app

After the first upload:
1. Ensure testers have accepted the opt-in link
2. Wait 5-10 minutes for Play Store propagation
3. Testers may need to search for "Famick Home" in the Play Store or use the direct link from the Internal testing page
