#ifdef BOF

/* MSVCRT */
WINBASEAPI SIZE_T WINAPI MSVCRT$strlen(const char* str);
DECLSPEC_IMPORT int __cdecl MSVCRT$strcmp(const char *_Str1,const char *_Str2);
WINBASEAPI void* WINAPI MSVCRT$strcpy(const char* dest, const char* source);
WINBASEAPI void* WINAPI MSVCRT$strcat(const char* dest, const char* source);
WINBASEAPI void* __cdecl MSVCRT$malloc( size_t size);
WINBASEAPI void __cdecl MSVCRT$free( void* memblock);

/* PDH */
WINBASEAPI int __cdecl PDH$PdhOpenQuery(LPCSTR szDataSource, DWORD_PTR dwUserData, PDH_HQUERY *phQuery);
WINBASEAPI int __cdecl PDH$PdhAddCounterA(PDH_HQUERY hQuery, LPCSTR szFullCounterPath, DWORD_PTR dwUserData, PDH_HCOUNTER *phCounter);
WINBASEAPI int __cdecl PDH$PdhCollectQueryData(PDH_HQUERY hQuery);
WINBASEAPI int __cdecl PDH$PdhCloseQuery(PDH_HQUERY hQuery);

#else

/* MSVCRT */
#define MSVCRT$strlen strlen
#define MSVCRT$strcmp strcmp
#define MSVCRT$strcpy strcpy
#define MSVCRT$strcat strcat
#define MSVCRT$malloc malloc
#define MSVCRT$free free
#define MSVCRT$malloc malloc
#define MSVCRT$free free

/* PDH */
#define PDH$PdhOpenQuery PdhOpenQuery
#define PDH$PdhAddCounterA PdhAddCounterA
#define PDH$PdhCollectQueryData PdhCollectQueryData
#define PDH$PdhCloseQuery PdhCloseQuery

#endif