# Requires: Windows with Microsoft Excel installed
# Usage (PowerShell):
#   powershell -ExecutionPolicy Bypass -File .\build_excel.ps1

param(
    [string]$OutputPath = "Marketing_ChinchillaAparts.xlsx",
    [string]$FactCsv = "Fact_RuStore_Installs.csv",
    [string]$MediaPlanCsv = "MediaPlan.csv",
    [string]$AsoCsv = "ASO_Checklist.csv",
    [string]$SummaryCsv = "Summary.csv"
)

function Import-CsvIntoSheet {
    param(
        [Parameter(Mandatory=$true)] [object]$Worksheet,
        [Parameter(Mandatory=$true)] [string]$CsvPath
    )
    if (-not (Test-Path -LiteralPath $CsvPath)) {
        throw "CSV not found: $CsvPath"
    }
    $resolved = (Resolve-Path -LiteralPath $CsvPath).Path
    Write-Host "[Import]" $resolved
    $conn = "TEXT;" + $resolved
    $qt = $Worksheet.QueryTables.Add($conn, $Worksheet.Range("A1"))
    $qt.TextFilePlatform = 65001            # UTF-8
    $qt.TextFileParseType = 1               # xlDelimited
    $qt.TextFileCommaDelimiter = $true
    $qt.TextFileOtherDelimiter = $false
    $qt.AdjustColumnWidth = $true
    $qt.PreserveFormatting = $true
    $qt.Refresh()
    # Remove external connection to keep workbook clean
    $qt.Delete()
}

$excel = $null
try {
    Write-Host "[Start] Building Excel workbook..."
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false

    $wb = $excel.Workbooks.Add()

    # Use existing first three sheets, then add the fourth
    $wsFact = $wb.Worksheets.Item(1)
    $wsFact.Name = "Fact_RuStore_Installs"
    Import-CsvIntoSheet -Worksheet $wsFact -CsvPath $FactCsv

    $wsMedia = $wb.Worksheets.Item(2)
    $wsMedia.Name = "MediaPlan"
    Import-CsvIntoSheet -Worksheet $wsMedia -CsvPath $MediaPlanCsv

    $wsAso = $wb.Worksheets.Item(3)
    $wsAso.Name = "ASO_Checklist"
    Import-CsvIntoSheet -Worksheet $wsAso -CsvPath $AsoCsv

    $wsSummary = $wb.Worksheets.Add()
    $wsSummary.Move($wb.Worksheets.Item(1)) # move to front if desired
    $wsSummary.Name = "Summary"
    Import-CsvIntoSheet -Worksheet $wsSummary -CsvPath $SummaryCsv

    # Ensure formulas exist in Summary (column B rows 3-7)
    $wsSummary.Cells.Item(3,2).Formula  = "=SUM(Fact_RuStore_Installs!B2:B1048576)"
    $wsSummary.Cells.Item(4,2).Formula  = "=ROUND(AVERAGE(Fact_RuStore_Installs!B2:B1048576),1)"
    $wsSummary.Cells.Item(5,2).Formula  = "=MIN(Fact_RuStore_Installs!B2:B1048576)"
    $wsSummary.Cells.Item(6,2).Formula  = "=MAX(Fact_RuStore_Installs!B2:B1048576)"
    $wsSummary.Cells.Item(7,2).Formula  = "=ROUND(AVERAGE(Fact_RuStore_Installs!B2:B1048576)*7,0)"

    # Save workbook
    $fullOut = Join-Path -Path (Get-Location).Path -ChildPath $OutputPath
    $xlOpenXMLWorkbook = 51  # .xlsx
    $wb.SaveAs($fullOut, $xlOpenXMLWorkbook)
    Write-Host "[Done] Saved:" $fullOut
}
catch {
    Write-Error $_
    exit 1
}
finally {
    if ($wb) { $wb.Close($true) | Out-Null }
    if ($excel) { $excel.Quit() }
}


