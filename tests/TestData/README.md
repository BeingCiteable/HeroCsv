# FastCsv Test Data Files

This directory contains various CSV test files used for unit testing and benchmarking the FastCsv library.

## Basic Test Files

### `simple.csv` (4 lines, 3 records)
```csv
Name,Age,City
John,30,New York
Jane,25,Los Angeles
Bob,35,Chicago
```
Basic CSV with header, simple data types, no special characters.

### `no_header.csv` (5 lines, 5 records)
```csv
John,30,New York
Jane,25,Los Angeles
Bob,35,Chicago
Alice,28,Seattle
Charlie,32,Boston
```
CSV without header row, useful for testing `hasHeader: false` scenarios.

## Realistic Business Data

### `employees.csv` (11 lines, 10 records)
Employee database with mixed data types:
- ID (integer)
- Name (string)
- Department (string)
- Salary (decimal)
- HireDate (date)
- IsActive (boolean)

### `products.csv` (11 lines, 10 records)
Product catalog with complex fields:
- Quoted fields with commas inside
- Escaped quotes within quoted fields
- Mixed categories
- Price decimals

### `sales_data.csv` (11 lines, 10 records)
Sales transaction data:
- Date fields
- Regional data
- Revenue calculations
- Customer names

### `mixed_data_types.csv` (11 lines, 10 records)
Comprehensive data type testing:
- Integers, decimals, booleans
- Dates in standard format
- Letter grades
- Mixed positive/negative numbers

## Edge Cases

### `empty.csv` (0 bytes)
Completely empty file for testing empty input handling.

### `header_only.csv` (1 line, 0 records)
```csv
Name,Age,City
```
File with only header row, no data records.

### `with_empty_lines.csv` (7 lines, 3 records)
```csv
Name,Age,City

John,30,New York

Jane,25,Los Angeles

Bob,35,Chicago
```
CSV with empty lines between records to test line skipping.

## Special Character Support

### `special_characters.csv` (6 lines, 5 records)
International character support:
- Spanish: José García
- Chinese: 李小明
- French: François Dubois
- German: Müller Schmidt
- Russian: Анна Иванова
- Unicode symbols: ☕

### `quoted_fields.csv` (6 lines, 5 records)
Complex quoting scenarios:
- Fields with commas requiring quotes
- Escaped quotes within quoted fields
- Email addresses and phone numbers
- Address fields with commas

## Different Delimiters

### `different_delimiters.csv` (4 lines, 3 records)
Semicolon-delimited data for testing custom delimiters.

### `pipe_delimited.csv` (4 lines, 3 records)
Pipe-delimited data (|) for testing alternative separators.

### `tab_delimited.csv` (4 lines, 3 records)
Tab-separated values for TSV testing.

## Malformed Data

### `malformed.csv` (7 lines, 6 records)
Intentionally malformed CSV for error handling testing:
- Missing fields in some rows
- Extra fields in some rows
- Inconsistent field counts

## Performance Testing Files

### `large_dataset.csv` (11 lines, 10 records)
Small baseline dataset for performance comparisons.

### `medium_dataset.csv` (1001 lines, 1000 records)
Medium-sized dataset (1K records) for performance testing.
Generated with 10 columns of realistic employee data.

### `large_dataset_10k.csv` (10001 lines, 10000 records)
Large dataset (10K records) for stress testing.
**Note:** This file is ~800KB and may take time to process.

### `huge_dataset.csv` (100001 lines, 100000 records)
Huge dataset (100K records) for extreme performance testing.
**Note:** This file is ~8MB and is used for benchmarking only.

## File Generation

The large datasets are generated using `generate_large_csv.py`:

```bash
cd tests/TestData
python generate_large_csv.py
```

This script creates:
- Random employee data with realistic names
- Random departments, salaries, hire dates
- Geographic diversity (US cities/states)
- Consistent structure across all sizes

## Usage in Tests

Use the `TestDataHelper` class to access these files:

```csharp
// Read file content
var content = TestDataHelper.ReadTestFile(TestDataHelper.Files.Employees);

// Get file path
var path = TestDataHelper.GetTestFilePath(TestDataHelper.Files.Simple);

// Open as stream
using var stream = TestDataHelper.OpenTestFile(TestDataHelper.Files.Products);

// Check if file exists
if (TestDataHelper.TestFileExists(TestDataHelper.Files.HugeDataset))
{
    // Run performance test
}
```

## File Sizes

| File | Size | Records | Purpose |
|------|------|---------|---------|
| empty.csv | 0 B | 0 | Empty file handling |
| header_only.csv | 13 B | 0 | Header-only scenarios |
| simple.csv | 52 B | 3 | Basic functionality |
| employees.csv | 482 B | 10 | Business data types |
| products.csv | 1.2 KB | 10 | Complex quoting |
| medium_dataset.csv | ~80 KB | 1,000 | Performance testing |
| large_dataset_10k.csv | ~800 KB | 10,000 | Stress testing |
| huge_dataset.csv | ~8 MB | 100,000 | Benchmarking |

## Test Categories

1. **Basic Functionality**: simple.csv, no_header.csv
2. **Real-World Data**: employees.csv, products.csv, sales_data.csv
3. **Edge Cases**: empty.csv, header_only.csv, with_empty_lines.csv
4. **Character Encoding**: special_characters.csv
5. **Field Complexity**: quoted_fields.csv, mixed_data_types.csv
6. **Delimiter Variations**: different_delimiters.csv, pipe_delimited.csv, tab_delimited.csv
7. **Error Handling**: malformed.csv
8. **Performance**: medium_dataset.csv, large_dataset_10k.csv, huge_dataset.csv

This comprehensive test data ensures FastCsv works correctly across all real-world CSV scenarios.