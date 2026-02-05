import Foundation
import Security

/// Reads JWT tokens and base URL from the shared keychain group written by the main MAUI app.
enum KeychainHelper {
    // IMPORTANT: Replace TEAMID with your actual Apple Team ID, or configure via build settings.
    // The access group must match the keychain-access-groups entitlement.
    private static let accessGroup = "\(teamId).com.famick.homemanagement.shared"
    private static let serviceName = "com.famick.homemanagement"

    // Team ID - set via build setting or hardcode here
    private static var teamId: String {
        // Try reading from bundle's build settings first
        if let id = Bundle.main.infoDictionary?["AppIdentifierPrefix"] as? String {
            return id.trimmingCharacters(in: CharacterSet(charactersIn: "."))
        }
        // Fallback: read from the entitlements at runtime
        return ""
    }

    static func getAccessToken() -> String? {
        return readKeychainItem(account: "widget_access_token")
    }

    static func getBaseUrl() -> String? {
        return readKeychainItem(account: "widget_base_url")
    }

    private static func readKeychainItem(account: String) -> String? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: serviceName,
            kSecAttrAccount as String: account,
            kSecAttrAccessGroup as String: accessGroup,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne
        ]

        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)

        guard status == errSecSuccess,
              let data = result as? Data,
              let value = String(data: data, encoding: .utf8) else {
            return nil
        }

        return value
    }
}
