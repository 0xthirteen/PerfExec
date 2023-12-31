

#include "pch.h"
#include <windows.h>
#include <pdh.h>
#include <stdio.h>

#define COUNTER_NAME L"ProcessedRequests"

DWORD g_CounterValue = 0;

PDH_HCOUNTER g_hCounter = NULL;

PDH_HQUERY g_hQuery = NULL;

extern "C" __declspec(dllexport) DWORD APIENTRY OpenPerformanceData(LPWSTR lpDeviceNames)
{
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    const char* commandLine = "cmd.exe";
    wchar_t wideCommandLine[MAX_PATH];
    MultiByteToWideChar(CP_UTF8, 0, commandLine, -1, wideCommandLine, MAX_PATH);

    if (!CreateProcess(NULL,
        wideCommandLine,
        NULL,
        NULL,
        FALSE,
        0,
        NULL,
        NULL,
        &si,
        &pi)
        )
    {
        printf("CreateProcess failed (%d).\n", GetLastError());
        return 1;
    }

    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);

    return ERROR_SUCCESS;

    PDH_STATUS status = PdhOpenQuery(NULL, 0, &g_hQuery);
    if (status != ERROR_SUCCESS)
        return status;

    status = PdhAddCounter(g_hQuery, COUNTER_NAME, 0, &g_hCounter);
    if (status != ERROR_SUCCESS)
    {
        PdhCloseQuery(g_hQuery);
        g_hQuery = NULL;
        return status;
    }

    return ERROR_SUCCESS;
}

extern "C" __declspec(dllexport) DWORD APIENTRY CollectPerformanceData(LPWSTR lpValueName, LPVOID * lppData, LPDWORD lpcbTotalBytes, LPDWORD lpNumObjectTypes)
{

    PDH_STATUS status = PdhCollectQueryData(g_hQuery);
    if (status != ERROR_SUCCESS)
        return status;

    PDH_FMT_COUNTERVALUE counterValue;
    status = PdhGetFormattedCounterValue(g_hCounter, PDH_FMT_LONG, NULL, &counterValue);
    if (status != ERROR_SUCCESS)
        return status;

    *lpcbTotalBytes = sizeof(PERF_COUNTER_BLOCK) + sizeof(DWORD);
    *lpNumObjectTypes = 1;
    *lppData = (LPVOID)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, *lpcbTotalBytes);
    if (*lppData == NULL)
        return ERROR_OUTOFMEMORY;

    PPERF_COUNTER_BLOCK pCounterBlock = (PPERF_COUNTER_BLOCK)*lppData;
    pCounterBlock->ByteLength = sizeof(PERF_COUNTER_BLOCK) + sizeof(DWORD);

    DWORD* pCounterValue = (DWORD*)((PBYTE)pCounterBlock + sizeof(PERF_COUNTER_BLOCK));
    *pCounterValue = counterValue.longValue;
    g_CounterValue = counterValue.longValue;

    return ERROR_SUCCESS;
}

extern "C" __declspec(dllexport) DWORD APIENTRY ClosePerformanceData()
{
    if (g_hQuery != NULL)
    {
        PdhRemoveCounter(g_hCounter);
        PdhCloseQuery(g_hQuery);
        g_hQuery = NULL;
        g_hCounter = NULL;
    }

    return ERROR_SUCCESS;
}
