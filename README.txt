LabelPrinterApp (WinForms, .NET 9)

Solution:
  - LabelPrinterApp.sln (open in Visual Studio)
  - Project folder: LabelPrinterApp\LabelPrinterApp.csproj

Login:
  admin / admin (opens directly to Print Label tab)
  other users (opens to Print Label tab)

Security:
  Design Label tab requires password: "NewViros##9141" (applies to ALL users including admin)

Features:
- Dashboard with 3 tabs: Design Label, Print Label, Reports
- Admin user login opens directly to Print Label tab for quick access
- Password protection on Design Label tab for ALL users (including admin)
- Design: browse .prn, edit, live preview via Labelary API (ZPL only), scrollable preview panel
- Print Label: select design, quantity, start serial, custom date/text, preview, print (stub), logs saved
- Reports: view print logs, filter by design name or serial, reprint selected, Excel export functionality
- Enhanced shortcut codes: {SERIAL}, {SERIAL1-5}, {DATE}, {TIME}, {CHAR_MM}, {CUSTOM_TEXT}, etc.
- Excel Export: Professional reports with statistics and business analytics

Build & Run (CLI):
  cd LabelPrinterApp
  dotnet restore
  dotnet build
  dotnet run

Publish EXE:
  .\publish.ps1   # outputs in bin\Release\net9.0-windows\win-x64\publish

Notes:
- Preview uses Labelary API and supports ZPL. Other PRN flavors show text fallback; or upload a preview image in Design.
- Database app.db is auto-created in the program folder.
- PrintingService.PrintRaw is a stub (always succeeds). Replace with real raw printing for production.
