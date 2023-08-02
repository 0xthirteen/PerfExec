#include <windows.h>
#include <winreg.h>
#include "perf-gather.h"

#ifdef BOF
#include "beacon.h"
#else
#include <stdio.h>
#endif

#include "common.c"

#define MAX_KEY_LENGTH 256
#define MAX_VALUE_NAME 512

void execute(char* target) {
    internal_printf("[*] Target: %s\n", target);
    
    HKEY hHKLM, hServices, hSubkey, hPerformance;
    DWORD dwIndex = 0;
    CHAR szSubkeyName[MAX_KEY_LENGTH];
    DWORD dwSubkeyNameSize = sizeof(szSubkeyName);
    LONG lResult;

    BYTE szLibrary[MAX_VALUE_NAME];
    BYTE szPerfIniFile[MAX_VALUE_NAME];
    BYTE szOpen[MAX_VALUE_NAME];
    BYTE szCollect[MAX_VALUE_NAME];
    BYTE szClose[MAX_VALUE_NAME];
    DWORD dwBufferSize = sizeof(szLibrary);
    DWORD dwType = 0;

    
    /* Connect to HKLM on the target */
    if (MSVCRT$strcmp(target, "localhost") == 0) {
        lResult = ADVAPI32$RegConnectRegistryA(NULL, HKEY_LOCAL_MACHINE, &hHKLM);
    }
    else {
        lResult = ADVAPI32$RegConnectRegistryA(target, HKEY_LOCAL_MACHINE, &hHKLM);
    }

    if (lResult != ERROR_SUCCESS) {
        internal_printf("[!] RegConnectRegistryA failed with code %d\n", lResult);
        return;
    }
 
    /* Enumerate services */
    lResult = ADVAPI32$RegOpenKeyExA(hHKLM, "SYSTEM\\CurrentControlSet\\Services", 0, KEY_READ, &hServices);
    if (lResult != ERROR_SUCCESS) {
        internal_printf("[!] RegOpenKeyExA failed with code %d\n", lResult);
        ADVAPI32$RegCloseKey(hHKLM);
        return;
    }

    internal_printf("[*] Enumerating Services subkeys...\n\n");

    /* Loop through services subkeys*/
    while ((lResult = ADVAPI32$RegEnumKeyExA(hServices, dwIndex, szSubkeyName, &dwSubkeyNameSize, NULL, NULL, NULL, NULL)) != ERROR_NO_MORE_ITEMS) {
        if (lResult != ERROR_SUCCESS) {
            internal_printf("[!] Error enumerating Services subkeys: %ld\n", lResult);
            ADVAPI32$RegCloseKey(hServices);
            ADVAPI32$RegCloseKey(hHKLM);
            return;
        }

        lResult = ADVAPI32$RegOpenKeyExA(hServices, szSubkeyName, 0, KEY_READ, &hSubkey);
        if (lResult == ERROR_SUCCESS) {

            /* Check if Performance subkey exists*/
            if (ADVAPI32$RegOpenKeyExA(hSubkey, "Performance", 0, KEY_READ, &hPerformance) == ERROR_SUCCESS) {
                internal_printf("[+] HKLM\\SYSTEM\\CurrentControlSet\\Services\\%s\\Performance\n", szSubkeyName);

                /* Grab library, INI file name and Open/Close/Collect func names */
                const char* values[] = {"PerfIniFile", "Library" , "Open", "Collect", "Close"};
                BYTE* buffers[] = {szLibrary, szPerfIniFile, szOpen, szCollect, szClose};

                for (int i = 0; i < sizeof(values) / sizeof(values[0]); i++) {
                    MSVCRT$memset(buffers[i], 0, dwBufferSize);
                    dwBufferSize = sizeof(szLibrary);

                    lResult = ADVAPI32$RegQueryValueExA(hPerformance, values[i], NULL, &dwType, buffers[i], &dwBufferSize);
                    if (lResult == ERROR_SUCCESS) {
                        internal_printf(" |_ %s: %s\n", values[i], buffers[i]);
                    }
                }

                ADVAPI32$RegCloseKey(hPerformance);
            }
            ADVAPI32$RegCloseKey(hSubkey);
        }

        dwIndex++;
        dwSubkeyNameSize = sizeof(szSubkeyName);
    }

    /* Close reg handles */
    ADVAPI32$RegCloseKey(hServices);
    ADVAPI32$RegCloseKey(hHKLM);
    
    return;
}

#ifdef BOF
void go(IN PCHAR Args, IN ULONG Length) {
	char* target = NULL;
	
	datap parser;
	BeaconDataParse(&parser, Args, Length);
	target = (char*)BeaconDataExtract(&parser, NULL);
    
    if(!bofstart())
	{
		return;
	}

	execute(target);
    printoutput(TRUE);
	return;
}

#else

int main(int argc, char* argv[]) {
    char* target;
    if (argc < 2) {
		target = "localhost";
	}
    else {
	    target = argv[1];
    }
    
    execute(target);
    return 0;
}

#endif