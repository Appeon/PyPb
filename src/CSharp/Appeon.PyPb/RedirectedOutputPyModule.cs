using Python.Runtime;
using System.Transactions;

namespace Appeon.PyPb;

public class RedirectedOutputPyModule : PyModule
{
    private readonly Action<string>? callback;

    public RedirectedOutputPyModule(Action<string>? callback) : base()
    {
        this.callback = callback;

        if (callback is not null)
            InjectRedirection();
    }

    private void InjectRedirection()
    {
        Exec(
            """
import sys

class NetConsole(object):
    def __init__(self, writeCallback):
        self.writeCallback = writeCallback

    def write(self, message):
        self.writeCallback(message)

    def flush(self):
        # this flush method is needed for python 3 compatibility.
        # this handles the flush command by doing nothing.
        # you might want to specify some extra behavior here.
        pass

def setConsoleOut(writeCallback):
    sys.stdout = NetConsole(writeCallback)
"""
            );

        dynamic setConsoleOutFn = Eval("setConsoleOut");

        setConsoleOutFn(callback);
    }
}
