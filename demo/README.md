# Demo Tutorial

This demo provides examples that demonstrate multiple techniques for utilizing PyPb and an alternative approach for consuming Python code from PowerBuilder: a custom C# wrapper.

## Requirements

- PowerBuilder 2022 R3 or 2025 or 2025 R2
- Python 3.11-3.13 (CPU architecture depends on the bitness of your target application/IDE setting)

## Installing the runtime

The demo application requires a 64 bit Python runtime. You can follow the next instructions to install the highest supported version:

1. Go to the [Python page](https://www.python.org/downloads/) and download Python 3.13 64-bit. You can also use [this direct link](https://www.python.org/ftp/python/3.13.12/python-3.13.12-amd64.exe).
2. Execute the installer and install Python into a known location (e.g. `C:\python313`). 
3. After the installation is finished, open a CMD window on the demo directory (*DemoApp*).
4. Execute the following command:

```cmd
C:\python313\python.exe -m ensurepip
```

> Note: this command assumes the Python runtime was installed on the directory `C:\python313`. Replace the path if you installed it on a different location.

5. After the command finished executing, run the following (use the requirements file appropriate for your Python runtime's bitness):

```cmd
C:\python313\python.exe -m pip install -r py.examples\requirements-[32|64].txt

```

6. After the previous command completes, run the following to download the AI model used by the Remove Background example:

```powershell
C:\python313\python.exe py.examples\RemoveBackground.py
```

## Running the Demo

The first time the demo us run, it will require you to specify the path to a Python runtime DLL. Supported versions are from 3.11 (for PyPb.Inspector's `getmembers_static()`) to 3.13 (latest supported version by Python.NET at the time of writing). Python runtimes usually come with 2 versions of the DLL, e.g. `python3.dll` and `python313.dll`. You should always pick the one with the minor version (i.e. `python313.dll`). If initialization fails on this step, you will need to restart the entire application. If running on the IDE, you will need to restart the IDE. This is due to a limitation on Python.NET where even a failed initialization attempt makes changes to the internal state of the engine.

> Note: Running the demo on 32-bit, changing the architecture to 64-bit and running it again results in an unstable configuration. When changing the IDE's runtime architecture from 32 to 64-bit, restart the PowerBuilder IDE

### XLSX Writer

This example demonstrates the use of the python library [openpyxl](https://foss.heptapod.net/openpyxl/openpyxl) to create an XLSX worksheet from a DataWindow's data.

After running the application, select a color profile and interleave setting and click the *Export* button. Select a destination path and save. An XLSX file will be created with the specified settings. 

This application demonstrates two approaches for interacting with the Python runtime:

#### Using InvocationRequest

This approach configures an [InvocationRequest](#Using-InvocationRequest) object with the details of the function invcation/object construction and passes it to the object/module. It's used in the `pypbdemo.u_excel_writer::of_writedatatoworkbook` function.

#### Using the dynamically generated classes

When obtaining an object from any call to the PyPb.Net Library, a dynamic object is created with methods that have the same name as those defined in the Python object/module. These functions  can be called directly from the PowerBuilder code. This approach is demonstrated in the `pypbdemo.u_excel_writer::of_writedatatoworkbookstatic` function

You can change between these two approaches by commenting and uncommenting the appropriate code sections in the `u_excel_writer::of_writedatatoxlsx` function.

### XLSX Writer (Custom Wrapper)

This example is very similar to the previous one, except it uses a wrapper C# library specifically made for it providing an interface to perform exactly the needed operations, as opposed to the previous one where the general-purpose PyPb library was used. The main function that performs this functionality is `u_excel_writer_chart::of_writedatatoxlsxstatic`.

### Module Explorer

This example allows you to visualize the declared members of any Python module you select. There are some examples included in the *PyPb.Pb\py.examples* directory.

This demo makes use of the `Appeon.PyPb.Inspector` project which can be located in `src\CSharp\Appeon.PyPb.Inspector`. This project uses Python's `inspect` module through the *Python.NET* library to access a module's members and functions' signatures.

> Note: If the module to be loaded has references to external libraries, these libraries need to be already installed on the runtime selected when initializing the Demo. This is because Python's `inspect` library only works with live objects, and in order to create a live object from the selected module, it has to be loaded up.

### Web Crawler

This example uses a web crawler to retrieve the top repositories from GitHub and displays the front page articles in a DataWindow.

### Background Removal

This example uses the ML-powered [rembg](https://github.com/danielgatis/rembg) Python library to remove the background from a picture. Please note that due to it needing to do AI processing it might take a relatively long time to initialize and perform the operation. You can find a sample photo in the `res\stock_photos` directory.

The rembg library depends on packages that are only available in a 64-bit architecture, thus this example is disabled when running on 32-bit.

### Plotly Charts

This example makes use of [Plotly](https://plotly.com/python/) to showcase powerful and dynamic charts that can be generated locally and displayed on a PB window through the WebBrowser control.

The pandas library (a plotly dependency) is only available in a 64-bit architecture, thus this example is disabled when running on 32-bit.

### Alternative approaches

There are other ways to consume Python code in a PowerBuilder application. One of the most practical is to create a Web API that exposes the Python code you want to consume as a set of HTTP REST endpoints. You can connect to these endpoints by using PowerBuilder's RestClient or HttpClient.
This approach removes the need to have the user set up and configure a Python runtime on their machine, or for the developer to provide it with the executable files. Additionally, by having the Python processing reside on a completely different environment, processing-heavy tasks can be performed in a more controlled environment and not depend on the user's machine.

## Troubleshooting

### The module xxx could not be found

The module `xxx` needs to be installed on the selected Python runtime. Please follow the steps in [this document](https://pip.pypa.io/en/stable/user_guide/) page to install the missing packages. The requirement list mentions the packages needed for this demo.

### Error when trying to invoke function removeBackground. 'NoneType' object has on attribute 'write'

This error is most often caused by `rembg` not being able to locate the ML models necessary for its operation. Make sure you performed step 6 when [installing the runtime](#installing-the-runtime). The models should be downloaded to `<your user folder>\.u2net` (e.g. `"C:\Users\<user>\.u2net\u2net.onnx"`)

### The type initializer for 'Delegates' threw an exception

This issue occurs when the Python DLL selected is not the appropriate bitness for the process. If you run into this issue, you will need to restart the process (i.e. the IDE if running it from there).

### Python ABI \<some version number> is not supported

An unsupported version of the Python runtime was selected. Please check the [Requirements](#requirements) section and verify you're using the appropriate Python version. If you run into this issue, you will need to restart the process (i.e. the IDE if running it from there).

### Standard output redirection doesn't work 

Make sure the correct event name has been registered for the target. Additionally, if you're running from the IDE, make sure there is no dotnetinvoker.dll file in the `bin.pypb.appeon` directory. In contrast, if running straight from the EXE, make sure that file exists in that same folder. 
