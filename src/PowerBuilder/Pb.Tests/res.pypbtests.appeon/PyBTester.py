class PyBTester:
    property1 = "test"
    property2 = 67
    
    
    def Add(self, arg1, arg2):
        print("Adding two entities...")
        return arg1 + arg2
    
    def AddPair(self, pair):
        print("Adding the elements of a pair together...")
        return pair.arg1 + pair.arg2
    
    def getInt(self):
        return 42
    
    def getString(self):
        return "42"
    
    def getBool(self):
        return True
    
    def getDouble(self):
        return 4.2
    
    def getFloat(self):
        return 4.2
    
    def Print(self, *args):
        for arg in args:
            print(arg)
            
    def namedArguments(self, **kwargs):
        if "first" in kwargs:
            return 1
        if "second" in kwargs:
            return 2
        
        return 0
    
    def Throw(self):
        raise Exception("Manually thrown")
    
def createPyBTesterInstance():
    print("Creating PyBTester through function")
    return PyBTester()
    
def namedArguments(**kwargs):
    if "first" in kwargs:
        return 1
    if "second" in kwargs:
        return 2
    
    return 0

def Throw():
    raise Exception("Manually thrown")

array = [0, 1, 2, 3, 4]
dictionary = dict(first=1, second=2, third=3)