using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Diagnostics;
using System.Threading;

namespace PerfExec
{
    class Program
    {

        static void PerfGather(string categoryName, string target)
        {
            PerformanceCounter.CloseSharedResources();
            PerformanceCounterCategory category = null;
            if (target != String.Empty)
            {
                Console.WriteLine("Remote checks against {0}", target);
                foreach (PerformanceCounterCategory cat in PerformanceCounterCategory.GetCategories(target))
                {
                    string[] counterNames = null;
                    string[] instanceNames = null;
                    PerformanceCounterCategory counterCategory = new PerformanceCounterCategory(cat.CategoryName, target);
                    try
                    {
                        counterNames = counterCategory.GetCounters().Select(c => c.CounterName).ToArray();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0} did not have any counters", cat.CategoryName);
                    }


                    instanceNames = counterCategory.GetInstanceNames();
                    Console.WriteLine("[+] Category Name: {0}", cat.CategoryName);
                    //Console.WriteLine("  {0}", cat.CategoryHelp);
                    //Console.WriteLine("  {0}",  cat.CategoryType);
                    if (counterNames != null)
                    {
                        Console.WriteLine("\t[+]COUNTER NAMES");
                        foreach (string counterName in counterNames)
                        {
                            Console.WriteLine("\t  {0}", counterName);
                        }
                    }

                    if (instanceNames != null)
                    {
                        Console.WriteLine("\t[+]INSTANCE NAMES");
                        foreach (string instanceName in instanceNames)
                        {
                            Console.WriteLine("\t  {0}", instanceName);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Local checks");
                foreach (PerformanceCounterCategory cat in PerformanceCounterCategory.GetCategories())
                {
                    string[] counterNames = null;
                    string[] instanceNames = null;
                    PerformanceCounterCategory counterCategory = new PerformanceCounterCategory(cat.CategoryName);
                    try
                    {
                        counterNames = counterCategory.GetCounters().Select(c => c.CounterName).ToArray();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[-] {0} did not have any counters", cat.CategoryName);
                    }


                    instanceNames = counterCategory.GetInstanceNames();
                    Console.WriteLine("[+] Category Name: {0}", cat.CategoryName);
                    //Console.WriteLine("  {0}", cat.CategoryHelp);
                    //Console.WriteLine("  {0}",  cat.CategoryType);
                    if (counterNames != null)
                    {
                        Console.WriteLine("\t[+]Counter Names");
                        foreach (string counterName in counterNames)
                        {
                            Console.WriteLine("\t      {0}", counterName);
                        }
                    }

                    if (instanceNames != null)
                    {
                        Console.WriteLine("\t[+]Instance Names");
                        foreach (string instanceName in instanceNames)
                        {
                            Console.WriteLine("\t      {0}", instanceName);
                        }
                    }
                }
            }
        }

        static void DiagnosticRun(string category, string counter, string instance, string target)
        {
            Console.WriteLine("[+] Remote Registry and DCOM");
            try
            {
                PerformanceCounter targetCounter = null;
                if (target == string.Empty)
                {
                    Console.WriteLine("Local machine");
                    targetCounter = new PerformanceCounter(category, counter, instance);
                    Thread.Sleep(5000);
                }
                else
                {
                    Console.WriteLine("Remote machine");
                    targetCounter = new PerformanceCounter(category, counter, instance, target);
                    Thread.Sleep(5000);
                }
                float gcTimePercentage = targetCounter.NextValue();
                Console.WriteLine("Counter data  : " + gcTimePercentage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured but thats expected");
                Console.WriteLine(ex.Message);
            }
        }

        static void WMIPerf(ManagementScope scope, string categoryName, string counterName, string objectName, string target)
        {
            Console.WriteLine("[+] Using WMI");

            // Probably refresh but requires using COM
            //WbemScripting.SWbemRefresher refresher = new WbemScripting.SWbemRefresher();

            string query = $"SELECT {counterName} FROM Win32_PerfRawData_{categoryName}_{objectName}";
            ObjectQuery wmiquery = new ObjectQuery(query);

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, wmiquery);

                //WbemScripting.SWbemServicesEx sx = new WbemScripting.SWbemServicesEx();
                //refresher.AddEnum(sx, "Win32_PerfFormattedData");
                ManagementObjectCollection results = searcher.Get();


                Console.WriteLine("Query executed");
                foreach (ManagementObject obj in results)
                {
                    string instanceName = obj["Name"].ToString();
                    ulong value = (ulong)obj["CounterValue"];

                    Console.WriteLine($"Instance: {instanceName}, Value: {value}");
                }

            }
            catch (ManagementException ex)
            {
                Console.WriteLine($"[-] Error: {ex.Message}");
                Console.WriteLine("[*] Error expected here");
            }
        }

        static ManagementScope WMIConnect(string host, string username, string password)
        {
            string wmiNameSpace = "root\\CIMv2";
            ConnectionOptions options = new ConnectionOptions();
            Console.WriteLine("[+] Connecting to target : {0}", host);
            if (!String.IsNullOrEmpty(username))
            {
                Console.WriteLine("[+]  User credentials               : {0}", username);
                options.Username = username;
                options.Password = password;
            }
            Console.WriteLine();
            ManagementScope scope = new ManagementScope(String.Format("\\\\{0}\\{1}", host, wmiNameSpace), options);
            try
            {
                scope.Connect();
                Console.WriteLine("[+]  WMI connection established");
                return scope;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-]  Failed to connect to WMI: {0}", ex.Message);
                return null;
            }
        }

        static void UpdateRegValues(ManagementScope scope, string serviceName, Dictionary<string, string> updateRegKeys)
        {
            try
            {
                string subkeyName = string.Format("SYSTEM\\CurrentControlSet\\Services\\{0}\\Performance", serviceName);
                ManagementClass reg = new ManagementClass(scope, new ManagementPath("StdRegProv"), null);

                Console.WriteLine("[+] Updating {0} Reg values", subkeyName);

                // Im going to make the assumption that each library is a REG_EXPAND_SZ right now
                foreach (KeyValuePair<string, string> regval in updateRegKeys)
                {
                    if(regval.Key == "Library")
                    {
                        ManagementBaseObject inParamTwo = reg.GetMethodParameters("SetExpandedStringValue");
                        inParamTwo["hDefKey"] = 0x80000002;
                        inParamTwo["sSubKeyName"] = subkeyName;
                        inParamTwo["sValueName"] = regval.Key;
                        inParamTwo["sValue"] = regval.Value;
                        //ManagementBaseObject outParams4 = reg.InvokeMethod("SetStringValue", inParamTwo, null);
                        ManagementBaseObject outParams4 = reg.InvokeMethod("SetExpandedStringValue", inParamTwo, null);
                        Console.WriteLine("  [+] Updated {0}:{1} value to {2}", subkeyName, regval.Key, regval.Value);
                    }
                    else
                    {
                        ManagementBaseObject inParamTwo = reg.GetMethodParameters("SetStringValue");
                        inParamTwo["hDefKey"] = 0x80000002;
                        inParamTwo["sSubKeyName"] = subkeyName;
                        inParamTwo["sValueName"] = regval.Key;
                        inParamTwo["sValue"] = regval.Value;
                        //ManagementBaseObject outParams4 = reg.InvokeMethod("SetStringValue", inParamTwo, null);
                        ManagementBaseObject outParams4 = reg.InvokeMethod("SetStringValue", inParamTwo, null);
                        Console.WriteLine("  [+] Updated {0}:{1} value to {2}", subkeyName, regval.Key, regval.Value);
                    }
                }
                //Not a requirement but we'd like to have the same open value for our DLL so we dont have to build based on the name
                //Console.WriteLine("Updating Open Value");
                //ManagementBaseObject inThree = reg.GetMethodParameters("SetDWORDValue");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured : {0}", ex);
            }

        }

        static Dictionary<string, string> ReadRegValues(ManagementScope scope, string serviceName)
        {

            // Two uses cases: create a new registry entry (not yet tested), update existing

            //List<ManagementBaseObject> originalstate = new List<ManagementBaseObject>();
            Dictionary<string, string> originalstate = new Dictionary<string, string>();
            try
            {
                //Get original value
                string subkeyName = string.Format("SYSTEM\\CurrentControlSet\\Services\\{0}", serviceName);
                ManagementClass reg = new ManagementClass(scope, new ManagementPath("StdRegProv"), null);
                ManagementBaseObject inParams = reg.GetMethodParameters("EnumKey");
                inParams["hDefKey"] = 0x80000002;
                inParams["sSubKeyName"] = subkeyName;
                ManagementBaseObject outParams = reg.InvokeMethod("EnumKey", inParams, null);
                string[] skeyArray = (string[])outParams["sNames"];

                Console.WriteLine("[+] Checking sub keys in HKLM\\{0}\n", subkeyName);
                foreach (string subKey in skeyArray)
                {
                    Console.WriteLine(subKey);
                }

                string performanceKey = string.Format("{0}\\Performance", subkeyName);
                ManagementBaseObject inTwo = reg.GetMethodParameters("EnumValues");
                inTwo["hDefKey"] = 0x80000002;
                inTwo["sSubKeyName"] = performanceKey;
                ManagementBaseObject outTwo = reg.InvokeMethod("EnumValues", inTwo, null);

                string[] arrValueNames = (string[])outTwo["sNames"];
                UInt32[] arrTypes = (UInt32[])outTwo["Types"];

                Dictionary<string, UInt32> valueTypes = new Dictionary<string, UInt32>();
                for (int i = 0; i < arrValueNames.Length; i++)
                {
                    valueTypes.Add(arrValueNames[i], arrTypes[i]);
                }

                Console.WriteLine("[+] Getting Performance values\n");
                Console.WriteLine("Name - Type - Data");
                foreach (KeyValuePair<string, UInt32> value in valueTypes)
                {
                    string valueName = value.Key;
                    UInt32 valueType = value.Value;
                    string valueData = String.Empty;

                    Console.WriteLine($"Value Name: {valueName}");
                    Console.WriteLine($"Value Type: {valueType}");

                    ManagementBaseObject getParams = reg.GetMethodParameters("GetStringValue");
                    getParams["hDefKey"] = 0x80000002;
                    getParams["sSubKeyName"] = performanceKey;
                    getParams["sValueName"] = valueName;

                    if (valueType == 1) // REG_SZ
                    {
                        ManagementBaseObject getStringOutParams = reg.InvokeMethod("GetStringValue", getParams, null);
                        valueData = getStringOutParams["sValue"].ToString();
                        Console.WriteLine($"Value Data: {valueData}");
                    }
                    else if (valueType == 2) // REG_EXPAND_SZ
                    {
                        ManagementBaseObject getStringOutParams = reg.InvokeMethod("GetExpandedStringValue", getParams, null);
                        valueData = getStringOutParams["sValue"].ToString();
                        Console.WriteLine($"Value Data: {valueData}");
                    }
                    else if (valueType == 3) // REG_BINARY
                    {
                        ManagementBaseObject getBinaryOutParams = reg.InvokeMethod("GetBinaryValue", getParams, null);
                        Byte[] valueByteData = (Byte[])getBinaryOutParams["uValue"];
                        Console.WriteLine($"Value Data: {BitConverter.ToString(valueByteData).Replace("-", "")}");
                    }
                    else if (valueType == 4)
                    {
                        ManagementBaseObject getDWordOutParams = reg.InvokeMethod("GetDWordValue", getParams, null);
                        UInt32 valueByteData = (UInt32)getDWordOutParams["uValue"];
                        Console.WriteLine($"Value Data: {valueByteData}");
                    }

                    if(valueName == "Library" || valueName == "Open" || valueName == "Collect" || valueName == "Close")
                    {
                        originalstate.Add(valueName, valueData);
                    }
                    
                    Console.WriteLine();
                }
                return originalstate;
            }
            catch (Exception ex)
            {
                Console.WriteLine("error    : {0}", ex);
                return null;
            }
        }

        static void Main(string[] args)
        {
            var arguments = new Dictionary<string, string>();
            foreach (string argument in args)
            {
                int idx = argument.IndexOf('=');
                if (idx > 0)
                    arguments[argument.Substring(0, idx)] = argument.Substring(idx + 1);
            }

            string servicename = string.Empty;
            string categoryname = string.Empty;
            string countername = string.Empty;
            string instancename = string.Empty;
            string computername = string.Empty;
            string objectname = string.Empty;
            string dllPath = string.Empty;
            string username = string.Empty;
            string password = string.Empty;
            string openrv = string.Empty;
            string collectrv = string.Empty;
            string closerv = string.Empty;
            Dictionary<string, string> newValues = new Dictionary<string, string>();

            if (arguments.ContainsKey("username"))
            {
                if (!arguments.ContainsKey("password"))
                {
                    Console.WriteLine("Your args are incorrect");
                    return;
                }
                else
                {
                    username = arguments["username"];
                    password = arguments["password"];
                }
            }
            if (arguments.ContainsKey("category"))
            {
                categoryname = arguments["category"];
            }
            if (arguments.ContainsKey("counter"))
            {
                countername = arguments["counter"];
            }
            if (arguments.ContainsKey("instance"))
            {
                instancename = arguments["instance"];
            }
            if (arguments.ContainsKey("computername"))
            {
                computername = arguments["computername"];
            }
            if (arguments.ContainsKey("object"))
            {
                objectname = arguments["object"];
            }
            if (arguments.ContainsKey("dllpath"))
            {
                dllPath = arguments["dllpath"];
                newValues.Add("Library", dllPath);
            }
            if (arguments.ContainsKey("service"))
            {
                servicename = arguments["service"];
            }
            if (arguments.ContainsKey("open"))
            {
                openrv = arguments["open"];
                newValues.Add("Open", openrv);
            }
            if (arguments.ContainsKey("collect"))
            {
                collectrv = arguments["collect"];
                newValues.Add("Collect", collectrv);
            }
            if (arguments.ContainsKey("close"))
            {
                closerv = arguments["close"];
                newValues.Add("Close", closerv);
            }


            if (arguments.ContainsKey("read"))
            {
                ManagementScope wmiConn = WMIConnect(computername, username, password);
                Dictionary<string, string> originalValues = ReadRegValues(wmiConn, servicename);
            }
            else if (arguments.ContainsKey("updatereg"))
            {
                ManagementScope wmiConn = WMIConnect(computername, username, password);
                UpdateRegValues(wmiConn, servicename, newValues);
            }
            else if (arguments.ContainsKey("gather"))
            {
                PerfGather(categoryname, computername);
            }
            else if (arguments.ContainsKey("wmirun"))
            {
                ManagementScope wmiConn = WMIConnect(computername, username, password);
                Dictionary<string, string> originalValues = ReadRegValues(wmiConn, servicename);
                UpdateRegValues(wmiConn, servicename, newValues);
                Thread.Sleep(5000);
                WMIPerf(wmiConn, categoryname, countername, objectname, computername);
                Thread.Sleep(2000);
                UpdateRegValues(wmiConn, servicename, originalValues);
            }
            else if(arguments.ContainsKey("diagrun"))
            {
                ManagementScope wmiConn = WMIConnect(computername, username, password);
                Dictionary<string, string> originalValues = ReadRegValues(wmiConn, servicename);
                UpdateRegValues(wmiConn, servicename, newValues);
                Thread.Sleep(5000);
                DiagnosticRun(categoryname, countername, instancename, computername);
                Thread.Sleep(2000);
                UpdateRegValues(wmiConn, servicename, originalValues);
            }
        }
    }
}
