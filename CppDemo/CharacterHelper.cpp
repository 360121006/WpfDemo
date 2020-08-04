#include "stdafx.h"
#include "CharacterHelper.h"



CCharacterHelper::CCharacterHelper()
{
}


CCharacterHelper::~CCharacterHelper()
{
}

//CString->char*
void CCharacterHelper::ConvertUnicodeToANSI(CString strUnicode, char* strANSI)
{
	int len = WideCharToMultiByte(CP_ACP, 0, strUnicode, strUnicode.GetLength(), NULL, 0, NULL, NULL);
	WideCharToMultiByte(CP_ACP, 0, strUnicode, -1, strANSI, len+1, NULL, NULL);          //��unicodeת��ansi     
	strANSI[len] = 0;
}


void CCharacterHelper::ConvertUTF8ToANSI(char* strUTF8, CString &strANSI)
{
	int nLen = ::MultiByteToWideChar(CP_UTF8, 0, strUTF8, -1, NULL, 0);
	//������Ҫ��unicode����     
	WCHAR * wszANSI = new WCHAR[nLen + 1];
	memset(wszANSI, 0, nLen * 2 + 2);
	nLen = MultiByteToWideChar(CP_UTF8, 0, strUTF8, -1, wszANSI, nLen);    //��utf8ת��unicode    
	nLen = WideCharToMultiByte(CP_ACP, 0, wszANSI, -1, NULL, 0, NULL, NULL);        //�õ�Ҫ��ansi����     
	char *szANSI = new char[nLen + 1];
	memset(szANSI, 0, nLen + 1);
	WideCharToMultiByte(CP_ACP, 0, wszANSI, -1, szANSI, nLen, NULL, NULL);          //��unicodeת��ansi     
	strANSI = szANSI;
	delete[] wszANSI;
	wszANSI = NULL;
	delete[] szANSI;
	szANSI = NULL;
}

wchar_t * CCharacterHelper::UTF8ToUnicode(const char* str)
{
	int    textlen = 0;
	wchar_t * result;
	textlen = MultiByteToWideChar(CP_UTF8, 0, str, -1, NULL, 0);
	result = (wchar_t *)malloc((textlen + 1) * sizeof(wchar_t));
	memset(result, 0, (textlen + 1) * sizeof(wchar_t));
	MultiByteToWideChar(CP_UTF8, 0, str, -1, (LPWSTR)result, textlen);
	return    result;
}

CString CCharacterHelper::UTF8ToUnicodeEx(const char* str)
{
	int    textlen = 0;
	wchar_t * result;
	textlen = MultiByteToWideChar(CP_UTF8, 0, str, -1, NULL, 0);
	result = (wchar_t *)malloc((textlen + 1) * sizeof(wchar_t));
	memset(result, 0, (textlen + 1) * sizeof(wchar_t));
	MultiByteToWideChar(CP_UTF8, 0, str, -1, (LPWSTR)result, textlen);
	CString rtn(result);
	free(result);
	return rtn;
}

//Unicodeת��ANSI  
char * CCharacterHelper::UnicodeToANSI(const wchar_t *str)
{
	char * result;
	int textlen = 0;
	// wide char to multi char  
	textlen = WideCharToMultiByte(CP_ACP, 0, str, -1, NULL, 0, NULL, NULL);
	result = (char *)malloc((textlen + 1) * sizeof(char));
	memset(result, 0, sizeof(char) * (textlen + 1));
	WideCharToMultiByte(CP_ACP, 0, str, -1, result, textlen, NULL, NULL);
	return result;
}

char* CCharacterHelper::UTF8ToANSI(const char* buf)
{
	return UnicodeToANSI(UTF8ToUnicode(buf));
}

//ANSIת��Unicode  
wchar_t* CCharacterHelper::AnsiToUnicode(const char* buf)
{
	int textlen = 0;

	wchar_t* result;

	textlen = MultiByteToWideChar(CP_ACP, 0, buf, -1, NULL, 0);

	result = (wchar_t *)malloc((textlen + 1) * sizeof(wchar_t));

	memset(result, 0, (textlen + 1) * sizeof(wchar_t));

	MultiByteToWideChar(CP_ACP, 0, buf, -1, (LPWSTR)result, textlen);

	return result;
}

CString CCharacterHelper::AnsiToUnicodeEx(const char* buf)
{
	int textlen = 0;

	wchar_t* result;

	textlen = MultiByteToWideChar(CP_ACP, 0, buf, -1, NULL, 0);

	result = (wchar_t *)malloc((textlen + 1) * sizeof(wchar_t));

	memset(result, 0, (textlen + 1) * sizeof(wchar_t));

	MultiByteToWideChar(CP_ACP, 0, buf, -1, (LPWSTR)result, textlen);

	CString szRtn(result);
	free(result);
	return szRtn;
}

//Unicodeת��UTF8  
char* CCharacterHelper::UnicodeToUtf8(const wchar_t* buf)
{
	char* result;

	int textlen = 0;

	textlen = WideCharToMultiByte(CP_UTF8, 0, buf, -1, NULL, 0, NULL, NULL);

	result = (char *)malloc((textlen + 1) * sizeof(char));

	memset(result, 0, sizeof(char) * (textlen + 1));

	WideCharToMultiByte(CP_UTF8, 0, buf, -1, result, textlen, NULL, NULL);

	return result;
}

char* CCharacterHelper::AnsiToUtf8(const char* szAnsi)
{
	/*   if (szAnsi == NULL)
	return NULL ;

	_bstr_t   bstrTmp (szAnsi) ;
	int       nLen = ::WideCharToMultiByte (CP_UTF8, 0, (LPCWSTR)bstrTmp, -1, NULL, 0, NULL, NULL) ;
	char      * pUTF8 = new char[nLen+1] ;
	ZeroMemory (pUTF8, nLen + 1) ;
	::WideCharToMultiByte (CP_UTF8, 0, (LPCWSTR)bstrTmp, -1, pUTF8, nLen, NULL, NULL) ;
	return pUTF8 ;
	*/

	return UnicodeToUtf8(AnsiToUnicode(szAnsi));
}

