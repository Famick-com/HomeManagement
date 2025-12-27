# Home Management Plugins

This folder contains product lookup plugins for the Home Management application.

## Quick Start

1. Copy `config.example.json` to `config.json`
2. Add your API keys to `config.json`
3. Restart the application

```bash
cp config.example.json config.json
# Edit config.json with your API keys
```

> **Note:** `config.json` is gitignored to prevent committing API keys.

## Plugin Configuration

Plugins are configured in `config.json`. Each plugin entry has the following properties:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | string | Yes | Unique plugin identifier |
| `enabled` | boolean | Yes | Whether the plugin is active |
| `builtin` | boolean | Yes | `true` for built-in plugins, `false` for external DLLs |
| `assembly` | string | No | Path to DLL (relative to plugins folder, required for external plugins) |
| `displayName` | string | Yes | Human-readable plugin name |
| `config` | object | No | Plugin-specific configuration |

## Built-in Plugins

### USDA FoodData Central (`usda`)

Searches the USDA FoodData Central database for food products and nutrition information.

**Configuration:**
```json
{
  "apiKey": "YOUR_API_KEY",
  "baseUrl": "https://api.nal.usda.gov/fdc/v1/",
  "defaultMaxResults": 20
}
```

To get an API key, register at: https://fdc.nal.usda.gov/api-key-signup.html

## External Plugins

External plugins are DLL files placed in the `external/` subfolder. To add an external plugin:

1. Create a folder for your plugin: `external/myplugin/`
2. Place the plugin DLL: `external/myplugin/MyPlugin.dll`
3. Add an entry to `config.json`:

```json
{
  "id": "myplugin",
  "enabled": true,
  "builtin": false,
  "assembly": "external/myplugin/MyPlugin.dll",
  "displayName": "My Custom Plugin",
  "config": {
    "setting1": "value1"
  }
}
```

## Developing Plugins

To create a custom plugin, implement the `IProductLookupPlugin` interface:

```csharp
public interface IProductLookupPlugin
{
    string PluginId { get; }
    string DisplayName { get; }
    string Version { get; }
    int Priority { get; }
    bool IsAvailable { get; }

    Task InitAsync(JsonElement? pluginConfig, CancellationToken ct = default);
    Task<List<ProductLookupResult>> SearchByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<List<ProductLookupResult>> SearchByNameAsync(string query, int maxResults = 20, CancellationToken ct = default);
}
```

### Plugin Requirements

1. Reference `Famick.HomeManagement.Core` to get the interface definition
2. Implement a parameterless constructor (DI is not available for external plugins)
3. Handle your own HTTP client creation and configuration
4. Return empty list on errors (don't throw exceptions)

## Docker Mounting

To add external plugins via Docker:

```yaml
volumes:
  - ./my-plugins:/app/plugins/external:ro
```
