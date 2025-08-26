# Shortcut Codes Documentation

## Overview
The Label Printer App now supports shortcut codes that are automatically replaced with actual values during preview and printing. This feature allows you to create dynamic labels with current date, time, and other system information.

## Available Shortcut Codes

| Shortcut Code | Description | Example Output |
|---------------|-------------|----------------|
| `{SERIAL}` | Simple serial number | 1, 2, 3, 123... |
| `{SERIAL1}` | 2-digit serial number | 01, 02, 03, 123 |
| `{SERIAL2}` | 3-digit serial number | 001, 002, 003, 123 |
| `{SERIAL3}` | 4-digit serial number | 0001, 0002, 0003, 0123 |
| `{SERIAL4}` | 5-digit serial number | 00001, 00002, 00003, 00123 |
| `{SERIAL5}` | 6-digit serial number | 000001, 000002, 000003, 000123 |
| `{DATE}` | Current system date | 26/08/2025 |
| `{DD}` | Current day (2 digits) | 26 |
| `{MM}` | Current month (2 digits) | 08 |
| `{YYYY}` | Current year (4 digits) | 2025 |
| `{YY}` | Current year (2 digits) | 25 |
| `{CHAR_MM}` | Month as character | H (for August) |
| `{TIME}` | Current time (24-hour format) | 14:30:25 |

## Month Character Mapping (`{CHAR_MM}`)
- 01 (January) → A
- 02 (February) → B
- 03 (March) → C
- 04 (April) → D
- 05 (May) → E
- 06 (June) → F
- 07 (July) → G
- 08 (August) → H
- 09 (September) → I
- 10 (October) → J
- 11 (November) → K
- 12 (December) → L

## Usage Examples

### Basic ZPL Label with Serial Number
```zpl
^XA
^FO50,50^A0N,40,40^FDSerial: {SERIAL}^FS
^XZ
```

### Complete Label with All Serial Formats
```zpl
^XA
^PW600
^FO50,50^A0N,35,35^FDSerial: {SERIAL}^FS
^FO50,100^A0N,30,30^FD2-digit: {SERIAL1}^FS
^FO50,150^A0N,30,30^FD3-digit: {SERIAL2}^FS
^FO50,200^A0N,30,30^FD4-digit: {SERIAL3}^FS
^FO50,250^A0N,25,25^FD5-digit: {SERIAL4}^FS
^FO50,300^A0N,25,25^FD6-digit: {SERIAL5}^FS
^FO50,350^A0N,20,20^FDDate: {DATE} Time: {TIME}^FS
^XZ
```

### Production Label with Formatted Serial
```zpl
^XA
^PW800
^FO50,50^A0N,45,45^FDProduct: P-{SERIAL3}^FS
^FO50,120^A0N,35,35^FDBatch: {YY}{CHAR_MM}{SERIAL2}^FS
^FO50,180^A0N,30,30^FDManufactured: {DATE}^FS
^FO50,240^A0N,25,25^FDSerial: {SERIAL5}^FS
^XZ
```

## How It Works

### In Design Mode
- When you type shortcut codes in the PRN editor, they are shown as-is
- The preview automatically replaces shortcut codes with sample values
- `{SERIAL}` shows as "1" in preview
- Date/time codes show current system values

### In Print Mode
- Preview shows the actual serial number you're about to print
- All shortcut codes are replaced with current values at print time
- Each label in a batch gets incremented serial numbers

### In Reports
- Reprint function uses the original serial number
- Date/time codes reflect the current reprint time, not original print time

## Features

### Shortcut Codes Help Button
- Click "Shortcut Codes" button in Design Label tab
- Shows complete list of available codes with current examples
- "Insert Sample" button adds a sample label with all shortcut codes

### Real-time Preview
- Preview updates automatically as you type
- Shows how your label will look with actual data
- Works with ZPL format via Labelary API

### Smart Replacement
- Codes are case-sensitive (must be in UPPERCASE)
- Works with any PRN format (ZPL, EPL, CPCL, etc.)
- Preserves exact positioning and formatting

## Best Practices

1. **Use Consistent Formatting**
   - Always use UPPERCASE for shortcut codes
   - Include braces: `{CODE}` not `CODE`

2. **Test Your Design**
   - Use the preview to verify placement
   - Print a test label before batch printing

3. **Consider Label Size**
   - Ensure enough space for variable-length data
   - Account for different serial number lengths (1 vs 1000)

4. **Date Format Considerations**
   - `{DATE}` uses DD/MM/YYYY format
   - Use individual components (`{DD}`, `{MM}`, `{YYYY}`) for custom formats

## Sample Designs Included

The application includes updated sample designs:
- **sample_3x1.prn**: Compact label with serial, date, and time
- **sample_4x2.prn**: Full-featured label with all available codes
- **Sample ZPL with Shortcuts**: Database sample showing comprehensive usage

## Troubleshooting

### Common Issues
1. **Codes not replacing**: Ensure exact case matching `{SERIAL}` not `{serial}`
2. **Preview shows codes**: Normal behavior in design mode - actual values appear in print preview
3. **Wrong date format**: Currently uses DD/MM/YYYY - modify ShortcutCodeService for different formats

### Error Handling
- Invalid dates default to current system time
- Missing printers fall back gracefully
- API failures show text preview with codes replaced

## Technical Implementation

### Files Modified
- `Services/ShortcutCodeService.cs` - Core replacement logic
- `UI/DesignForm.cs` - Help dialog and preview integration
- `UI/PrintForm.cs` - Print-time replacement
- `UI/ReportsForm.cs` - Reprint functionality
- `Services/Database.cs` - Updated sample design

### Key Methods
- `ShortcutCodeService.ReplaceShortcutCodes()` - Main replacement function
- `ShortcutCodeService.ReplaceShortcutCodesForPreview()` - Preview-specific replacement
- `ShortcutCodeService.GetShortcutCodesHelpText()` - Help documentation

This feature enhances the label printer application by providing dynamic, time-sensitive labeling capabilities while maintaining compatibility with existing designs.
