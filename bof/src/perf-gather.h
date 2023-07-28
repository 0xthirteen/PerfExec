#ifdef BOF

/* ADVAPI32 */
WINADVAPI LONG WINAPI ADVAPI32$RegConnectRegistryA(LPCSTR lpMachineName, HKEY hKey, PHKEY phkResult);
WINADVAPI LONG WINAPI ADVAPI32$RegOpenKeyExA(HKEY hKey, LPCSTR lpSubKey, DWORD ulOptions, REGSAM samDesired, PHKEY phkResult);
WINADVAPI LONG WINAPI ADVAPI32$RegEnumKeyExA(HKEY hKey, DWORD dwIndex, LPSTR lpName, LPDWORD lpcName, LPDWORD lpReserved, LPSTR lpClass, LPDWORD lpcClass, PFILETIME lpftLastWriteTime);
WINADVAPI LONG WINAPI ADVAPI32$RegQueryValueExA(HKEY hKey, LPCSTR lpValueName, LPDWORD lpReserved, LPDWORD lpType, LPBYTE lpData, LPDWORD lpcbData);
WINADVAPI LONG WINAPI ADVAPI32$RegCloseKey(HKEY hKey);

/* MSVCRT */
WINBASEAPI void __cdecl MSVCRT$memset(void *dest, int c, size_t count);
DECLSPEC_IMPORT int __cdecl MSVCRT$strcmp(const char *_Str1,const char *_Str2);

#else

/* ADVAPI32 */
#define ADVAPI32$RegConnectRegistryA RegConnectRegistryA
#define ADVAPI32$RegOpenKeyExA RegOpenKeyExA
#define ADVAPI32$RegEnumKeyExA RegEnumKeyExA
#define ADVAPI32$RegQueryValueExA RegQueryValueExA
#define ADVAPI32$RegCloseKey RegCloseKey

/* MSVCRT */
#define MSVCRT$memset memset
#define MSVCRT$strcmp strcmp

#endif
