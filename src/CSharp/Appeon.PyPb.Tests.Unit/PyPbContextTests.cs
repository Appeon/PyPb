using Appeon.PyPb.Tests.Common;
using Appeon.PyPb.Tests.Unit.Facilities;

namespace Appeon.PyPb.Tests.Unit
{
    [TestCaseOrderer(
    ordererTypeName: "Appeon.PyPb.Tests.PriorityOrderer",
    ordererAssemblyName: "Appeon.PyPb.Tests")]
    [Collection("PyPbContext-dependent tests collection")]
    public class PyPbContextTests
    {
        private readonly ContextProvider contextProvider;

        public PyPbContextTests(ContextProvider context)
        {
            this.contextProvider = context;
        }


        [Fact]
        [TestPriority(-3)]
        public void CanInitializeContextFromPythonDotIncluded()
        {
            if (this.contextProvider.Context is not null)
                /// This is the only test that can initialize a PyPbContext
                /// If it exists, it's assume this test has already been called
                return;
            var context = PyPbContext.Init([], out var error);

            if (error is not null)
                throw new Exception(error);

            Assert.NotNull(context);

            this.contextProvider.Context = context;
        }

        [Fact]
        [TestPriority]
        public void CanImportSystemModule()
        {
            var module = contextProvider.Context.Import("os", out var error);

            if (error is not null)
                throw new Exception(error);
            Assert.NotNull(module);
        }

        [Fact]
        [TestPriority(-2)]
        public void CanImportPyModuleFromFile()
        {
            if (contextProvider.Module is not null)
                /// This is the only test that can initialize a PyPbModule
                /// If it exists, it's assume this test has already been called
                return;

            using var script = new TempTextFile($"PyBTester.py", ContextProvider.SamplePythonScript);

            var module = contextProvider.Context.LoadModule(script.Path, out var error);

            if (error is not null)
                throw new Exception(error);

            Assert.NotNull(module);

            contextProvider.Module = module;

        }


        [Fact]
        [TestPriority(-2)]
        public void CanImportPyModuleFromString()
        {
            if (contextProvider.Module is not null)
                /// This is the only test that can initialize a PyPbModule
                /// If it exists, it's assume this test has already been called
                return;

            var module = contextProvider.Context.LoadModuleFromString("PyBTester", ContextProvider.SamplePythonScript, out var error);

            if (error is not null)
                throw new Exception(error);

            Assert.NotNull(module);

            contextProvider.Module = module;

        }

        [Fact]
        [TestPriority]
        public void CanImportObjectFromModule()
        {
            var module = contextProvider.Context.FromImportObject("os", "replace", out var error);

            if (error is not null)
                throw new Exception(error);

            Assert.NotNull(module);
        }

        [Fact]
        [TestPriority]
        public void CanImportSubmodule()
        {
            var module = contextProvider.Context.FromImportModule("os", "sys", out var error);

            if (error is not null)
                throw new Exception(error);

            Assert.NotNull(module);
        }

        [Fact]
        [TestPriority]
        public void CanExecuteStatementWithoutLocals()
        {
            var res = contextProvider.Context.ExecuteStatement("list()", out var result, out var error);

            if (error is not null)
                throw new Exception(error);

            Assert.Equal(0, res);
            Assert.NotNull(result);

            result.GetMember("__class__", out var cls, out error);
            cls.GetMember("__name__", out var clsname, out error);
            Assert.Equal("list", clsname.ToString());


        }

        [Fact]
        [TestPriority]
        public void FailsToExecuteInvalidStatementWithoutLocals()
        {
            var res = contextProvider.Context.ExecuteStatement("isbnvsovkn()", out var result, out var error);

            Assert.NotEqual(0, res);
            Assert.Null(result);
            Assert.NotNull(error);
        }

        [Fact]
        [TestPriority]
        public void CanExecuteStatementWithLocals()
        {
            var request = new InvocationRequest("stub");
            request.AddNamedArgument("a", "abc123");

            var res = contextProvider.Context.ExecuteStatement("tuple(a)", request, out var result, out var error);

            if (error is not null)
                throw new Exception(error);

            Assert.Equal(0, res);
            Assert.NotNull(result);


            result.GetMember("__class__", out var cls, out error);
            cls.GetMember("__name__", out var clsname, out error);
            Assert.Equal("tuple", clsname.ToString());


        }

        [Fact]
        [TestPriority]
        public void FailsToExecuteInvalidStatementWithLocals()
        {
            var request = new InvocationRequest("stub");
            request.AddNamedArgument("a", "abc123");
            request.AddNamedArgument("b", "bcd234");

            var res = contextProvider.Context.ExecuteStatement("listasdasd(a, b)", request, out var result, out var error);

            Assert.NotEqual(0, res);
            Assert.Null(result);
            Assert.NotNull(error);
        }
    }
}