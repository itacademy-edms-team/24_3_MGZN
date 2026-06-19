using Xunit;

// Все integration-тесты делят один SQL Server — без параллелизма внутри сборки.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
