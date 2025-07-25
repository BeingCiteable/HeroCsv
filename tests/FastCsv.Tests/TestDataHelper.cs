using System.IO;
using System.Reflection;

namespace FastCsv.Tests;

/// <summary>
/// Helper class for accessing test CSV files
/// </summary>
public static class TestDataHelper
{
    private static readonly string TestDataDirectory;

    static TestDataHelper()
    {
        // Get the directory where the test assembly is located
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        
        // Navigate to the TestData directory
        TestDataDirectory = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "TestData");
        TestDataDirectory = Path.GetFullPath(TestDataDirectory);
    }

    /// <summary>
    /// Gets the full path to a test CSV file
    /// </summary>
    /// <param name="fileName">Name of the CSV file (without path)</param>
    /// <returns>Full path to the test file</returns>
    public static string GetTestFilePath(string fileName)
    {
        var fullPath = Path.Combine(TestDataDirectory, fileName);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Test file not found: {fullPath}");
        }
        return fullPath;
    }

    /// <summary>
    /// Reads the content of a test CSV file
    /// </summary>
    /// <param name="fileName">Name of the CSV file</param>
    /// <returns>File content as string</returns>
    public static string ReadTestFile(string fileName)
    {
        return File.ReadAllText(GetTestFilePath(fileName));
    }

    /// <summary>
    /// Opens a test CSV file as a stream
    /// </summary>
    /// <param name="fileName">Name of the CSV file</param>
    /// <returns>FileStream for the test file</returns>
    public static FileStream OpenTestFile(string fileName)
    {
        return File.OpenRead(GetTestFilePath(fileName));
    }

    /// <summary>
    /// Checks if a test file exists
    /// </summary>
    /// <param name="fileName">Name of the CSV file</param>
    /// <returns>True if the file exists</returns>
    public static bool TestFileExists(string fileName)
    {
        var fullPath = Path.Combine(TestDataDirectory, fileName);
        return File.Exists(fullPath);
    }

    // Predefined test file names for easy access
    public static class Files
    {
        public const string Simple = "simple.csv";
        public const string NoHeader = "no_header.csv";
        public const string Employees = "employees.csv";
        public const string Products = "products.csv";
        public const string SalesData = "sales_data.csv";
        public const string Empty = "empty.csv";
        public const string HeaderOnly = "header_only.csv";
        public const string WithEmptyLines = "with_empty_lines.csv";
        public const string SpecialCharacters = "special_characters.csv";
        public const string QuotedFields = "quoted_fields.csv";
        public const string MixedDataTypes = "mixed_data_types.csv";
        public const string LargeDataset = "large_dataset.csv";
        public const string MediumDataset = "medium_dataset.csv";
        public const string LargeDataset10K = "large_dataset_10k.csv";
        public const string HugeDataset = "huge_dataset.csv";
        public const string DifferentDelimiters = "different_delimiters.csv";
        public const string PipeDelimited = "pipe_delimited.csv";
        public const string TabDelimited = "tab_delimited.csv";
        public const string Malformed = "malformed.csv";
        public const string FinancialData = "financial_data.csv";
        public const string ScientificData = "scientific_data.csv";
        public const string SportsStatistics = "sports_statistics.csv";
        public const string InventoryTracking = "inventory_tracking.csv";
    }

    /// <summary>
    /// Gets information about all available test files
    /// </summary>
    /// <returns>Array of test file information</returns>
    public static FileInfo[] GetAllTestFiles()
    {
        var directory = new DirectoryInfo(TestDataDirectory);
        return directory.GetFiles("*.csv");
    }

    /// <summary>
    /// Gets the size of a test file in bytes
    /// </summary>
    /// <param name="fileName">Name of the CSV file</param>
    /// <returns>File size in bytes</returns>
    public static long GetTestFileSize(string fileName)
    {
        var fileInfo = new FileInfo(GetTestFilePath(fileName));
        return fileInfo.Length;
    }
}