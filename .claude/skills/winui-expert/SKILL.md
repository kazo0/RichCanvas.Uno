---
name: winui-expert
description: Prevent common Uno/WinUI 3 API mistakes and guide correct WinUI 3 /Uno usage. Load when generating WinUI 3 or Uno Platform code that touches the UI. Covers the full UWP-to-WinUI 3 migration table, XAML controls, Data Binding patterns, and C# conventions.
metadata:
  author: uno-platform
  version: "1.0"
  category: rapid-prototype
---

# WinUI 3 / Uno Error Prevention & API Guide

This skill ensures correct WinUI 3 and Uno Platform API usage. It prevents the most common mistakes caused by legacy UWP patterns that dominate training data but are **wrong** for WinUI 3 desktop apps. Always use the correct WinUI 3 alternative — **never** use legacy UWP APIs.

## XAML & Controls

### Namespace Conventions

```xml
<!-- Correct WinUI 3 namespaces -->
xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
xmlns:local="using:MyApp"
xmlns:controls="using:MyApp.Controls"

<!-- The default namespace maps to Microsoft.UI.Xaml, NOT Windows.UI.Xaml -->
```

### Key Controls and Patterns

- **NavigationView**: Primary navigation pattern for WinUI 3 apps
- **TabView**: Multi-document or multi-tab interfaces
- **InfoBar**: In-app notifications (not UWP `InAppNotification`)
- **NumberBox**: Numeric input with validation
- **TeachingTip**: Contextual help
- **BreadcrumbBar**: Hierarchical navigation breadcrumbs
- **Expander**: Collapsible content sections
- **ItemsRepeater**: Flexible, virtualizing list layouts
- **TreeView**: Hierarchical data display
- **ProgressRing / ProgressBar**: Use `IsIndeterminate` for unknown progress

### ContentDialog (Critical Pattern)

```csharp
// ✅ CORRECT — Always set XamlRoot
var dialog = new ContentDialog
{
    Title = "Confirm Action",
    Content = "Are you sure?",
    PrimaryButtonText = "Yes",
    CloseButtonText = "No",
    XamlRoot = this.Content.XamlRoot  // REQUIRED in WinUI 3
};

var result = await dialog.ShowAsync();
```

```csharp
// ❌ WRONG — UWP MessageDialog
var dialog = new Windows.UI.Popups.MessageDialog("Are you sure?");
await dialog.ShowAsync();

// ❌ WRONG — ContentDialog without XamlRoot
var dialog = new ContentDialog { Title = "Error" };
await dialog.ShowAsync();  // Throws InvalidOperationException
```

### File/Folder Pickers

```csharp
// ✅ CORRECT — Pickers need window handle in WinUI 3
var picker = new FileOpenPicker();
var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
picker.FileTypeFilter.Add(".txt");
var file = await picker.PickSingleFileAsync();
```

## Data Binding

### Binding Best Practices

- Prefer `{x:Bind}` over `{Binding}` — 8–20x faster, compile-time checked
- Use `Mode=OneWay` for dynamic data, `Mode=OneTime` for static
- Use `Mode=TwoWay` only for editable controls (TextBox, ToggleSwitch, etc.)
- Set `x:DataType` on Page/UserControl for compiled bindings


## Accessibility

- Set `AutomationProperties.Name` on all interactive control
- Hide decorative elements with `AutomationProperties.AccessibilityView="Raw"`

## Documentation Reference

### WinUI 3 API Reference

When looking up API references, control usage, or platform guidance:

- Use `microsoft_docs_search` for WinUI 3 and Windows App SDK documentation
- Use `microsoft_code_sample_search` with `language: "csharp"` for working code samples
- Always search for **"WinUI 3"** or **"Windows App SDK"** — never UWP equivalents

Key reference repositories:

- **[microsoft/microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml)** — WinUI 3 source code
- **[unoplatform/uno](https://github.com/unoplatform/uno)** — Uno Platform source code (cross-platform implementation of WinUI)
- **[microsoft/WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK)** — Windows App SDK
- **[microsoft/WindowsAppSDK-Samples](https://github.com/microsoft/WindowsAppSDK-Samples)** — Official samples
- **[microsoft/WinUI-Gallery](https://github.com/microsoft/WinUI-Gallery)** — WinUI 3 control gallery app

## Uno API Reference

When looking up API references, control usage, or platform guidance:

- Use `uno_platform_docs_search` for Uno Platform documentation

For a reference of implemented APIs and platform support, consult the Uno Platform API reference: https://platform.uno/docs/articles/implemented-views.html

Key reference repositories:

- **[unoplatform/uno](https://github.com/unoplatform/uno)** — Uno Platform source code (cross-platform implementation of WinUI)
- **[unoplatform/Uno.Samples](https://github.com/unoplatform/Uno.Samples)** — Official samples

## Spacing & Layout

**Core principle:** Use a **4px grid system**. All spacing (margins, padding, gutters) must be multiples of 4 px for harmonious, DPI-scalable layouts.

| Spacing | Usage |
|---------|-------|
| **4 px** | Tight/compact spacing between related elements |
| **8 px** | Standard spacing between controls and labels |
| **12 px** | Gutters in small windows; padding within cards |
| **16 px** | Standard content padding |
| **24 px** | Gutters in large windows; section spacing |
| **36–48 px** | Major section separators |

**Responsive breakpoints:**

| Size | Width | Typical Device |
|------|-------|----------------|
| Small | < 640px | Phones, small tablets |
| Medium | 641–1007px | Tablets, small PCs |
| Large | ≥ 1008px | Desktops, laptops |

```xml
<!-- Responsive layout with VisualStateManager -->
<VisualStateManager.VisualStateGroups>
    <VisualStateGroup>
        <VisualState x:Name="WideLayout">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="1008" />
            </VisualState.StateTriggers>
            <!-- Wide layout setters -->
        </VisualState>
        <VisualState x:Name="NarrowLayout">
            <VisualState.StateTriggers>
                <AdaptiveTrigger MinWindowWidth="0" />
            </VisualState.StateTriggers>
            <!-- Narrow layout setters -->
        </VisualState>
    </VisualStateGroup>
</VisualStateManager.VisualStateGroups>
```

### Layout Controls

| Control | When to Use |
|---------|-------------|
| **Grid** | Complex layouts with rows/columns; preferred over nested StackPanels |
| **StackPanel / VerticalStackLayout** | Simple linear layouts (avoid deep nesting) |
| **RelativePanel** | Responsive layouts where elements position relative to each other |
| **ItemsRepeater** | Virtualizing, customizable list/grid layouts |
| **ScrollViewer** | Scrollable content areas |

**Best practices:**
- Prefer `Grid` over deeply nested `StackPanel` chains (performance)
- Use `Auto` for content-sized rows/columns, `*` for proportional sizing
- Avoid fixed pixel sizes — use responsive sizing with `MinWidth`/`MaxWidth`

## Control Selection Guide

| Need | Control | Notes |
|------|---------|-------|
| Primary navigation | **NavigationView** | Left or top nav; supports hierarchical items |
| Multi-document tabs | **TabView** | Tear-off, reorder, close support |
| In-app notifications | **InfoBar** | Persistent, non-blocking; severity levels |
| Contextual help | **TeachingTip** | One-time guidance; attach to target element |
| Numeric input | **NumberBox** | Built-in validation, spin buttons, formatting |
| Search with suggestions | **AutoSuggestBox** | Autocomplete, custom filtering |
| Hierarchical data | **TreeView** | Multi-select, drag-and-drop |
| Collection display | **ItemsView** | Modern collection control with built-in selection and layout flexibility |
| Standard lists/grids | **ListView / GridView** | Virtualized lists with built-in selection, grouping, drag-and-drop |
| Custom collection layout | **ItemsRepeater** | Lowest-level virtualizing layout — no built-in selection or interaction |
| Settings | **ToggleSwitch** | For on/off settings (not CheckBox) |
| Date selection | **CalendarDatePicker** | Calendar dropdown; use `DatePicker` for simple date |
| Progress (known) | **ProgressBar** | Determinate or indeterminate |
| Progress (unknown) | **ProgressRing** | Indeterminate spinner |
| Status indicators | **InfoBadge** | Dot, icon, or numeric badge |
| Expandable sections | **Expander** | Collapsible content sections |
| Breadcrumb navigation | **BreadcrumbBar** | Shows hierarchy path |

## Error Handling & Resilience

### Exception Handling in Async Code

```csharp
// ✅ CORRECT — Always wrap async operations
private async void Button_Click(object sender, RoutedEventArgs e)
{
    try
    {
        await LoadDataAsync();
    }
    catch (HttpRequestException ex)
    {
        ShowError("Network error", ex.Message);
    }
    catch (Exception ex)
    {
        ShowError("Unexpected error", ex.Message);
    }
}

private void ShowError(string title, string message)
{
    // Use InfoBar for non-blocking errors
    ErrorInfoBar.Title = title;
    ErrorInfoBar.Message = message;
    ErrorInfoBar.IsOpen = true;
    ErrorInfoBar.Severity = InfoBarSeverity.Error;
}
```

### Unhandled Exception Handler

```csharp
// In App.xaml.cs
public App()
{
    this.InitializeComponent();
    this.UnhandledException += App_UnhandledException;
}

private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
{
    // Log the exception
    Logger.LogCritical(e.Exception, "Unhandled exception");
    e.Handled = true; // Prevent crash if recoverable
}
```

## Resource Management

### String Resources (Localization)

```
Strings/
  en-us/
    Resources.resw
  fr-fr/
    Resources.resw
```

```xml
<!-- Reference in XAML -->
<TextBlock x:Uid="WelcomeMessage" />
<!-- Matches WelcomeMessage.Text in .resw -->
```

```csharp
// Reference in code
var loader = new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader();
string text = loader.GetString("WelcomeMessage/Text");
```

### Image Assets

- Place in `Assets/` folder
- Use qualified naming for DPI scaling: `logo.scale-200.png`
- Support scales: 100, 125, 150, 200, 300, 400
- Reference without scale qualifier: `ms-appx:///Assets/logo.png`

## C# Conventions

- File-scoped namespaces
- Nullable reference types enabled
- Pattern matching preferred over `as`/`is` with null checks
- `System.Text.Json` with source generators (not Newtonsoft)
- Allman brace style (opening brace on new line)
- PascalCase for types, methods, properties; camelCase for private fields
- `var` only when type is obvious from the right side