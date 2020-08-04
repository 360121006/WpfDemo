#pragma once

class CCharacterHelper
{
public:
	CCharacterHelper();
	~CCharacterHelper();

	void ConvertUnicodeToANSI(CString strUnicode, char* strANSI);
	void ConvertUTF8ToANSI(char* strUTF8, CString &strANSI);

	wchar_t* AnsiToUnicode(const char* buf);
	CString AnsiToUnicodeEx(const char * buf);
	char* UnicodeToUtf8(const wchar_t* buf);

	wchar_t * UTF8ToUnicode(const char* str);
	CString UTF8ToUnicodeEx(const char* str);
	char * UnicodeToANSI(const wchar_t *str);

	char* UTF8ToANSI(const char* buf);
	char* AnsiToUtf8(const char* buf);
};

