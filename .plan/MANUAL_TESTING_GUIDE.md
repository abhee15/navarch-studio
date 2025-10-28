# Manual Testing Guide - Hydrostatics Export Feature

**Version**: 1.0  
**Date**: October 28, 2025  
**Purpose**: Comprehensive testing checklist for the Hydrostatics Export feature

---

## Overview

This guide provides step-by-step testing procedures to validate the Hydrostatics Export feature before production deployment.

**Estimated Testing Time**: 1-2 hours  
**Prerequisites**: Running instance of NavArch Studio (local or staging)

---

## Test Environment Setup

### Required
- [ ] NavArch Studio running locally or on staging
- [ ] Test user account created
- [ ] Browser: Chrome/Edge (primary), Firefox/Safari (secondary)
- [ ] PDF reader installed (Adobe Acrobat, Foxit, etc.)
- [ ] Microsoft Excel or LibreOffice Calc
- [ ] Text editor for CSV/JSON viewing

### Test Data
- [ ] At least one test vessel with geometry
- [ ] Sample geometry data (stations, waterlines, offsets)
- [ ] Computed hydrostatic results

---

## Test Plan

### Phase 1: Basic Functionality

#### Test 1.1: CSV Export (Basic)

**Objective**: Verify CSV export generates valid file

**Steps**:
1. Navigate to vessel detail page
2. Ensure hydrostatic results are displayed
3. Click Export button
4. Select "CSV" format
5. Click Export

**Expected Results**:
- [ ] Toast notification: "Preparing export..."
- [ ] File downloads: `[VesselName]_hydrostatics.csv`
- [ ] Toast notification: "Export successful! Downloaded..."
- [ ] File size: 5-15 KB
- [ ] Opens in Excel without errors

**Validation**:
- [ ] CSV contains header row
- [ ] Column headers: Draft (m), Displacement (kg), KB (m), etc.
- [ ] All rows have correct number of columns
- [ ] Numeric values are properly formatted
- [ ] No missing or corrupt data

**Pass/Fail**: ___________

---

#### Test 1.2: JSON Export (Basic)

**Objective**: Verify JSON export generates valid file

**Steps**:
1. From same vessel, click Export
2. Select "JSON" format
3. Click Export

**Expected Results**:
- [ ] Toast notification: "Preparing export..."
- [ ] File downloads: `[VesselName]_hydrostatics.json`
- [ ] Toast notification: "Export successful..."
- [ ] File size: 10-25 KB
- [ ] Opens in text editor

**Validation**:
- [ ] Valid JSON syntax (paste into jsonlint.com)
- [ ] Array of result objects
- [ ] Each object has all expected properties
- [ ] Property names match API DTOs (camelCase)
- [ ] Numeric values are numbers (not strings)

**Pass/Fail**: ___________

---

#### Test 1.3: PDF Export (Basic, No Curves)

**Objective**: Verify PDF export generates valid document

**Steps**:
1. From same vessel, click Export
2. Select "PDF" format
3. **Uncheck** "Include curves"
4. Click Export

**Expected Results**:
- [ ] Toast notification: "Generating PDF report..."
- [ ] Generation time: < 2 seconds
- [ ] File downloads: `[VesselName]_hydrostatics.pdf`
- [ ] Toast notification: "Export successful..."
- [ ] File size: 50-150 KB
- [ ] Opens in PDF reader

**Validation**:
- [ ] Cover page present with vessel name
- [ ] Vessel dimensions displayed correctly
- [ ] Generation date/time shown
- [ ] Hydrostatic table present with all data
- [ ] Table is properly formatted with borders
- [ ] Form coefficients section present
- [ ] Page numbers in footer
- [ ] All text is readable (no overlaps)
- [ ] No "curves" section (since not included)

**Pass/Fail**: ___________

---

#### Test 1.4: Excel Export (Basic, No Curves)

**Objective**: Verify Excel export generates valid workbook

**Steps**:
1. From same vessel, click Export
2. Select "Excel (XLSX)" format
3. **Uncheck** "Include curves"
4. Click Export

**Expected Results**:
- [ ] Toast notification: "Generating Excel workbook..."
- [ ] Generation time: < 1.5 seconds
- [ ] File downloads: `[VesselName]_hydrostatics.xlsx`
- [ ] Toast notification: "Export successful..."
- [ ] File size: 15-35 KB
- [ ] Opens in Excel/LibreOffice

**Validation**:
- [ ] Workbook has 2 sheets: "Vessel Information", "Hydrostatic Table"
- [ ] Sheet 1 contains vessel name, dimensions, dates
- [ ] Sheet 2 contains all hydrostatic data
- [ ] Headers are color-coded
- [ ] Columns are auto-sized
- [ ] Number formatting applied
- [ ] No #VALUE or #REF errors
- [ ] Data can be used to create charts

**Pass/Fail**: ___________

---

### Phase 2: Advanced Features

#### Test 2.1: PDF Export with Curves

**Objective**: Verify curves are included in PDF

**Steps**:
1. Click Export
2. Select "PDF" format
3. **Check** "Include curves"
4. Click Export

**Expected Results**:
- [ ] Generation time: < 3 seconds
- [ ] File downloads successfully
- [ ] File size: 80-200 KB (larger due to curves)

**Validation**:
- [ ] All basic PDF validations pass (from Test 1.3)
- [ ] Additional "Curves Data" section present
- [ ] Lists data for each curve type:
  - [ ] Displacement curve
  - [ ] KB curve
  - [ ] LCB curve
  - [ ] Waterplane area curve
  - [ ] Metacentric height curve
- [ ] Curve data is properly formatted
- [ ] No data corruption

**Pass/Fail**: ___________

---

#### Test 2.2: Excel Export with Curves

**Objective**: Verify curves are included in Excel

**Steps**:
1. Click Export
2. Select "Excel" format
3. **Check** "Include curves"
4. Click Export

**Expected Results**:
- [ ] Generation time: < 2 seconds
- [ ] File size: 25-50 KB

**Validation**:
- [ ] Workbook has 3 sheets now: "Vessel Information", "Hydrostatic Table", "Curves"
- [ ] Sheet 3 contains all curve data
- [ ] Each curve has X and Y columns
- [ ] Data is properly formatted
- [ ] Can create charts from curve data

**Pass/Fail**: ___________

---

### Phase 3: Data Integrity

#### Test 3.1: Data Accuracy (CSV vs Display)

**Objective**: Verify exported data matches displayed data

**Steps**:
1. Note first 3 rows of data from UI table
2. Export to CSV
3. Open CSV and compare

**Validation**:
- [ ] Draft values match exactly
- [ ] Displacement values match (within rounding)
- [ ] KB values match
- [ ] LCB values match
- [ ] All other values match displayed data

**Pass/Fail**: ___________

---

#### Test 3.2: Data Consistency (All Formats)

**Objective**: Verify data is consistent across all export formats

**Steps**:
1. Export same vessel data to CSV, JSON, PDF, Excel
2. Compare the same data points across all files

**Validation**:
- [ ] Draft values identical in all formats
- [ ] Displacement identical (accounting for formatting)
- [ ] KB identical
- [ ] LCB identical
- [ ] All computed values consistent

**Pass/Fail**: ___________

---

### Phase 4: Error Handling

#### Test 4.1: Export Without Results

**Objective**: Verify proper error handling when no results exist

**Steps**:
1. Create a new vessel
2. Do NOT compute hydrostatics
3. Attempt to click Export button

**Expected Results**:
- [ ] Export button is disabled (grayed out)
- OR
- [ ] Error toast: "No results to export"

**Pass/Fail**: ___________

---

#### Test 4.2: Network Error Simulation

**Objective**: Verify proper error handling on network failure

**Steps**:
1. Open browser DevTools
2. Go to Network tab
3. Enable "Offline" mode
4. Try to export PDF or Excel

**Expected Results**:
- [ ] Loading toast appears
- [ ] After timeout, error toast appears
- [ ] Error message is user-friendly
- [ ] No unhandled exceptions in console

**Pass/Fail**: ___________

---

#### Test 4.3: Large Dataset Export

**Objective**: Test performance with large number of data points

**Steps**:
1. Compute hydrostatics with 100+ draft points
2. Export to each format

**Expected Results**:
- [ ] CSV export: < 500ms
- [ ] JSON export: < 500ms
- [ ] PDF export: < 5 seconds
- [ ] Excel export: < 3 seconds
- [ ] No timeout errors
- [ ] Files open correctly

**Pass/Fail**: ___________

---

### Phase 5: User Experience

#### Test 5.1: Toast Notifications

**Objective**: Verify all toast notifications work correctly

**Validation**:
- [ ] Loading toast appears immediately on export
- [ ] Loading toast updates message for PDF ("Generating PDF report...")
- [ ] Loading toast updates message for Excel ("Generating Excel workbook...")
- [ ] Success toast appears on completion
- [ ] Success toast shows filename
- [ ] Error toast appears on failure
- [ ] Error toast shows helpful message
- [ ] Toasts auto-dismiss after 3-5 seconds

**Pass/Fail**: ___________

---

#### Test 5.2: Dialog Behavior

**Objective**: Verify export dialog behaves correctly

**Validation**:
- [ ] Dialog opens when Export button clicked
- [ ] Dialog closes when Cancel clicked
- [ ] Dialog closes automatically after successful export
- [ ] Format selection radio buttons work
- [ ] Include curves checkbox works
- [ ] Checkbox only enabled for PDF/Excel
- [ ] Export button disabled while exporting
- [ ] Close button (X) works

**Pass/Fail**: ___________

---

#### Test 5.3: File Download Behavior

**Objective**: Verify files download correctly

**Validation**:
- [ ] Files download to browser's Downloads folder
- [ ] Filenames are properly formatted (underscores, no spaces)
- [ ] File extensions are correct (.csv, .json, .pdf, .xlsx)
- [ ] Browser shows download progress
- [ ] Can click "Show in folder" from browser

**Pass/Fail**: ___________

---

### Phase 6: Cross-Browser Testing

#### Test 6.1: Chrome/Edge

**Browser**: Chrome or Edge  
**Steps**: Run Tests 1.1-1.4 (basic exports)

**Results**:
- [ ] CSV export: PASS / FAIL
- [ ] JSON export: PASS / FAIL
- [ ] PDF export: PASS / FAIL
- [ ] Excel export: PASS / FAIL

**Notes**: ___________

---

#### Test 6.2: Firefox

**Browser**: Firefox  
**Steps**: Run Tests 1.1-1.4 (basic exports)

**Results**:
- [ ] CSV export: PASS / FAIL
- [ ] JSON export: PASS / FAIL
- [ ] PDF export: PASS / FAIL
- [ ] Excel export: PASS / FAIL

**Notes**: ___________

---

#### Test 6.3: Safari (Mac only)

**Browser**: Safari  
**Steps**: Run Tests 1.1-1.4 (basic exports)

**Results**:
- [ ] CSV export: PASS / FAIL
- [ ] JSON export: PASS / FAIL
- [ ] PDF export: PASS / FAIL
- [ ] Excel export: PASS / FAIL

**Notes**: ___________

---

### Phase 7: Performance Testing

#### Test 7.1: Export Speed Benchmarks

**Objective**: Measure export generation times

| Format | Draft Points | Time (ms) | Pass (<2s) |
|--------|--------------|-----------|------------|
| CSV | 10 | _______ | [ ] |
| CSV | 50 | _______ | [ ] |
| CSV | 100 | _______ | [ ] |
| JSON | 10 | _______ | [ ] |
| JSON | 50 | _______ | [ ] |
| JSON | 100 | _______ | [ ] |
| PDF (no curves) | 10 | _______ | [ ] |
| PDF (with curves) | 10 | _______ | [ ] |
| Excel (no curves) | 10 | _______ | [ ] |
| Excel (with curves) | 10 | _______ | [ ] |

**Pass/Fail**: ___________

---

#### Test 7.2: File Size Validation

**Objective**: Ensure file sizes are reasonable

| Format | Draft Points | Size (KB) | Reasonable |
|--------|--------------|-----------|------------|
| CSV | 10 | _______ | [ ] <20KB |
| JSON | 10 | _______ | [ ] <30KB |
| PDF (no curves) | 10 | _______ | [ ] <150KB |
| PDF (with curves) | 10 | _______ | [ ] <250KB |
| Excel (no curves) | 10 | _______ | [ ] <40KB |
| Excel (with curves) | 10 | _______ | [ ] <60KB |

**Pass/Fail**: ___________

---

### Phase 8: Edge Cases

#### Test 8.1: Special Characters in Vessel Name

**Objective**: Handle special characters correctly

**Steps**:
1. Create vessel with name: "Test & Vessel #1 (2025)"
2. Export to all formats

**Validation**:
- [ ] Filenames don't contain invalid characters
- [ ] Files download successfully
- [ ] Vessel name displays correctly in exports

**Pass/Fail**: ___________

---

#### Test 8.2: Very Long Vessel Name

**Objective**: Handle long names gracefully

**Steps**:
1. Create vessel with 100+ character name
2. Export to all formats

**Validation**:
- [ ] Filenames are truncated appropriately
- [ ] Files download successfully
- [ ] Names don't break layouts in PDF/Excel

**Pass/Fail**: ___________

---

#### Test 8.3: Vessel with No Description

**Objective**: Handle missing optional data

**Steps**:
1. Create vessel with no description
2. Export to PDF/Excel

**Validation**:
- [ ] Exports succeed
- [ ] Description field shows "N/A" or is blank
- [ ] No errors in console

**Pass/Fail**: ___________

---

## Summary Checklist

### Critical Tests (Must Pass)
- [ ] Test 1.1: CSV Export
- [ ] Test 1.2: JSON Export
- [ ] Test 1.3: PDF Export (No Curves)
- [ ] Test 1.4: Excel Export (No Curves)
- [ ] Test 2.1: PDF Export with Curves
- [ ] Test 2.2: Excel Export with Curves
- [ ] Test 3.1: Data Accuracy
- [ ] Test 3.2: Data Consistency
- [ ] Test 4.1: Export Without Results
- [ ] Test 5.1: Toast Notifications

### Important Tests (Should Pass)
- [ ] Test 4.2: Network Error Simulation
- [ ] Test 4.3: Large Dataset Export
- [ ] Test 5.2: Dialog Behavior
- [ ] Test 5.3: File Download Behavior
- [ ] Test 6.1: Chrome/Edge
- [ ] Test 7.1: Export Speed

### Nice-to-Have Tests
- [ ] Test 6.2: Firefox
- [ ] Test 6.3: Safari
- [ ] Test 7.2: File Size Validation
- [ ] Test 8.1-8.3: Edge Cases

---

## Test Results Summary

**Tester Name**: ___________  
**Test Date**: ___________  
**Environment**: [ ] Local [ ] Staging [ ] Production

**Overall Results**:
- **Total Tests**: 25
- **Passed**: _____ / 25
- **Failed**: _____ / 25
- **Skipped**: _____ / 25

**Critical Issues Found**: ___________

**Recommendation**: [ ] APPROVE FOR PRODUCTION [ ] NEEDS FIXES

---

## Issue Reporting Template

If you find issues, use this template:

```markdown
### Issue #X: [Brief Description]

**Severity**: [ ] Critical [ ] High [ ] Medium [ ] Low

**Test**: Test X.Y - [Test Name]

**Steps to Reproduce**:
1. 
2. 
3. 

**Expected**:
- 

**Actual**:
- 

**Screenshots**: [Attach if applicable]

**Browser**: 
**OS**: 
**Notes**: 
```

---

## Approval

**Tested By**: ___________  
**Date**: ___________  
**Signature**: ___________

**Approved for Production**: [ ] YES [ ] NO

**Notes**: ___________

---

**End of Manual Testing Guide**

*For automated testing, see: backend/DataService.Tests/Services/Hydrostatics/ExportServiceTests.cs*

