# PyPb

PyPb is a library that provides an object-centric, general-purpose wrapper around [Python.NET](https://github.com/pythonnet/pythonnet) intended to be used from PowerBuilder that allows it to interact with Python code. This library provides facilities for invoking module functions, instantiating classes, invoking methods setting properties, among others. It aims to give PowerBuilder developers an easy and convenient way to use popular Python libraries such as pandas, numpy, OpenCV, openpyxl and many more.

## Requirements

This library has the following software requirements:

| Component      | Version                                                      |
| -------------- | ------------------------------------------------------------ |
| PowerBuilder   | 2022 R3, 2025, 2025 R2                                       |
| .NET           | 8.0, 10.0                                                    |
| Python runtime | 3.11 - 3.13 (32-bit or 64-bit, depending on the bitness of the application) |

## Structure

This repository is organized as follows: 1

```
pypb-repo
├── README.md -- this document
├── demo                
│   ├── README.md -- documentation for the demo application
│   └── openpyxl-wrapper
└── src						    
	├── README.md -- documentation for the source code
    ├── CSharp					
    └── PowerBuilder			
        ├── Pb.Tests			
        └── PyPb.Pb
```

- demo - Contains the PowerBuilder application that demonstrates the utilization of the library
  - openpyxl-wrapper - Source code for the openpyxl-wrapper library which is used in the demo application to demonstrate a different approach to consuming Python code from PB
- src - Source code for the PyPb library
  - CSharp - C# solution with the source and test projects for the PyPb .NET components
  - PowerBuilder
    - PyPb.Pb - PowerBuilder source code (PBLs) for the PowerScript wrappers to the .NET components of the library.
    - Pb.Tests - Unit test project for the PyPb.Pb objects

The PyPb library is composed of the following main components:

- PyPb. This is the core library that performs the initialization of the Python environment and marshalling of operations between PowerBuilder and Python
- PyPb.Inspector. This library provides tools to get information about the members of a module, and the signature of functions or callables.
- PyPb.Utils. Standalone tool to get information about an existing system's Python environment (e.g. if python exists in path, the bitness of the python executable)



## PyPb Architecture Overview

The following diagram illustrates the high-level architecture and interaction flow between PowerBuilder, PyPb, Python.NET, and the Python runtime.

```
+------------------------------------------------------+
|                PowerBuilder Application              |
|  - Calls PyPb from PowerScript                       |
|  - Uses PB-friendly wrapper classes                  |
|  - Benefits from PB IDE code assist                  |
+------------------------------------------------------+
                            |
                            | PB wrapper call
                            v
+------------------------------------------------------+
|             PowerBuilder Wrapper Layer               |
|  PyPb.Pb                                             |
|  - PowerBuilder-side wrapper interface               |
|  - Exposes .NET functionality in PB-friendly form    |
|  - Hides underlying .NET/Python.NET complexity       |
+------------------------------------------------------+
                            |
                            | .NET interface call
                            v
+------------------------------------------------------+
|               .NET Integration Layer                 |
|  C# / PyPb .NET Components                           |
|  - Core implementation of the PyPb library           |
|  - Abstraction layer over Python.NET                 |
|  - Initializes Python environment                    |
|  - Handles interoperation and marshalling            |
|  - Provides inspection and utility services          |
+------------------------------------------------------+
                            |
                            | Python.NET bridge
                            v
+------------------------------------------------------+
|                    Python.NET Layer                  |
|  - Underlying interoperability library               |
|  - Connects .NET code with Python runtime            |
+------------------------------------------------------+
                            |
                            | Python execution
                            v
+------------------------------------------------------+
|                 Python Runtime / Modules             |
|  - Python interpreter                                |
|  - Imported Python modules and functions             |
|  - Actual Python code execution                      |
+------------------------------------------------------+
```

## Quick Start

### Installing Python

If you don't already have a compatible Python runtime in your system you can follow the next steps to obtain one.

1. Go to the [download page for Python 3.13.13](https://www.python.org/downloads/release/python-31313/) and download the installer for the distribution you want to use.
   Alternatively, you can use these links: [32-bit installer](https://www.python.org/ftp/python/3.13.13/python-3.13.13.exe) | [64-bit installer](https://www.python.org/ftp/python/3.13.13/python-3.13.13.exe).
   You must install the runtime with the same CPU architecture of your end application. I.e. if you're working on a 32-bit application you must use a 32-bit Python runtime. Conversely, for a 64-bit application you will need the 64-bit runtime.
2. Run the installer.
   It's recommended to install the runtime to a known, easy to access location. (e.g. C:\python313_32 or C:\python313_64)

### Using PyPbLib

The following guide will detail the simplest example of utilizing the PyPb library to access Python's modules from PowerBuilder and perform a very simple access property operation.

1. Import the `pypblib.pbd` and  `bin.pypb.appeon` directory into your workspace, then add the PBD into your library list.
2. Use the `f_pypbcontextinit` function to create an instance of a `n_cst_pypbcontext`:

```python
n_cst_pypbcontext lnv_context
string ls_error
lnv_context = f_pypbcontextinit("path to the python313.dll", ls_error)
If IsNull(lnv_context) Then
	MessageBox("Could not initialize Python context", ls_error)
	Return
End If
```

> Note: The path passed to the function should be the DLL that contains the 3 digit version number in the filename. e.g. *C:\python_313_32\python313.dll* 

3. Use the PyPbContext's `of_import` function to import the module that will be used from PowerBuilder (in this example, Python's `platform` module):

```python
n_cst_pypbmodule lnv_module
int res
res = lnv_context.of_import("platform", Ref lnv_module)
If res <> 0 Then
	MessageBox("Could not initialize Python module", lnv_context.of_lasterrormessage())
	Return
End If
```

4. Invoke the module's code with `of_invoke`. In this example, the `python_version()` function is being invoked:

```python
int res
n_cst_pypbobject lnv_result
res = lnv_module.of_invoke("python_version", lnv_result)
If res <> 0 Then
	MessageBox("Could not invoke function", lnv_module.of_lasterrormessage())
	Return
End If
```

5. Convert the returned object into a string to display it in PowerBuilder:
```python
string ls_result
res = lnv_result.of_tostring(ls_result)
If res <> 0 Then
	MessageBox("Could not convert value to string", lnv_result.of_lasterrormessage())
	Return
End If
MessageBox("Python Version", ls_result)
```

6. Access the module's `__version__` string attribute:

```python
int res
string ls_version
res = lnv_module.of_get("__version__", Ref ls_version)
If res <> 0 Then
	MessageBox("Could not access property", lnv_module.of_lasterrormessage())
	Return
End If
```

For learning more about PyPbLib's capabilites such as class instantiation and index accessing, refer to the [source code's intro page](src/README.md).

### Deploying

For deploying this library with your project all that needs to be done is copying the PBD and the respective dependencies folder to the executable's location. The target machine must have a compatible Python environment (or you can provide your own compact runtime).

For more details on deployment, check the [source code documentation](src/README.md#deployment).

## Demo

See [this document](./demo/README.md) for details about the included demo.

## PyPbLib

See [this document](./src/README.md) to learn how to use the PyPb library (PyPbLib) and understand its internal design. 

## License

See [LICENSE](License.txt) for details
