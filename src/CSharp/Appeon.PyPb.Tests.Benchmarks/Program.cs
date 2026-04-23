using Appeon.PyPb.Tests.Benchmarks;
using BenchmarkDotNet.Running;


#if DEBUG
var tests = new PyPbTests();
tests.AccessPropertiesPyPb();
tests.AccessPropertiesPythonNet();
tests.InvokeFunctionPyPb();
tests.InvokeFunctionPythonNet();
tests.InvokeFunctionDynamic();

#else
    BenchmarkRunner.Run<PyPbTests>();
#endif

Console.WriteLine("Press enter to exit");
Console.ReadLine();
