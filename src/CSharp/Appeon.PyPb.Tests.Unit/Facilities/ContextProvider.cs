using Appeon.PyPb.Inspector;
using Appeon.PyPb.Tests.Common;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Appeon.PyPb.Tests.Unit.Facilities;

public class ContextProvider
{
    public const string SamplePythonScript = "class PyBTester:\r\n    property1 = \"test\"\r\n    property2 = 67\r\n    \r\n    \r\n    def Add(self, arg1, arg2):\r\n        print(\"Adding two entities...\")\r\n        return arg1 + arg2\r\n    \r\n    def AddPair(self, pair):\r\n        print(\"Adding the elements of a pair together...\")\r\n        return pair.arg1 + pair.arg2\r\n    \r\n    def getInt(self):\r\n        return 42\r\n    \r\n    def getIntString(self):\r\n        return \"42\"\r\n    \r\n    def getString(self):\r\n        return \"abc\"\r\n    \r\n    def getBool(self):\r\n        return True\r\n    \r\n    def getDouble(self):\r\n        return 4.2\r\n    \r\n    def getFloat(self):\r\n        return 4.2\r\n    \r\n    def Print(self, *args):\r\n        for arg in args:\r\n            print(arg)\r\n            \r\n    def namedArguments(self, **kwargs):\r\n        if \"first\" in kwargs:\r\n            return 1\r\n        if \"second\" in kwargs:\r\n            return 2\r\n        \r\n        return 0\r\n    \r\ndef createPyBTesterInstance():\r\n    print(\"Creating PyBTester through function\")\r\n    return PyBTester()\r\n    \r\ndef namedArguments(**kwargs):\r\n    if \"first\" in kwargs:\r\n        return 1\r\n    if \"second\" in kwargs:\r\n        return 2\r\n    \r\n    return 0\r\n\r\narray = [0, 1, 2, 3, 4]\r\ndictionary = dict(first=1, second=2, third=3)";


    private PyPbContext? context;
    private PyPbModule? module;
    private PyPbObject? @object;

    public PyPbContext Context
    {
        get
        {
            if (context is null)
                CreateContext();
            return context;
        }

        set => context = value;
    }
    public PyPbModule Module
    {
        get
        {
            if (module is null)
                CreateModule();
            return module;
        }

        set => module = value;
    }
    public PyPbObject Object
    {
        get
        {
            if (@object is null)
                CreateObject();
            return @object;
        }

        set => @object = value;
    }

    private PythonInspector? inspector;

    public PythonInspector Inspector
    {
        get
        {
            if (inspector is null)
                CreateInspector();

            return inspector;
        }
        set => inspector = value;
    }


    [MemberNotNull(nameof(context))]
    private void CreateContext()
    {
        if (this.context is not null)
            return;
        var context = PyPbContext.Init([], out var error);

        if (error is not null)
            throw new Exception(error);

        if (context is null)
            throw new NullReferenceException("Context could not be initialized");

        this.context = context;
    }

    [MemberNotNull(nameof(module))]
    private void CreateModule()
    {

        if (this.module is not null)
            return;
        CreateContext();

        using var script = new TempTextFile($"PyBTester.py", ContextProvider.SamplePythonScript);

        var module = context.LoadModule(script.Path, out var error);
        if (error is not null)
            throw new Exception(error);

        if (module is null)
            throw new NullReferenceException("Module could not be initialized");

        this.module = module;
    }

    [MemberNotNull(nameof(@object))]
    private void CreateObject()
    {
        CreateModule();

        int res = module.Instantiate("PyBTester", out var @object, out var error);
        if (error is not null)
            throw new Exception(error);

        if (@object is null)
            throw new NullReferenceException("Object could not be initialized");

        this.@object = @object;
    }

    [MemberNotNull(nameof(inspector))]
    private void CreateInspector()
    {
        CreateContext();

        var result = PythonInspectorBuilder.Build(context, out var inspector, out var error);

        if (error is not null)
            throw new Exception(error);

        if (inspector is null)
            throw new NullReferenceException("Could not initialize PythonInspector");

        this.inspector = inspector;
    }
}
