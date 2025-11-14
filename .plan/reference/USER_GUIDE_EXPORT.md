# User Guide: Exporting Hydrostatic Data

**Version**: 1.0  
**Last Updated**: October 28, 2025  
**For**: NavArch Studio Users

---

## Table of Contents

1. [Introduction](#introduction)
2. [Before You Begin](#before-you-begin)
3. [Accessing the Export Feature](#accessing-the-export-feature)
4. [Export Formats](#export-formats)
5. [Step-by-Step Export Guide](#step-by-step-export-guide)
6. [Advanced Options](#advanced-options)
7. [Troubleshooting](#troubleshooting)
8. [FAQs](#faqs)
9. [Tips & Best Practices](#tips--best-practices)

---

## Introduction

The Hydrostatics Export feature allows you to export your vessel analysis data in multiple professional formats. Whether you need to create client reports, analyze data in Excel, or archive calculations, this feature has you covered.

### What You Can Export
- Hydrostatic tables (displacement, centers, metacentric data)
- Form coefficients (block, prismatic, midship, waterplane)
- Hydrostatic curves (displacement, KB, LCB, waterplane, metacentric height)
- Vessel geometry information

### Export Formats Available
- **CSV** - For Excel and data analysis
- **JSON** - For API integration and programmatic use
- **PDF** - For professional reports and documentation
- **Excel** - For advanced analysis and presentations

---

## Before You Begin

### Prerequisites
1. **Create a Vessel**: You must have a vessel created in the system
2. **Add Geometry**: Import or manually enter vessel geometry (stations, waterlines, offsets)
3. **Compute Hydrostatics**: Run hydrostatic computations to generate results

### Recommended Workflow
```
Create Vessel → Import Geometry → Compute Hydrostatics → Export Results
```

---

## Accessing the Export Feature

### From Vessel Detail Page

1. **Navigate to Hydrostatics**:
   - Go to `Hydrostatics → Vessels` from the main menu
   - Click on your vessel to open the detail page

2. **Compute Results** (if not already done):
   - Select the "Hydrostatics" tab
   - Configure draft range and step
   - Click "Compute" button
   - Wait for calculations to complete

3. **Open Export Dialog**:
   - Once results are displayed, click the **Export** button in the toolbar
   - The export dialog will appear

---

## Export Formats

### CSV (Comma-Separated Values)

**Best For**: Quick data export, Excel import, data analysis

**Features**:
- ✅ Compact file size (~10 KB)
- ✅ Fast generation (< 100ms)
- ✅ Opens directly in Excel
- ✅ Easy to parse programmatically

**Structure**:
```csv
Draft (m),Displacement (kg),KB (m),LCB (m),...
5.000,5125000.000,2.500,50.000,...
6.000,6355000.000,3.000,50.000,...
```

**Use Cases**:
- Quick Excel import
- Data processing with Python/R
- Backup of raw data

---

### JSON (JavaScript Object Notation)

**Best For**: API integration, data backup, programmatic access

**Features**:
- ✅ Structured format
- ✅ Fast generation (< 100ms)
- ✅ Easy to parse in any language
- ✅ Includes metadata

**Structure**:
```json
[
  {
    "draft": 5.0,
    "dispVolume": 5000,
    "dispWeight": 5125000,
    "kBz": 2.5,
    ...
  }
]
```

**Use Cases**:
- Integration with other systems
- Automated workflows
- Version control of data

---

### PDF (Portable Document Format)

**Best For**: Professional reports, client deliverables, documentation

**Features**:
- ✅ Professional formatting
- ✅ Multiple pages
- ✅ Includes vessel metadata
- ✅ Optional curves graphs
- ✅ Headers and footers

**Generation Time**: 0.5-1.5 seconds

**Contents**:
1. **Cover Page**:
   - Vessel name and description
   - Vessel dimensions (Lpp, Beam, Draft)
   - Generation date and time

2. **Hydrostatic Table**:
   - All computed results
   - Properly formatted with units
   - Easy-to-read layout

3. **Form Coefficients**:
   - Block coefficient (Cb)
   - Prismatic coefficient (Cp)
   - Midship coefficient (Cm)
   - Waterplane coefficient (Cwp)

4. **Curves Data** (if included):
   - Displacement curve
   - KB curve
   - LCB curve
   - Waterplane area curve
   - Metacentric height curve

**Use Cases**:
- Client reports
- Documentation
- Project deliverables
- Archival

---

### Excel (XLSX)

**Best For**: Advanced analysis, charting, presentations

**Features**:
- ✅ Multi-sheet workbook
- ✅ Professional formatting
- ✅ Auto-sized columns
- ✅ Optional curves data

**Generation Time**: 0.3-0.8 seconds

**Sheets**:

**Sheet 1: Vessel Information**
- Name, Description
- Dimensions (Lpp, Beam, Draft)
- Creation/update dates

**Sheet 2: Hydrostatic Table**
- All computed results
- Number formatting applied
- Color-coded headers
- Ready for charting

**Sheet 3: Curves Data** (if included)
- One section per curve
- X and Y values
- Easy to create charts

**Use Cases**:
- Creating custom charts
- Advanced analysis
- Presentations
- Data comparison

---

## Step-by-Step Export Guide

### Step 1: Prepare Your Data

1. Ensure you have computed hydrostatic results
2. Check that results are displayed in the table
3. Review the data for accuracy

### Step 2: Open Export Dialog

1. Click the **Export** button in the toolbar
2. The export dialog will open

![Export Dialog](../screenshots/export-dialog.png)

### Step 3: Select Format

Choose your desired export format:

- **CSV**: Select "CSV - Comma-separated values"
- **JSON**: Select "JSON - JavaScript object notation"
- **PDF**: Select "PDF - Professional report with charts"
- **Excel**: Select "Excel (XLSX) - Advanced analysis ready"

Each format shows a brief description to help you choose.

### Step 4: Configure Options

#### Include Curves (PDF and Excel only)

Check the "Include hydrostatic curves" box if you want to export curve data along with the table.

**Curves Included**:
- Displacement vs Draft
- KB vs Draft
- LCB vs Draft
- Waterplane Area vs Draft
- Transverse Metacentric Height vs Draft

**Note**: Including curves increases generation time by 200-500ms.

### Step 5: Export

1. Click the **Export** button
2. A loading toast will appear:
   - "Preparing export..." (CSV/JSON)
   - "Generating PDF report..." (PDF)
   - "Generating Excel workbook..." (Excel)

3. Wait for the file to generate
4. Your browser will automatically download the file

### Step 6: Success

A success toast will appear showing:
- "Export successful! Downloaded [filename]"

The file will be saved to your browser's download folder.

---

## Advanced Options

### Exporting with Loadcases

If you've created loadcases (weight distributions):

1. Select your loadcase from the dropdown (if available)
2. Compute hydrostatics for that loadcase
3. Export as normal

The exported file will include loadcase-specific results.

### File Naming

Files are automatically named with:
- Vessel name (spaces replaced with underscores)
- "hydrostatics"
- Appropriate file extension

**Example**: `My_Test_Vessel_hydrostatics.pdf`

### Multiple Exports

You can export the same data in multiple formats:

1. Export to CSV for quick Excel viewing
2. Export to PDF for client report
3. Export to Excel for detailed analysis

Each export is independent and doesn't affect the others.

---

## Troubleshooting

### "No results to export"

**Problem**: The Export button is disabled or shows an error.

**Solution**:
1. Ensure you've computed hydrostatics
2. Check that results are displayed in the table
3. Try recomputing if results seem stale

### "Export failed: Vessel not found"

**Problem**: The backend can't find your vessel.

**Solution**:
1. Refresh the page
2. Navigate back to the vessel from the vessels list
3. Try the export again

### "Export failed: Network error"

**Problem**: Can't connect to the backend service.

**Solution**:
1. Check your internet connection
2. Verify the backend is running (if local development)
3. Try again in a few moments

### PDF/Excel Generation is Slow

**Problem**: PDF or Excel export takes > 2 seconds.

**Possible Causes**:
- Large dataset (100+ draft points)
- Curves included (adds processing time)
- Server under heavy load

**Solutions**:
- Reduce number of draft points
- Export without curves if not needed
- Try again during off-peak hours

### Downloaded File Won't Open

**Problem**: Exported file is corrupted or won't open.

**Solutions**:
- **CSV**: Ensure you're opening with Excel or text editor
- **JSON**: Use a JSON viewer or text editor
- **PDF**: Update your PDF reader (Adobe, Foxit, etc.)
- **Excel**: Update Microsoft Excel or use LibreOffice

### File Downloads to Unknown Location

**Problem**: Can't find the downloaded file.

**Solution**:
1. Check your browser's Downloads folder
2. Check browser download settings
3. Look in `C:\Users\[YourName]\Downloads` (Windows)
4. Look in `~/Downloads` (Mac/Linux)

---

## FAQs

### Q: Can I export multiple vessels at once?

**A**: Not currently. Each export is for a single vessel. You'll need to export each vessel individually.

### Q: What units are used in exports?

**A**: All exports use metric units:
- Lengths: meters (m)
- Weights: kilograms (kg)
- Areas: square meters (m²)
- Volumes: cubic meters (m³)

### Q: Can I customize the PDF report layout?

**A**: Not currently. The PDF uses a standard professional layout. Custom templates may be added in future versions.

### Q: Do exports include my vessel geometry?

**A**: PDF and Excel exports include vessel dimensions (Lpp, Beam, Design Draft) but not the detailed offset table. CSV and JSON exports include only computed results.

### Q: How do I export just the curves?

**A**: Currently, curves are exported along with the hydrostatic table when you check "Include curves". Standalone curve export may be added in the future.

### Q: Are exports saved in the cloud?

**A**: No, exports are generated on-demand and downloaded directly to your device. They are not stored on the server.

### Q: Can I schedule automatic exports?

**A**: Not currently. Scheduled exports may be added in a future version.

### Q: What's the maximum number of data points I can export?

**A**: There's no hard limit, but performance degrades after ~1000 draft points. We recommend exporting <100 points for optimal performance.

---

## Tips & Best Practices

### Choosing the Right Format

**Quick View in Excel**: Use CSV
**Detailed Analysis**: Use Excel
**Client Reports**: Use PDF
**API Integration**: Use JSON

### Optimization Tips

1. **Reduce Draft Points**: Compute with fewer drafts for faster exports
2. **Skip Curves**: If you don't need curves, uncheck the box for faster generation
3. **Use CSV for Large Datasets**: CSV is fastest for bulk data

### Professional Reports

For client-facing reports:
1. Use **PDF format**
2. Check **Include curves**
3. Ensure vessel name and description are professional
4. Review the data before exporting

### Data Backup

To backup your analysis:
1. Export to **JSON** (complete data)
2. Export to **Excel** (for viewing)
3. Store both files with version numbers

### Collaboration

To share with colleagues:
- **Technical team**: Share CSV or JSON
- **Management**: Share PDF
- **Analysts**: Share Excel

---

## Example Workflow

### Complete Export Workflow Example

**Scenario**: You need to create a client report for a new vessel design.

**Steps**:

1. **Create Vessel**:
   ```
   Name: "Offshore Supply Vessel OSV-2025"
   Lpp: 65 m
   Beam: 14 m
   Design Draft: 5.5 m
   ```

2. **Import Geometry**:
   - Upload CSV with stations, waterlines, offsets
   - Verify import success

3. **Compute Hydrostatics**:
   - Draft range: 3.0 m to 7.0 m
   - Draft step: 0.5 m
   - Click "Compute"

4. **Review Results**:
   - Check displacement values
   - Verify centers and metacentric data
   - Ensure form coefficients are reasonable

5. **Export to PDF**:
   - Click Export button
   - Select "PDF"
   - Check "Include curves"
   - Click Export
   - Wait for "OSV-2025_hydrostatics.pdf" to download

6. **Export to Excel** (for internal analysis):
   - Click Export button again
   - Select "Excel"
   - Check "Include curves"
   - Click Export
   - Wait for "OSV-2025_hydrostatics.xlsx" to download

7. **Share**:
   - Send PDF to client
   - Share Excel with engineering team

**Result**: Professional deliverables in < 2 minutes! 🎉

---

## Getting Help

### Need More Help?

- **Documentation**: Check [HYDROSTATICS_MODULE.md](./HYDROSTATICS_MODULE.md)
- **Issues**: Report problems on GitHub Issues
- **Support**: Contact your system administrator

### Feedback

We're constantly improving! If you have suggestions for:
- New export formats
- Additional options
- UI improvements

Please let us know through GitHub Issues or your support channel.

---

## Version History

### v1.0 (October 28, 2025)
- Initial export feature release
- CSV, JSON, PDF, Excel formats
- Include curves option
- Toast notifications

### Future Versions (Planned)
- v1.1: Custom PDF templates
- v1.2: Batch export multiple vessels
- v1.3: Scheduled exports
- v1.4: Cloud storage integration

---

**End of User Guide**

*For technical documentation, see [HYDROSTATICS_MODULE.md](./HYDROSTATICS_MODULE.md)*  
*For developer documentation, see [../docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md)*

