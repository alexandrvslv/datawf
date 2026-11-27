# DataWF.Benchmark.Invokers

This project contains benchmark tests for the `TestClass` and its properties, focusing on various invocation techniques such as constructors, properties, and fields. The benchmarks are implemented using different methods including direct calls, reflection, and expression trees.

## Project Structure

- **Benchmarks/TestInvokerBenchmarks.cs**: Contains benchmark tests for the `TestClass` and its properties.
- **Benchmarks/InvokerComparerListSorting.cs**: Compares multi-key sorting with `InvokerComparerList<TestClass>` against LINQ `OrderBy`/`ThenBy`.
- **Common/TestClass.cs**: Defines the `TestClass` model with properties of all types.
- **Common/TestStruct.cs**: Defines the `TestStruct` model with properties of all types.
- **Program.cs**: Entry point for the benchmark application, responsible for running benchmarks and displaying results.
- **DataWF.Benchmark.Invokers.csproj**: Project file containing configuration settings, dependencies, and build options.

## Running the Benchmarks

To run the benchmarks, follow these steps:

1. Ensure you have the .NET SDK installed on your machine.
2. Open a terminal and navigate to the project directory.
3. Run the benchmarks using the following command:

   ```
   sudo dotnet run -c Release
   ```

4. Review the output in the console to see the benchmark results.

To run only the comparer-list sorting benchmarks:

```
dotnet run -c Release -- --filter "*InvokerComparerListSorting*"
```

## Contributing

Contributions to the project are welcome. Please feel free to submit issues or pull requests for any improvements or additional benchmarks.

## License

This project is licensed under the MIT License. See the LICENSE file for more details.