ModuleScopeVariable = "Module"

class PyBTester:
    property1 = "test"
    property2 = 67
    
    def Add(self, arg1, arg2):
        print("Adding two entities...")
        return arg1 + arg2
    
    def AddPair(self, pair):
        print("Adding the elements of a pair together...")
        return pair.arg1 + pair.arg2
    
    def Print(self, *args):
        for arg in args:
            print(arg)
    
def createPyBTesterInstance():
    print("Creating PyBTester through function")
    return PyBTester()