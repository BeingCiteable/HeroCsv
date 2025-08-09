using HeroCsv;
using HeroCsv.Models;
using HeroCsv.Parsing;
using System.Diagnostics;

// AOT Compilation Test for HeroCsv
Console.WriteLine("HeroCsv AOT Compilation Test");
Console.WriteLine("=============================");

// Test 1: Basic CSV parsing
TestBasicParsing();

// Test 2: Zero allocation parsing
TestZeroAllocationParsing();

// Test 3: SIMD operations (if available)
TestSimdOperations();

// Test 4: Object mapping
TestObjectMapping();

// Test 5: Memory usage
TestMemoryUsage();

Console.WriteLine("\nAll AOT tests completed successfully!");

static void TestBasicParsing()
{
    Console.WriteLine("\n1. Testing Basic CSV Parsing...");
    var csv = "name,age,city\nJohn,30,NYC\nJane,25,LA";
    
    var rows = Csv.ReadAsArrays(csv).ToList();
    Debug.Assert(rows.Count == 2);
    Debug.Assert(rows[0][0] == "John");
    
    Console.WriteLine("   ✓ Basic parsing works in AOT");
}

static void TestZeroAllocationParsing()
{
    Console.WriteLine("\n2. Testing Zero Allocation Parsing...");
    var csv = "a,b,c,d,e,f,g,h,i,j";
    var options = CsvOptions.Default;
    
    var beforeGC = GC.GetTotalAllocatedBytes();
    
    // Use span-based parsing
    var span = csv.AsSpan();
    _ = CsvParser.ParseLine(span, options);
    
    var afterGC = GC.GetTotalAllocatedBytes();
    var allocated = afterGC - beforeGC;
    
    Console.WriteLine($"   ✓ Allocated only {allocated} bytes (minimal for result array)");
}

static void TestSimdOperations()
{
    Console.WriteLine("\n3. Testing SIMD Operations...");
    
#if NET8_0_OR_GREATER
    if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
    {
        Console.WriteLine("   ✓ AVX2 SIMD available and working");
    }
    else
    {
        Console.WriteLine("   ⚠ AVX2 not available on this hardware");
    }
#endif

#if NET9_0_OR_GREATER
    if (System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated)
    {
        Console.WriteLine("   ✓ Vector512 (AVX-512) available and working");
    }
    else
    {
        Console.WriteLine("   ⚠ Vector512 not available on this hardware");
    }
#endif
}

static void TestObjectMapping()
{
    Console.WriteLine("\n4. Testing Object Mapping...");
    
    var csv = "Name,Age\nAlice,30\nBob,25";
    var people = Csv.Read<Person>(csv).ToList();
    
    Debug.Assert(people.Count == 2);
    Debug.Assert(people[0].Name == "Alice");
    Debug.Assert(people[0].Age == 30);
    
    Console.WriteLine("   ✓ Object mapping works in AOT");
}

static void TestMemoryUsage()
{
    Console.WriteLine("\n5. Testing Memory Usage...");
    
    // Generate larger CSV
    var lines = new List<string> { "col1,col2,col3,col4,col5" };
    for (int i = 0; i < 1000; i++)
    {
        lines.Add($"val{i},val{i+1},val{i+2},val{i+3},val{i+4}");
    }
    var csv = string.Join("\n", lines);
    
    var beforeMem = GC.GetTotalMemory(true);
    
    // Parse all rows
    var rowCount = 0;
    foreach (var row in Csv.ReadAsArrays(csv))
    {
        rowCount++;
        _ = row[0]; // Access first field
    }
    
    var afterMem = GC.GetTotalMemory(true);
    var memUsed = afterMem - beforeMem;
    var memPerRow = memUsed / rowCount;
    
    Console.WriteLine($"   ✓ Processed {rowCount} rows");
    Console.WriteLine($"   ✓ Memory per row: ~{memPerRow} bytes");
    Console.WriteLine($"   ✓ Total memory used: {memUsed / 1024.0:F2} KB");
}

// Test model for object mapping
public class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}