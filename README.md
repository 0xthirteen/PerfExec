# PerfExec Tooling

Proof of concept tooling referenced at [this blog](https://posts.specterops.io/performance-diagnostics-and-wmi-21f3e01790d3) 

The code is not super clean but project contains an example performance dll that will run CMD.exe and a .NET assembly that will execute the DLL or gather performance data locally or remotely.

Two execution methods currently exist, WMI and Remote Registry

Diagnostic Run example (uses remote registry with DCOM)
```
.\PerfExec.exe diagrun=true service=DNS object="DNS" category="DNS" counter="Total Query Received" dllpath="C:\Users\user\SFPerf.dll" open="OpenPerformanceData" collect="CollectPerformanceData" close="ClosePerformanceData" computername=10.10.10.13
```



WMI Run example (uses WMI)
```
.\PerfExec.exe wmirun=true service=DNS object="DNS" category="TotalQueryReceived" dllpath="C:\Users\user\SFPerf.dll" open="OpenPerformanceData" collect="CollectPerformanceData" close="ClosePerformanceData" computername=10.10.10.13
```


### Credit
Lee Christensen (@tifkin_)