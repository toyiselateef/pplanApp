# MFG Docs API (.NET 8) - PDF-only

Stateless document-generation API used by Power Pages / Power Automate. Saves nothing to a database. All outputs are PDFs.

## Features
- **/api/workorders/pdf** → Work Order PDF (QuestPDF)
- **/api/pour-plans/pdf** → Pour Plan PDF (QuestPDF) with visual schedule and site diagrams
- **/api/delivery-slips/pdf** → Delivery Slip PDF (QuestPDF)

## Build & Run

```bash
dotnet restore
dotnet build
dotnet run --project MfgDocs.Api
```

API root: http://localhost:5000 (or as shown in console).

## Configuration (appsettings.json)
- Pricing and rounding rules (mirrors BRD logic).
- Branding (company name, address, phone).

## Example Payloads
(see original README for sample WorkOrder and Delivery payloads — same shapes apply for PourPlanRequest)

### Pour Plan (PDF)
```json
POST /api/pour-plans/pdf
{
  "planDate": "2025-09-01",
  "orders": [
    { /* WorkOrderRequest object as defined in Models.cs */ }
  ]
}
```

## Notes on Diagrams
- The Pour Plan PDF includes a Gantt-like horizontal timeline (8-slot day) and per-order footprint sketches (rectangles drawn via QuestPDF canvas). These are approximations for planners and can be refined further with actual scheduling and spatial logic.
- If you want exact AutoCAD-level precision and vector exports, we can add DXF/SVG exporters or integrate with CAD libs — but for printable site diagrams and operational planning PDFs, QuestPDF canvas with scaled rectangles provides an accurate, reproducible visual aid.