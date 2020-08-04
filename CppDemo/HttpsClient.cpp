#include "stdafx.h"
#include "HttpsClient.h"
#include <Sensapi.h>  //网络检查
#pragma comment(lib, "Sensapi.lib")
#include "iphlpapi.h"
#pragma comment(lib, "iphlpapi.lib")
#include <WinSock2.h>  
#include <WS2tcpip.h> 
CHttpsClient::CHttpsClient()
{
}

CHttpsClient::~CHttpsClient()
{

}

CString  CHttpsClient::szApiUrl = L"";
CString  CHttpsClient::szToken = L"";
CString  CHttpsClient::szAESKey = L"";
CString  CHttpsClient::szLoginToken = L"";
CString  CHttpsClient::szCentApiUrl = L"";
CString  CHttpsClient::szStaticUrl = L"";

int CHttpsClient::URLEncode(const char* str, int strSize, char* result, const int resultSize)
{
	int i;
	int j = 0;//for result index  
	char ch;
	strSize = strlen(str);
	if ((str == NULL) || (result == NULL) || (strSize <= 0) || (resultSize <= 0)) {
		return 0;
	}
	for (i = 0; (i<strSize) && (j<resultSize); i++) {
		ch = str[i];
		if (((ch >= 'A') && (ch <= 'Z')) ||
			((ch >= 'a') && (ch <= 'z')) ||
			((ch >= '0') && (ch <= '9'))) {
			result[j++] = ch;
		}
		else if (ch == ' ') {
			result[j++] = '+';
		}
		else if (ch == '.' || ch == '-' || ch == '_' || ch == '*') {
			result[j++] = ch;
		}
		else {
			if (j + 3 < resultSize) {
				sprintf_s(result + j, strSize, "%%%02x", (unsigned char)ch);
				j += 3;
			}
			else {
				return 0;
			}
		}
	}
	result[j] = '\0';
	return j;
}

CString CHttpsClient::GetMacByCmd()
{
	PIP_ADAPTER_INFO pAdapterInfo;
	DWORD AdapterInfoSize;
	TCHAR szMac[32] = { 0 };
	DWORD Err;

	AdapterInfoSize = 0;
	Err = GetAdaptersInfo(NULL, &AdapterInfoSize);

	if ((Err != 0) && (Err != ERROR_BUFFER_OVERFLOW))
	{
		TRACE("获得网卡信息失败！");
		return   L"";
	}

	//   分配网卡信息内存  
	pAdapterInfo = (PIP_ADAPTER_INFO)GlobalAlloc(GPTR, AdapterInfoSize);
	if (pAdapterInfo == NULL)
	{
		TRACE("分配网卡信息内存失败");
		return   L"";
	}

	if (GetAdaptersInfo(pAdapterInfo, &AdapterInfoSize) != 0)
	{
		TRACE(_T("获得网卡信息失败！\n"));
		GlobalFree(pAdapterInfo);
		return   L"";
	}
	CString strMac;
	strMac.Format(_T("%02X%02X%02X%02X%02X%02X"),
		pAdapterInfo->Address[0],
		pAdapterInfo->Address[1],
		pAdapterInfo->Address[2],
		pAdapterInfo->Address[3],
		pAdapterInfo->Address[4],
		pAdapterInfo->Address[5]);

	GlobalFree(pAdapterInfo);
	return   strMac;
}

CString CHttpsClient::GetOsInfo()
{
	HKEY hKey;
	long lRet;
	TCHAR Buffer[MAX_PATH * 2] = { 0 };
	DWORD dwType = REG_SZ;
	DWORD dwSize = MAX_PATH * 2;
	CString szOsInfo;
	lRet = RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"Software\\Microsoft\\Windows NT\\CurrentVersion", 0, KEY_READ, &hKey);
	if (lRet == ERROR_SUCCESS)
	{
		RegQueryValueEx(hKey, L"ProductName", 0, &dwType, (LPBYTE)Buffer, &dwSize);
		szOsInfo = CString(Buffer);
	}
	return szOsInfo;
}

CString CHttpsClient::GUID_Generator()
{
	CString szGUID;
	GUID guid;
	CoInitialize(NULL);
	if (S_OK == ::CoCreateGuid(&guid))
	{
		szGUID.Format(_T("%08X%04X%04X%02X%02X%02X%02X%02X%02X%02X%02X"),
			guid.Data1,
			guid.Data2,
			guid.Data3,
			guid.Data4[0], guid.Data4[1],
			guid.Data4[2], guid.Data4[3],
			guid.Data4[4], guid.Data4[5],
			guid.Data4[6], guid.Data4[7]);
	}
	CoUninitialize();
	CString szMac = GetMacByCmd();
	return szGUID + szMac;
}

CString  CHttpsClient::HttpsGetRz(LPCTSTR strurl)
{
	CString strResult;
	CoInitialize(NULL);
	try
	{
		MSXML2::IServerXMLHTTPRequestPtr xmlrequest;
		xmlrequest.CreateInstance("Msxml2.ServerXMLHTTP");
		if (xmlrequest == NULL)
		{
			strResult.Format(_T("%s"), "ServerXMLHTTP创建失败！");
			return strResult;
		}
		xmlrequest->open("GET", _bstr_t(strurl), FALSE);
		//xmlrequest->setRequestHeader(_bstr_t("Content-Type"), _bstr_t("text/xml; charset=utf-8"));
		//xmlrequest->setRequestHeader(_bstr_t("SOAPAction"), _bstr_t("WsAdapterImplService"));
		xmlrequest->setRequestHeader("Content-Type", "application/octet-stream");
		xmlrequest->setOption(MSXML2::SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, _bstr_t("13056"));
		xmlrequest->setTimeouts(1000, 1000, 1000, 2000);
		xmlrequest->send(_variant_t(""));
		BSTR bstrbody;
		xmlrequest->get_responseText(&bstrbody);
		strResult.Format(_T("%s"), bstrbody);
		strResult.Replace(L"&lt;", L"<");
		strResult.Replace(L"&gt;", L">");
		SysFreeString(bstrbody); // 用完释放 
		xmlrequest.Release();
	}
	catch (_com_error &e)
	{
		strResult.Format(_T("%s"), e.Description());
	}
	CoUninitialize();
	return strResult;
}

CString  CHttpsClient::HttpPostforLogin(LPCTSTR strurl, LPCTSTR strParam, LPCTSTR strToken)
{
	CString strResult;
	CoInitialize(NULL);
	try
	{
		MSXML2::IServerXMLHTTPRequestPtr xmlrequest;
		xmlrequest.CreateInstance("Msxml2.ServerXMLHTTP");
		if (xmlrequest == NULL)
		{
			strResult.Format(_T("%s"), "ServerXMLHTTP创建失败！");
			return strResult;
		}
		CMD5Helper md5helper;
		CCharacterHelper characterHelper;
		CHmacsha hmacsha;
		CBase64Helper base64helper;
		CString szMd5Sign = md5helper.GetMD5(strParam);
		CString szCurrTime;
		SYSTEMTIME systm;
		GetLocalTime(&systm);
		szCurrTime.Format(_T("%4d-%.2d-%.2d %.2d:%.2d:%.2d"), systm.wYear, systm.wMonth, systm.wDay, systm.wHour, systm.wMinute, systm.wSecond);
		CString szHeader = L"POST\napplication/xml\n" + szMd5Sign + L"\napplication/xml\nX-Gravitee-Api-Key:73477da5-ff73-4524-a003-1fb129c84b3a\nX-aisino-date:" + szCurrTime + L"\nX-aisino-access-token:" + strToken + L"\nX-aisino-signature-method:HMAC-SHA1\nX-aisino-signature-version:1.0\n/user/login";
		char* pHeader = characterHelper.UnicodeToUtf8(szHeader);
		char out[64];
		int lenOut = 32;
		hmacsha.hmac_sha1("73477da5", 8, pHeader, strlen(pHeader), out, &lenOut);
		CString szSha1Sign = hmacsha.bytestohexstring(out, lenOut);
		szSha1Sign.MakeUpper();
		int lenSha1Sign = szSha1Sign.GetLength();
		char* pSha1Sign = characterHelper.UnicodeToUtf8(szSha1Sign);
		char * base64code = new char[lenSha1Sign * 4 + 1];
		memset(base64code, 0, lenSha1Sign * 4 + 1);
		int x = strlen(pSha1Sign);
		base64helper.Base64Encode(base64code, pSha1Sign, x);
		szSha1Sign = characterHelper.UTF8ToUnicodeEx(base64code);
		free(pHeader);
		free(pSha1Sign);
		xmlrequest->open("POST", _bstr_t(strurl), FALSE);
		xmlrequest->setRequestHeader(_bstr_t("Accept"), _bstr_t("application/xml"));
		xmlrequest->setRequestHeader(_bstr_t("Content-MD5"), _bstr_t(szMd5Sign));
		xmlrequest->setRequestHeader(_bstr_t("Content-Type"), _bstr_t("application/xml"));
		xmlrequest->setRequestHeader(_bstr_t("X-aisino-date"), _bstr_t(szCurrTime));
		xmlrequest->setRequestHeader(_bstr_t("X-Gravitee-Api-Key"), _bstr_t("73477da5-ff73-4524-a003-1fb129c84b3a"));
		xmlrequest->setRequestHeader(_bstr_t("Authorization"), _bstr_t(szSha1Sign));
		if (strToken!=L"") xmlrequest->setRequestHeader(_bstr_t("X-aisino-access-token"), _bstr_t(strToken));
		xmlrequest->setRequestHeader(_bstr_t("X-aisino-signature-method"), _bstr_t("HMAC-SHA1"));
		xmlrequest->setRequestHeader(_bstr_t("X-aisino-signature-version"), _bstr_t("1.0"));
		xmlrequest->setOption(MSXML2::SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, _bstr_t("13056"));
		xmlrequest->setTimeouts(10000, 10000, 10000, 20000);
		xmlrequest->send(_variant_t(strParam));
		BSTR bstrbody;
		xmlrequest->get_responseText(&bstrbody);
		long status = xmlrequest->Getstatus();
		strResult.Format(_T("%s"), bstrbody);
		strResult.Replace(L"&lt;", L"<");
		strResult.Replace(L"&gt;", L">");
		SysFreeString(bstrbody); // 用完释放 
		xmlrequest.Release();
	}
	catch (_com_error &e)
	{
		strResult.Format(_T("%s"), e.Description());
	}
	CoUninitialize();
	return strResult;
}

CString CHttpsClient::CommonHttpRequest(char* Method,LPCTSTR strurl, LPCTSTR strParam, LPCTSTR strApiKey, LPCTSTR MethodURL, LPCTSTR acceptType, LPCTSTR contentType)
{
	//CLogHelper log;
	//log.WriteLog(L"Method",CString(Method));
	//log.WriteLog(L"strurl", CString(strurl));
	//log.WriteLog(L"strParam", CString(strParam));
	//log.WriteLog(L"strApiKey", CString(strApiKey));
	//log.WriteLog(L"MethodURL", CString(MethodURL));
	//log.WriteLog(L"acceptType", CString(acceptType));
	CString strResult;
	CoInitialize(NULL);
	try
	{
		MSXML2::IServerXMLHTTPRequestPtr xmlrequest;
		xmlrequest.CreateInstance("Msxml2.ServerXMLHTTP");
		if (xmlrequest == NULL)
		{
			strResult.Format(_T("%s"), "ServerXMLHTTP创建失败！");
			return strResult;
		}
		CMD5Helper md5helper;
		CCharacterHelper characterHelper;
		CHmacsha hmacsha;
		CBase64Helper base64helper;
		CString szMd5Sign, szCurrTime; 
		bool isBodyEmpty = CString(strParam) == L"";
		if (!isBodyEmpty) szMd5Sign = md5helper.GetMD5(strParam);
		//log.WriteLog(L"szMd5Sign", CString(szMd5Sign));
		SYSTEMTIME systm;
		GetLocalTime(&systm);
		szCurrTime.Format(_T("%4d-%.2d-%.2d %.2d:%.2d:%.2d"), systm.wYear, systm.wMonth, systm.wDay, systm.wHour, systm.wMinute, systm.wSecond);
		CString szHeader = CString(Method) + L"\n"+ acceptType +L"\n" + szMd5Sign + L"\n" + (isBodyEmpty ? L"" : contentType) + L"\nX-Gravitee-Api-Key:" + strApiKey + L"\nX-aisino-date:" + szCurrTime + L"\nX-aisino-access-token:" + szToken + L"\nX-aisino-signature-method:HMAC-SHA1\nX-aisino-signature-version:1.0\n" + MethodURL;
		char* pHeader = characterHelper.UnicodeToUtf8(szHeader);
		//log.WriteLog(L"pHeader", CString(pHeader));
		char out[64];
		int lenOut = 32;
		CString szKey = CString(strApiKey).Mid(0, 8);
		char* pKey = characterHelper.UnicodeToANSI(szKey);
		hmacsha.hmac_sha1(pKey, 8, pHeader, strlen(pHeader), out, &lenOut);
		free(pKey);
		CString szSha1Sign = hmacsha.bytestohexstring(out, lenOut);
		szSha1Sign.MakeUpper();
		int lenSha1Sign = szSha1Sign.GetLength();
		char* pSha1Sign = characterHelper.UnicodeToUtf8(szSha1Sign);
		char * base64code = new char[lenSha1Sign * 4 + 1];
		memset(base64code, 0, lenSha1Sign * 4 + 1);
		int x = strlen(pSha1Sign);
		base64helper.Base64Encode(base64code, pSha1Sign, x);
		szSha1Sign = characterHelper.UTF8ToUnicodeEx(base64code);
		//log.WriteLog(L"szSha1Sign", szSha1Sign);
		free(pHeader);
		free(pSha1Sign);
		delete[] base64code;
		base64code = NULL;
		//log.WriteLog(L"1", L"");
		xmlrequest->open(Method, _bstr_t(strurl), FALSE);
		xmlrequest->setRequestHeader(_bstr_t("Accept"), _bstr_t(acceptType));
		if (szMd5Sign!=L"") xmlrequest->setRequestHeader(_bstr_t("Content-MD5"), _bstr_t(szMd5Sign));
		if (!isBodyEmpty) xmlrequest->setRequestHeader(_bstr_t("Content-Type"), _bstr_t(contentType));
		xmlrequest->setRequestHeader(_bstr_t("X-aisino-date"), _bstr_t(szCurrTime));
		xmlrequest->setRequestHeader(_bstr_t("X-Gravitee-Api-Key"), _bstr_t(strApiKey));
		xmlrequest->setRequestHeader(_bstr_t("Authorization"), _bstr_t(szSha1Sign));
		if (szToken != L"") xmlrequest->setRequestHeader(_bstr_t("X-aisino-access-token"), _bstr_t(szToken));
		xmlrequest->setRequestHeader(_bstr_t("X-aisino-signature-method"), _bstr_t("HMAC-SHA1"));
		xmlrequest->setRequestHeader(_bstr_t("X-aisino-signature-version"), _bstr_t("1.0"));
		xmlrequest->setOption(MSXML2::SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, _bstr_t("13056"));
		xmlrequest->setTimeouts(10000, 10000, 10000, 20000);
		//log.WriteLog(L"2", L"");
		xmlrequest->send(_variant_t(strParam));
		//log.WriteLog(L"3", L"");
		BSTR bstrbody;
		xmlrequest->get_responseText(&bstrbody);
		long status = xmlrequest->Getstatus();
		strResult.Format(_T("%s"), bstrbody);
		strResult.Replace(L"&lt;", L"<");
		strResult.Replace(L"&gt;", L">");
		SysFreeString(bstrbody); // 用完释放 
		xmlrequest.Release();
	}
	catch (_com_error &e)
	{
		strResult.Format(_T("%s"), e.Description());
		//log.WriteLog(L"strResult", strResult);

	}
	catch (exception &e)
	{
		strResult.Format(_T("%s"), e.what());
		//log.WriteLog(L"exception", strResult);
	}
	CoUninitialize();
	return strResult;
}

//CString CHttpsClient::SocketHttpsRequest(char* Method, LPCTSTR strurl, LPCTSTR strParam, LPCTSTR strApiKey, LPCTSTR MethodURL, LPCTSTR acceptType)
//{
//	CLogHelper log;
//	//log.WriteLog(L"Method", CString(Method));
//	//log.WriteLog(L"strurl", CString(strurl));
//	//log.WriteLog(L"strParam", CString(strParam));
//	//log.WriteLog(L"strApiKey", CString(strApiKey));
//	//log.WriteLog(L"MethodURL", CString(MethodURL));
//	//log.WriteLog(L"acceptType", CString(acceptType));
//
//	CString ret;
//	CMD5Helper md5helper;
//	CCharacterHelper characterHelper;
//	CHmacsha hmacsha;
//	CBase64Helper base64helper;
//	CString szMd5Sign, szCurrTime;
//	bool isBodyEmpty = CString(strParam) == L"";
//	if (!isBodyEmpty) szMd5Sign = md5helper.GetMD5(strParam);
//	SYSTEMTIME systm;
//	GetLocalTime(&systm);
//	szCurrTime.Format(_T("%4d-%.2d-%.2d %.2d:%.2d:%.2d"), systm.wYear, systm.wMonth, systm.wDay, systm.wHour, systm.wMinute, systm.wSecond);
//	CString szHeader = CString(Method) + L"\n" + acceptType + L"\n" + szMd5Sign + L"\n" + (isBodyEmpty ? L"" : L"application/xml") + L"\nX-Gravitee-Api-Key:" + strApiKey + L"\nX-aisino-date:" + szCurrTime + L"\nX-aisino-access-token:" + szToken + L"\nX-aisino-signature-method:HMAC-SHA1\nX-aisino-signature-version:1.0\n" + MethodURL;
//	char* pHeader = characterHelper.UnicodeToUtf8(szHeader);
//	//log.WriteLog(L"pHeader", CString(pHeader));
//	char out[64];
//	int lenOut = 32;
//	CString szKey = CString(strApiKey).Mid(0, 8);
//	char* pKey = characterHelper.UnicodeToANSI(szKey);
//	hmacsha.hmac_sha1(pKey, 8, pHeader, strlen(pHeader), out, &lenOut);
//	free(pKey);
//	CString szSha1Sign = hmacsha.bytestohexstring(out, lenOut);
//	szSha1Sign.MakeUpper();
//	int lenSha1Sign = szSha1Sign.GetLength();
//	char* pSha1Sign = characterHelper.UnicodeToUtf8(szSha1Sign);
//	char * base64code = new char[lenSha1Sign * 4 + 1];
//	memset(base64code, 0, lenSha1Sign * 4 + 1);
//	int x = strlen(pSha1Sign);
//	base64helper.Base64Encode(base64code, pSha1Sign, x);
//	szSha1Sign = characterHelper.UTF8ToUnicodeEx(base64code);
//	//log.WriteLog(L"szSha1Sign", szSha1Sign);
//	free(pHeader);
//	free(pSha1Sign);
//	delete[] base64code;
//	base64code = NULL;
//	CString headers = L"Accept:"+ CString(acceptType)+L"\r\n";
//	if (szMd5Sign != L"")
//		headers+= L"Content-MD5:" + CString(szMd5Sign) + L"\r\n";
//	headers += L"Content-Type:application/xml\r\n";
//	headers += L"X-aisino-date:" + CString(szCurrTime) + L"\r\n";
//	headers += L"X-Gravitee-Api-Key:" + CString(strApiKey) + L"\r\n";
//	headers += L"Authorization:" + CString(szSha1Sign) + L"\r\n";
//	if (szToken != L"")
//		headers += L"X-aisino-access-token:" + szToken + L"\r\n";
//	headers += L"X-aisino-signature-method:HMAC-SHA1\r\n";
//	headers += L"X-aisino-signature-version:1.0\r\n";
//
//	CString szStrurl(strurl);
//	CString szMethodURL = szStrurl.Right(szStrurl.GetLength() - szStrurl.Find(L"//") - 2);
//	int pos = szMethodURL.Find(L"/");
//	int port = 0;
//	CString szHost = szMethodURL.Left(pos);
//	szMethodURL = szMethodURL.Right(szMethodURL.GetLength() - pos);
//	pos = szHost.Find(L":");
//	if (pos != -1)
//	{
//		CString szPort = szHost.Right(szHost.GetLength() - pos - 1);
//		port = _ttoi(szPort);
//		szHost = szHost.Left(pos);
//	}
//
//	Request request;
//	if (szStrurl.Find(L"https")>-1)
//	{
//		if (port == 0) port = 443;
//		ret = request.HttpsRequest(szHost, port, Method, szMethodURL, headers, strParam);
//	}
//	else
//	{
//		if (port == 0) port = 80;
//		ret = request.HttpRequest(szHost, port, Method, szMethodURL, headers, strParam);
//	}
//	if (ret.IsEmpty())
//		return ret;
//	pos = ret.Find(L"\r\n\r\n");
//	if (pos == -1)
//		return L"";
//	ret = ret.Right(ret.GetLength() - pos - 4);
//	return ret;
//}

CString CHttpsClient::GetWeather()
{
	CString strResult;
	CoInitialize(NULL);
	try
	{
		MSXML2::IServerXMLHTTPRequestPtr xmlrequest;
		xmlrequest.CreateInstance("Msxml2.ServerXMLHTTP");
		if (xmlrequest == NULL)
		{
			strResult.Format(_T("%s"), "ServerXMLHTTP创建失败！");
			return strResult.AllocSysString();
		}
		xmlrequest->open("GET", _bstr_t("http://php.weather.sina.com.cn/iframe/index/w_cl.php?code=js&day=0&city=&dfc=1&charset=utf-8"), FALSE);
		xmlrequest->setRequestHeader(_bstr_t("Content-Type"), _bstr_t("text/xml; charset=utf-8"));
		//xmlrequest->setRequestHeader(_bstr_t("SOAPAction"), _bstr_t("WsAdapterImplService"));
		//xmlrequest->setRequestHeader("Content-Type", "application/octet-stream");
		xmlrequest->setOption(MSXML2::SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, _bstr_t("13056"));
		xmlrequest->setTimeouts(1000, 1000, 1000, 5000);
		xmlrequest->send(_variant_t(""));
		BSTR bstrbody;
		xmlrequest->get_responseText(&bstrbody);
		strResult.Format(_T("%s"), bstrbody);
		strResult.Replace(L"&lt;", L"<");
		strResult.Replace(L"&gt;", L">");
		SysFreeString(bstrbody); // 用完释放 
		xmlrequest.Release();
		//tring szWeather = "(function(){var w=[];w['宁波']=[{s1:'多云',s2:'阵雨',f1:'duoyun',f2:'zhenyu',t1:'28',t2:'21',p1:'3-4',p2:'3-4',d1:'东风',d2:'东南风'}];var add={now:'2017-06-04 9:21:40',time:'1496539300',update:'北京时间06月04日08:10更新',error:'0',total:'1'};window.SWther={w:w,add:add};})();//0";// m_httpsClient.GetWeather();
		CString szCity = getXmlValue(L";w['", L"']=[{s1", strResult);
		CString szWeather = getXmlValue(L"s1:'", L"',s2", strResult);
		CString szT1 = getXmlValue(L"t1:'", L"',t2:", strResult);
		CString szT2 = getXmlValue(L"t2:'", L"',p1", strResult);
		strResult = szCity + L" " + szWeather + L" " + szT2 + L"℃-" + szT1 + L"℃";
		//解析报文 
	}
	catch (_com_error &e)
	{
		strResult.Format(_T("%s"), e.Description());
	}
	CoUninitialize();
	return strResult;
}

CString  CHttpsClient::HttpsPost(LPCTSTR strurl, LPCTSTR strParam)
{
	CString strResult;
	CoInitialize(NULL);
	try
	{
		MSXML2::IServerXMLHTTPRequestPtr xmlrequest;
		xmlrequest.CreateInstance("Msxml2.ServerXMLHTTP");
		if (xmlrequest == NULL)
		{
			strResult.Format(_T("%s"), "ServerXMLHTTP创建失败！");
			return strResult;
		}
		xmlrequest->open("POST", _bstr_t(strurl), FALSE);
		//xmlrequest->setRequestHeader(_bstr_t("Content-Type"), _bstr_t("multipart/form-data"));
		xmlrequest->setRequestHeader(_bstr_t("Content-Type"), _bstr_t("application/x-www-form-urlencoded; charset=utf-8"));
		//xmlrequest->setRequestHeader(_bstr_t("SOAPAction"), _bstr_t("WsAdapterImplService"));
		//xmlrequest->setRequestHeader("Content-Type", "application/octet-stream");
		//xmlrequest->setRequestHeader("Content-Type", "application/json");
		xmlrequest->setTimeouts(10000, 10000, 10000, 30000);
		xmlrequest->setOption(MSXML2::SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, _bstr_t("13056"));
		xmlrequest->send(_variant_t(strParam));
		BSTR bstrbody;
		xmlrequest->get_responseText(&bstrbody);
		strResult.Format(_T("%s"), bstrbody);
		SysFreeString(bstrbody); // 用完释放 
		strResult.Replace(L"&lt;", L"<");
		strResult.Replace(L"&gt;", L">");
		strResult.Replace(L"\r\n", L"");
		xmlrequest.Release();
	}
	catch (_com_error &e)
	{
		strResult.Format(_T("%s"), e.Description());
	}
	CoUninitialize();
	return strResult;
}

CString  CHttpsClient::HttpsPost(LPCTSTR strurl, BYTE* strParam)
{
	CString strResult;
	CoInitialize(NULL);
	try
	{
		MSXML2::IServerXMLHTTPRequestPtr xmlrequest;
		xmlrequest.CreateInstance("Msxml2.ServerXMLHTTP");
		if (xmlrequest == NULL)
		{
			strResult.Format(_T("%s"), "ServerXMLHTTP创建失败！");
			return strResult;
		}
		xmlrequest->open("POST", _bstr_t(strurl), FALSE);
		xmlrequest->setRequestHeader(_bstr_t("Content-Type"), _bstr_t("multipart/form-data"));
		//xmlrequest->setRequestHeader(_bstr_t("SOAPAction"), _bstr_t("WsAdapterImplService"));
		//xmlrequest->setRequestHeader("Content-Type", "application/octet-stream");
		//xmlrequest->setRequestHeader("Content-Type", "application/json");
		xmlrequest->setTimeouts(10000, 10000, 10000, 30000);
		xmlrequest->setOption(MSXML2::SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, _bstr_t("13056"));
		xmlrequest->send(strParam);
		BSTR bstrbody;
		xmlrequest->get_responseText(&bstrbody);
		strResult.Format(_T("%s"), bstrbody);
		SysFreeString(bstrbody); // 用完释放 
		strResult.Replace(L"&lt;", L"<");
		strResult.Replace(L"&gt;", L">");
		strResult.Replace(L"\r\n", L"");
		xmlrequest.Release();
	}
	catch (_com_error &e)
	{
		strResult.Format(_T("%s"), e.Description());
	}
	CoUninitialize();
	return strResult;
}

bool CHttpsClient::UploadFile(LPCTSTR strURL, LPCTSTR strLocalFileName)  //待上传的本地文件路径
{
	ASSERT(strURL != NULL && strLocalFileName != NULL);

	BOOL bResult = FALSE;
	DWORD dwType = 0;
	CString strServer;
	CString strObject;
	INTERNET_PORT wPort = 0;
	DWORD dwFileLength = 0;
	char * pFileBuff = NULL;

	CHttpConnection * pHC = NULL;
	CHttpFile * pHF = NULL;
	//CInternetSession cis;
	afxCurrentAppName = L"YHQSZ";
	CInternetSession cis(L"YHQSZ.exe", 1, INTERNET_OPEN_TYPE_DIRECT);
	bResult = AfxParseURL(strURL, dwType, strServer, strObject, wPort);
	if (!bResult)
		return FALSE;
	CFile file;
	try
	{
		if (!file.Open(strLocalFileName, CFile::shareDenyNone | CFile::modeRead))
			return FALSE;
		dwFileLength = file.GetLength();
		if (dwFileLength <= 0)
			return FALSE;
		pFileBuff = new char[dwFileLength];
		memset(pFileBuff, 0, sizeof(char) * dwFileLength);
		file.Read(pFileBuff, dwFileLength);

		const int nTimeOut = 5000;
		cis.SetOption(INTERNET_OPTION_CONNECT_TIMEOUT, nTimeOut); //联接超时设置
		cis.SetOption(INTERNET_OPTION_CONNECT_RETRIES, 1);  //重试1次
		pHC = cis.GetHttpConnection(strServer, wPort);  //取得一个Http联接

		pHF = pHC->OpenRequest(CHttpConnection::HTTP_VERB_POST, strObject);
		if (!pHF->SendRequest(NULL, 0, pFileBuff, dwFileLength))
		{
			delete[]pFileBuff;
			pFileBuff = NULL;
			pHF->Close();
			pHC->Close();
			cis.Close();
			return FALSE;
		}
		DWORD dwStateCode = 0;
		pHF->QueryInfoStatusCode(dwStateCode);

		//if (dwStateCode == HTTP_STATUS_OK)
		//	bResult = TRUE;

		/*保存http响应*/
		char szChars[MAX_SIZE] = { 0 };
		string strRawResponse = "";
		UINT nReaded = 0;
		while ((nReaded = pHF->Read((void*)szChars, MAX_SIZE)) > 0)
		{
			szChars[nReaded] = '\0';
			strRawResponse += szChars;
			memset(szChars, 0, MAX_SIZE);
		}
		CCharacterHelper characterHelper;
		CString szRes = characterHelper.UTF8ToUnicode(strRawResponse.c_str());

		int posStart = szRes.Find(L"<Code>");
		int posEnd = szRes.Find(L"</Code>");
		CString szCode = szRes.Mid(posStart+6,posEnd-posStart-6);
		if (szCode == L"0000" && dwStateCode == HTTP_STATUS_OK)
			bResult = TRUE;
	}

	catch (CInternetException * pEx)
	{
		TCHAR sz[256] = { 0 };
		pEx->GetErrorMessage(sz, 25);
		CString str;
		str.Format(L"InternetException occur!\r\n%s", sz);

	}
	catch (CFileException& fe)
	{
		CString str;
		str.Format(L"FileException occur!\r\n%d", fe.m_lOsError);

	}
	catch (...)
	{
		DWORD dwError = GetLastError();
		CString str;
		str.Format(L"Unknow Exception occur!\r\n%d", dwError);

	}

	delete[]pFileBuff;
	pFileBuff = NULL;
	file.Close();
	pHF->Close();
	pHC->Close();
	cis.Close();
	return bResult;
}

bool CHttpsClient::DownloadFile(const CString &strURL, const CString &strFN)
{
	//此处第一个参数设为NULL会报错，提示没有ApplicationName，因此随意设置一个
	CInternetSession internetSession(L" ",
		1,
		PRE_CONFIG_INTERNET_ACCESS,
		NULL,
		NULL,
		0);
	BOOL bSucceed = TRUE;
	try
	{
		// 统一以二进制方式下载
		DWORD       dwFlag = INTERNET_FLAG_TRANSFER_BINARY | INTERNET_FLAG_DONT_CACHE | INTERNET_FLAG_RELOAD;
		//子类对基类进行了强制类型转换，利用函数打开要下载的网址链接 wangsl
		//将要下载文件的信息
		CHttpFile   * pF = (CHttpFile*)internetSession.OpenURL(strURL, 1, dwFlag);
		// 得到文件大小
		CString      str;
		pF->QueryInfo(HTTP_QUERY_CONTENT_LENGTH, str);
		int   nFileSize = _ttoi(str);	//文件大小,将字符串转换成整形
										//int nTotalSize = nFileSize;
		if (pF != NULL)
		{
			//创建下载文件，不存在就创建，存在就直接写入 wangsl
			CFile cf;
			if (!cf.Open(strFN, CFile::modeCreate | CFile::modeWrite, NULL))
			{
				return FALSE;
			}
			//
			BYTE Buffer[8192];
			//为Buffer分配空间 wangsl
			ZeroMemory(Buffer, sizeof(Buffer));
			int nReadLen = 0;
			while ((nReadLen = pF->Read(Buffer, sizeof(Buffer))) > 0)	//每次读取固定长度到Buffer中，返回实际读取的长度 wangsl
			{
				cf.Write(Buffer, nReadLen);	//在创建的文件中写入读取的内容 wangsl
				nFileSize -= nReadLen;
				TRACE("剩余:%d\n", nFileSize);
			}
			cf.Close();
			pF->Close();
			delete pF;
		}
	}
	catch (CInternetException * pEx)
	{
		TCHAR sz[256] = { 0 };
		pEx->GetErrorMessage(sz, 25);
		CString str;
		str.Format(L"InternetException occur!\r\n%s", sz);
		AfxMessageBox(str);

	}
	catch (CFileException& fe)
	{
		CString str;
		str.Format(L"FileException occur!\r\n%d", fe.m_lOsError);
		AfxMessageBox(str);

	}
	catch (...)
	{
		DWORD dwError = GetLastError();
		CString str;
		str.Format(L"Unknow Exception occur!\r\n%d", dwError);
		AfxMessageBox(str);

	}
	internetSession.Close();
	if (!bSucceed)
		DeleteFile(strFN);
	return bSucceed;
}


CString  CHttpsClient::Service(LPCTSTR strurl, LPCTSTR postData)
{
	AFX_MANAGE_STATE(AfxGetStaticModuleState());
	CString strResult;
	CoInitialize(NULL);

	DWORD   flags;//上网方式 
	BOOL isConnect; //是否连接网络
	isConnect = ::IsNetworkAlive(&flags);
	if (isConnect == FALSE)  //断网
	{
		return L"";
	}

	try
	{
		MSXML2::IServerXMLHTTPRequestPtr xmlrequest;
		xmlrequest.CreateInstance("Msxml2.ServerXMLHTTP");
		//MSXML2::IServerXMLHTTPRequestPtr xmlrequest;
		//xmlrequest.CreateInstance("Msxml2.ServerXMLHTTP");
		if (xmlrequest == NULL)
		{
			strResult.Format(_T("%s"), L"ServerXMLHTTP创建失败！");
			return strResult;
		}
		xmlrequest->open("GET", _bstr_t(strurl), FALSE);
		//xmlrequest->setRequestHeader(_bstr_t("Content-Length"), _bstr_t("0"));
		xmlrequest->setRequestHeader(_bstr_t("Content-Type"), _bstr_t("application/x-www-form-urlencoded"));
		//xmlrequest->setRequestHeader("Content-Type", "application/octet-stream");
		//xmlrequest->setOption(MSXML2::SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, _bstr_t("13056"));
		xmlrequest->send("");
		xmlrequest->open("POST", _bstr_t(strurl), FALSE);
		xmlrequest->setRequestHeader(_bstr_t("Content-Type"), _bstr_t("text/xml; charset=utf-8"));
		xmlrequest->setRequestHeader(_bstr_t("SOAPAction"), _bstr_t("WsAdapterImplService"));
		//xmlrequest->setOption(MSXML2::SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, _bstr_t("13056"));
		//xmlrequest->setTimeouts(10000, 10000, 10000, 180000);
		//AfxMessageBox(L"1");
		xmlrequest->send(_variant_t(postData));
		//AfxMessageBox(L"2");
		BSTR bstrbody;
		xmlrequest->get_responseText(&bstrbody);
		//AfxMessageBox(L"3");
		strResult.Format(_T("%s"), bstrbody);
		strResult.Replace(L"&lt;", L"<");
		strResult.Replace(L"&gt;", L">");
		strResult.Replace(L"&quot;", L"\"");

		SysFreeString(bstrbody);
		xmlrequest.Release();
		//AfxMessageBox(L"4");
	}
	catch (_com_error &e)
	{
		DWORD i = GetLastError();
		strResult.Format(_T("%s"), e.Description());
	}
	CoUninitialize();
	return strResult;
}

void CHttpsClient::SaveFile(CString filename, CString content)
{
	//char* strANSI = new char[content.GetLength() * 2 + 1];
	//memset(strANSI, 0, content.GetLength() * 2 + 1);
	CCharacterHelper m_characterHelper;
	char* strANSI = m_characterHelper.UnicodeToANSI(content);
	//XC_UnicodeToAnsi(content, content.GetLength(), strANSI, content.GetLength() * 2 + 1);
	CFile cfLog;
	cfLog.Open(filename, CFile::modeCreate | CFile::modeReadWrite | CFile::shareDenyNone);
	cfLog.SeekToEnd();
	cfLog.Write(strANSI, strlen(strANSI));
	cfLog.Close();
	/*delete[] strANSI;
	strANSI = NULL;*/
	free(strANSI);
}

void CHttpsClient::WriteLog(CString filename, CString content)
{
	time_t time1 = time(0);
	CString szCurrentDateTime;
	SYSTEMTIME systm;
	GetLocalTime(&systm);
	szCurrentDateTime.Format(_T("%4d-%.2d-%.2d %.2d:%.2d:%.2d"), systm.wYear, systm.wMonth, systm.wDay, systm.wHour, systm.wMinute, systm.wSecond);
	content = szCurrentDateTime + L"\t" + content + L"\n";
	CString szFile = CTime::GetCurrentTime().Format("%Y-%m-%d") + L".log";
	//char* strANSI = new char[content.GetLength() * 2 + 1];
	//memset(strANSI, 0, content.GetLength() * 2 + 1);
	CCharacterHelper characterHelper;
	char* strANSI = characterHelper.UnicodeToANSI(content);
	//XC_UnicodeToAnsi(content, content.GetLength(), strANSI, content.GetLength() * 2 + 1);
	CFile cfLog;
	cfLog.Open(filename + szFile, CFile::modeWrite | CFile::modeCreate | CFile::modeNoTruncate);//File::modeWrite | CFile::modeCreate | CFile::modeNoTruncate
	cfLog.SeekToEnd();
	cfLog.Write(strANSI, strlen(strANSI));
	cfLog.Close();
	/*delete[] strANSI;
	strANSI = NULL;*/
	free(strANSI);
}

CString CHttpsClient::ReadFile(CString filename)
{

	CFile txtFile1;
	CString strContent = L"";
	CCharacterHelper m_characterHelper;
	BOOL bExist1 = txtFile1.Open(filename, CFile::modeReadWrite | CFile::shareDenyNone);
	if (bExist1)
	{
		DWORD dwLen = txtFile1.GetLength();
		char* FileContent = new char[dwLen+1];
		memset(FileContent, 0, dwLen);//初始化FileContent
		txtFile1.Read(FileContent, dwLen);
		FileContent[dwLen] = 0;
		strContent = m_characterHelper.AnsiToUnicode(FileContent);
		txtFile1.Close();
		delete[] FileContent;
		FileContent = NULL;
	}
	return strContent;
}

CString CHttpsClient::getXmlValue(CString startWord, CString endWord, CString XmlContent)
{
	INT placeStart = 0;
	INT placeEnd = 0;
	CString Xmlresult;
	Xmlresult.Empty();
	placeStart = XmlContent.Find(startWord);
	placeEnd = XmlContent.Find(endWord);
	if (placeStart != -1 && placeEnd != -1)
	{
		Xmlresult = XmlContent.Mid(placeStart + startWord.GetLength(), placeEnd - placeStart - startWord.GetLength());
	}
	return Xmlresult;
}

CString CHttpsClient::GetNodeValue(CString strXML, CString nodeName)
{
	CString result = L"";
	result.Empty();
	int posStart = strXML.Find(L"<" + nodeName + L">");
	int posEnd = strXML.Find(L"</" + nodeName + L">");
	if (posEnd > posStart)
		result = strXML.Mid(posStart + nodeName.GetLength() + 2, posEnd - posStart - nodeName.GetLength() - 2);
	if (result.Find(L"![CDATA[")>-1)
	{
		posStart = result.Find(L"<![CDATA[");
		posEnd = result.Find(L"]]>");
		if (posEnd > posStart)
			result = result.Mid(posStart + 9 , posEnd - posStart - 9 );
	}
	return result;
}
/*
*统一Api入口调度
* @param AppId     APP的唯一标识 typt
* @param Format    返回值的类型，支持JSON与XML。默认为XML
* @param Version   API版本号，目前版本是2.0
* @param Signature 签名结果串，关于签名的计算方法，请参见签名机制。
* @param SignatureMethod   签名方式，目前支持HMAC_SHA256, SM9
SM9签名方式
需要签名的字符串 = 内部报文 + TimeStamp + SignatureNonce
* @param SignatureVersion  签名算法版本，目前版本是1.0
* @param TimeStamp 请求的时间戳。日期格式为：YYYY - MM - DD hh : mm : ss。例如，2014 - 11 - 11 12 : 00 : 00（为北京时间2014年11月11日20点0分0秒）
* @param EncryptMethod 内部报文加密方式 ，1 = AES，2 = 3DES, 3 = SM9
* @param SignatureNonce    唯一随机数，用于防止网络重放攻击。用户在不同请求间要使用不同的随机数值
* @param Id   启用Api对接方式控制填写发送数据方的税号，公司自己开发的客户端填写Token获取解密的Key
* @param AccessKeyId 颁发给用户的密钥ID，主要用于Api对接方式，进行Key的合法性判断
* @param Action 接口名称，系统规定参数，取值：typt
* @param ActionVersion 接口版本号1.0
* @param Val   内部报文(加密)  ActionValue 提交的数据
*/
CString CHttpsClient::PostServeAPI(CString szAppCode, CString szInterCode, CString szToke, CString szContent)
{
	if (szToken.GetLength() == 0)
	{
		OnePostServeAPI(L"");
		if (szToken.GetLength() == 0)
		{
			CString szOutXml = L"<Message>本地时间偏差大，请将时间设置为网络同步</Message>";
			return szOutXml;
		}
	}
	CLogHelper log;
	//char* aeskey = new char[MIN_SIZE];
	CCharacterHelper characterHelper;
	char* aeskey = characterHelper.UnicodeToANSI(szAESKey);
	//XC_UnicodeToAnsi(szAESKey, szAESKey.GetLength(), aeskey, MIN_SIZE);
//	CAESHelper m_AESHelper((unsigned char*)aeskey);
	free(aeskey);
	CString szPlainText = L"<?xml version=\"1.0\" encoding=\"utf-8\" ?><interface><globalInfo><version>1.0</version><applicationCode>"
		+ szAppCode + "</applicationCode><interfaceCode>" + szInterCode + "</interfaceCode><token>" + szToke + "</token></globalInfo><Data><content>"
		+ szContent + "</content></Data></interface>";

	szPlainText = szContent;
	log.WriteLog(szInterCode, szPlainText, L"debug");
	//CString szPostAESParam = m_AESHelper.CombinedEncode(pbPlainText);
	//CString szPostUrl = L"http://192.168.1.124:4006/typt2?actionValue=" + szPostAESParam;
	//CString szOutXml = HttpsPost(szPostUrl, L"");
	//if (szOutXml.GetLength() == 0) return szOutXml;
	//CString szDecodeOutXml = m_AESHelper.CombinedDecode(szOutXml);
	//log.WriteLog(szInterCode, szDecodeOutXml);

	CBase64Helper Base64Helper;
	int nPlainLen = szPlainText.GetLength();
	//char * pPlainText = new char[nPlainLen * 2 + 1];
	//memset(pPlainText, 0, nPlainLen * 2 + 1);

	char * pPlainText = characterHelper.UnicodeToUtf8(szPlainText);
	char * base64code = new char[nPlainLen * 4 + 1];
	memset(base64code, 0, nPlainLen * 4 + 1);
	//int x=XC_UnicodeToAnsi(szPlainText, nPlainLen, pPlainText, nPlainLen * 2 + 1);
	int x = strlen(pPlainText);
	int nBaseLen = Base64Helper.Base64Encode(base64code, pPlainText, x);

	CString szPlainBase64(base64code);

	CString szCurrentDateTime;
	SYSTEMTIME systm;
	GetLocalTime(&systm);
	szCurrentDateTime.Format(_T("%4d-%.2d-%.2d %.2d:%.2d:%.2d"), systm.wYear, systm.wMonth, systm.wDay, systm.wHour, systm.wMinute, systm.wSecond);
	CString szGUID = GUID_Generator();
	//签名
	CString szSignature;
	CHmacsha1 sm9;
	sm9.id = "Client_nbhtxx";
	sm9.key = "S63LsxYpYdD5a3cMBe9yl7YriPKDlje66CUEIY5NKbCTfFikvT4WRcfhLRrCPoDcA3T35xSnCpj7mocpvIqAehaSbgbJB9B7yqea38XJAFESjdQRSKyTsxE5oKP+iPhdVqNt0hoym+4uaCaNMQpiA1A1fjp+zvA4P5iFcxmPEAUwj5oprsyeimnq/b4FHzLDO6c1PaYIJNshZaP1dZZqCIzZgBcfKj/orgMWTEYz4OulxTZqHWA5Eob4VMwloomK09YsUPABbao5pG7UQRlyxSrxs0qJuof6WUHsi4GmRroKBpjQNQnP5Rnba2zaEL9aVdp2oHAMYBbxwmISmGo9TA==";
	sm9.SignatureString(szSignature, szPlainBase64 + szCurrentDateTime + szGUID);
	//int nRes=sm9.VerifySignatureString(szSignature, szPostAESParam + szCurrentDateTime + szGUID);

	int nSigLen = szSignature.GetLength();
	//char *pSing = new char[nSigLen * 2 + 1];
	//memset(pSing, 0, nSigLen * 2 + 1);
	//int len = XC_UnicodeToAnsi(szSignature, nSigLen, pSing, nSigLen * 2 + 1);
	char* pSing = characterHelper.UnicodeToANSI(szSignature);
	int len = strlen(pSing);
	char *pSingURL = new char[nSigLen * 4 + 1];
	memset(pSingURL, 0, nSigLen * 4 + 1);
	URLEncode(pSing, len, pSingURL, nSigLen * 4 + 1);
	CString szSingUrlCode = CString(pSingURL);
	/*delete[] pSing;
	pSing = NULL;*/
	free(pSing);
	delete[] pSingURL;
	pSingURL = NULL;
	free(pPlainText);
	pPlainText = NULL;
	//char *pVal = new char[szPlainBase64.GetLength() * 2 + 1];
	//memset(pVal, 0, szPlainBase64.GetLength() * 2 + 1);
	//len = XC_UnicodeToAnsi(szPlainBase64, szPlainBase64.GetLength(), pVal, MAX_SIZE);

	char *pValURL = new char[nBaseLen * 4 + 1];
	memset(pValURL, 0, nBaseLen * 4 + 1);

	URLEncode(base64code, nBaseLen, pValURL, nBaseLen * 4 + 1);
	CString szValUrlCode = CString(pValURL);
	//delete[] pVal;
	//pVal = NULL;
	delete[] base64code;
	base64code = NULL;
	delete[] pValURL;
	pValURL = NULL;
	CString szPostUrl = L"AppId=" + szAppCode
		+ L"&Format=XML"
		+ L"&Version=2.0"
		+ L"&Signature=" + szSingUrlCode
		+ L"&SignatureMethod=SM9"
		+ L"&SignatureVersion=1.0"
		+ L"&TimeStamp=" + szCurrentDateTime
		+ L"&EncryptMethod=1"
		+ L"&SignatureNonce=" + szGUID
		+ L"&Id=" + szToken
		+ L"&AccessKeyId=" + szAESKey
		+ L"&Action=" + szInterCode
		+ L"&ActionVersion=1.0"
		+ L"&Val=" + szValUrlCode;
	//log.WriteLog(szInterCode, CHttpsClient::szApiUrl+szPostUrl);
	CString szOutXml = HttpsPost(CHttpsClient::szApiUrl, szPostUrl);
	if (L"newCostRenew_userApp" != szInterCode) log.WriteLog(szInterCode, szOutXml, L"debug");
	if (GetNodeValue(szOutXml, L"Code") == L"1002")
	{

	}
	return szOutXml;
}

/*
* @param content 加密报文
* @param timeStamp 时间戳
* @param Signature 签名值
* @param token 令牌
*/
//CString CHttpsClient::PostWebServeAPI(CString szURL, CString szContent, CString szPage)
//{
//	CCharacterHelper characterHelper;
//	//CLogHelper log;
//	//char* aeskey = new char[MIN_SIZE];
//	char* aeskey = characterHelper.UnicodeToANSI(szAESKey);
//	//XC_UnicodeToAnsi(szAESKey, szAESKey.GetLength(), aeskey, MIN_SIZE);
//	//CAESHelper m_AESHelper((unsigned char*)aeskey);
//	free(aeskey);
//	CString szCurrentDateTime;
//	SYSTEMTIME systm;
//	GetLocalTime(&systm);
//	szCurrentDateTime.Format(_T("%4d-%.2d-%.2d %.2d:%.2d:%.2d"), systm.wYear, systm.wMonth, systm.wDay, systm.wHour, systm.wMinute, systm.wSecond);
//	//CString szPostAESParam = m_AESHelper.CombinedEncode(szContent);
//	//log.WriteLog(L"AES加密前", szContent);
//	//log.WriteLog(L"AES秘钥", szAESKey);
//	//log.WriteLog(L"AES加密后", szPostAESParam);
//	//CString aesjm = m_AESHelper.CombinedDecode(szPostAESParam);
//	//签名
//	CString szSignature;
//	CHmacsha1 sm9;
//	sm9.id = "Client_nbhtxx";
//	sm9.key = "S63LsxYpYdD5a3cMBe9yl7YriPKDlje66CUEIY5NKbCTfFikvT4WRcfhLRrCPoDcA3T35xSnCpj7mocpvIqAehaSbgbJB9B7yqea38XJAFESjdQRSKyTsxE5oKP+iPhdVqNt0hoym+4uaCaNMQpiA1A1fjp+zvA4P5iFcxmPEAUwj5oprsyeimnq/b4FHzLDO6c1PaYIJNshZaP1dZZqCIzZgBcfKj/orgMWTEYz4OulxTZqHWA5Eob4VMwloomK09YsUPABbao5pG7UQRlyxSrxs0qJuof6WUHsi4GmRroKBpjQNQnP5Rnba2zaEL9aVdp2oHAMYBbxwmISmGo9TA==";
//	sm9.SignatureString(szSignature, szPostAESParam + szCurrentDateTime + szToken);
//	int nRes = sm9.VerifySignatureString(szSignature, szPostAESParam + szCurrentDateTime + szToken);
//
//	int nSigLen = szSignature.GetLength();
//	//char *pSing = new char[nSigLen * 2 + 1];
//	//memset(pSing, 0, nSigLen * 2 + 1);
//	char* pSing = characterHelper.UnicodeToANSI(szSignature);
//	//int len = XC_UnicodeToAnsi(szSignature, nSigLen, pSing, nSigLen * 2 + 1);
//
//	char *pSingURL = new char[nSigLen * 4 + 1];
//	memset(pSingURL, 0, nSigLen * 4 + 1);
//	URLEncode(pSing, strlen(pSing), pSingURL, nSigLen * 4 + 1);
//	CString szSingUrlCode = CString(pSingURL);
//	/*delete[] pSing;
//	pSing = NULL;*/
//	free(pSing);
//	delete[] pSingURL;
//	pSingURL = NULL;
//
//	char *pVal = new char[szPostAESParam.GetLength() * 2 + 1];
//	memset(pVal, 0, szPostAESParam.GetLength() * 2 + 1);
//	pVal = characterHelper.UnicodeToANSI(szPostAESParam);
//	//len = XC_UnicodeToAnsi(szPostAESParam, szPostAESParam.GetLength(), pVal, MAX_SIZE);
//	int len = strlen(pVal);
//	char *pValURL = new char[len * 4 + 1];
//	memset(pValURL, 0, len * 4 + 1);
//
//	URLEncode(pVal, len, pValURL, len * 4 + 1);
//	CString szValUrlCode = CString(pValURL);
//
//	CString szPostUrl = szURL + szPage + "?content=" + szValUrlCode
//		+ L"&timeStamp=" + szCurrentDateTime
//		+ L"&Signature=" + szSingUrlCode
//		+ L"&token=" + szToken;
//	CLogHelper log;
//	//log.WriteLog(szPage,szPostUrl);
//	return szPostUrl;
//}
//

CString CHttpsClient::OnePostServeAPI(CString szVal)
{
	CString szCurrentDateTime;
	CString szValSM9;
	char* valsm9 = new char[MIN_SIZE];
	char urlcode[MAX_SIZE] = { 0 };
	int nVal = 0;
	SYSTEMTIME systm;
	GetLocalTime(&systm);
	szCurrentDateTime.Format(_T("%4d-%.2d-%.2d %.2d:%.2d:%.2d"), systm.wYear, systm.wMonth, systm.wDay, systm.wHour, systm.wMinute, systm.wSecond);
	CHmacsha1 sm9;
	sm9.id = "www.nbhtxx.com.cn";
	sm9.EncodeString(szCurrentDateTime, szValSM9, nVal);
	//CString szValSM92;
	//sm9.EncodeString(L"<?xml version=\"1.0\" encoding=\"utf-8\" ?><interface><globalInfo><version>3.0</version><interfaceCode>ZZFPCX</interfaceCode><token>330201999999868</token></globalInfo><Data><content>PEZQWFg+PE5TUlNCSD4zMzAyMDE5OTk5OTk4Njg8L05TUlNCSD48RERMU0g+MzMwMjAzOTk5OTk5MDE5MjAxNzA5MTkyMDA2MTI2NDM8L0RETFNIPjxGUERNPjMzMDIxNzEzMjA8L0ZQRE0+PEZQSE0+MDAwNzE1NzM8L0ZQSE0+PC9GUFhYPg==</content></Data></interface>", szValSM92, nVal);
	//int len = XC_UnicodeToAnsi(szValSM9, szValSM9.GetLength(), valsm9, MAX_SIZE);
	CCharacterHelper character;
	valsm9 =character.UnicodeToANSI(szValSM9);
	//UTF-8
	int nLen = ::MultiByteToWideChar(CP_UTF8, 0, valsm9, -1, NULL, 0);
	//返回需要的unicode长度     
	WCHAR * wszANSI = new WCHAR[nLen + 1];
	memset(wszANSI, 0, nLen * 2 + 2);
	nLen = MultiByteToWideChar(CP_UTF8, 0, valsm9, -1, wszANSI, nLen);    //把utf8转成unicode    
	nLen = WideCharToMultiByte(CP_ACP, 0, wszANSI, -1, NULL, 0, NULL, NULL);        //得到要的ansi长度     
	char *szANSI = new char[nLen + 1];
	memset(szANSI, 0, nLen + 1);
	nLen = WideCharToMultiByte(CP_ACP, 0, wszANSI, -1, szANSI, nLen, NULL, NULL);          //把unicode转成ansi    

	URLEncode(szANSI, nLen, urlcode, MAX_SIZE);

	CString szPostUrl = CHttpsClient::szApiUrl + L"/KeyRun?Val=" + CString(urlcode);
	CLogHelper log;
	log.WriteLog(L"api请求", szPostUrl, L"debug");
	CString szOutXml = HttpsPost(szPostUrl, L"");
	log.WriteLog(L"api返回", szOutXml, L"debug");
	CString szCode = GetNodeValue(szOutXml, L"Code");
	if (szCode == L"0000")
	{
		CString szDataSm9 = GetNodeValue(szOutXml, L"Data");
		CString szData;
		sm9.id = "Client_nbhtxx";
		sm9.key = "S63LsxYpYdD5a3cMBe9yl7YriPKDlje66CUEIY5NKbCTfFikvT4WRcfhLRrCPoDcA3T35xSnCpj7mocpvIqAehaSbgbJB9B7yqea38XJAFESjdQRSKyTsxE5oKP+iPhdVqNt0hoym+4uaCaNMQpiA1A1fjp+zvA4P5iFcxmPEAUwj5oprsyeimnq/b4FHzLDO6c1PaYIJNshZaP1dZZqCIzZgBcfKj/orgMWTEYz4OulxTZqHWA5Eob4VMwloomK09YsUPABbao5pG7UQRlyxSrxs0qJuof6WUHsi4GmRroKBpjQNQnP5Rnba2zaEL9aVdp2oHAMYBbxwmISmGo9TA==";
		sm9.DecodeString(szData, szDataSm9, nVal);
		szToken = GetNodeValue(szData, L"token");
		szAESKey = GetNodeValue(szData, L"Key");
		return L"";
	}
	else
	{
		szToken = L"";
		szAESKey = L"";
		CString szMessage = GetNodeValue(szOutXml, L"Message");
		return szMessage;
	}

}
CString CHttpsClient::PostServeAPIforUpdate(CString szAppCode, CString szInterCode, CString szToke, CString szContent)
{
	if (szToken.GetLength() == 0)
	{
		//OnePostServeAPI(L"");
		if (szToken.GetLength() == 0)
		{
			CString szOutXml = L"<Message>本地时间偏差大，请将时间设置为网络同步</Message>";
			return szOutXml;
		}
	}
	CLogHelper log;
	//char* aeskey = new char[MIN_SIZE];
	CCharacterHelper characterHelper;
	char* aeskey = characterHelper.UnicodeToANSI(szAESKey);
	//XC_UnicodeToAnsi(szAESKey, szAESKey.GetLength(), aeskey, MIN_SIZE);
//	CAESHelper m_AESHelper((unsigned char*)aeskey);
	free(aeskey);
	CString szPlainText = L"<?xml version=\"1.0\" encoding=\"utf-8\" ?><interface><globalInfo><version>1.0</version><applicationCode>"
		+ szAppCode + "</applicationCode><interfaceCode>" + szInterCode + "</interfaceCode><token>" + szToke + "</token></globalInfo><Data><content>"
		+ szContent + "</content></Data></interface>";

	szPlainText = szContent;
	log.WriteLog(szInterCode, szPlainText, L"debug");
	//CString szPostAESParam = m_AESHelper.CombinedEncode(pbPlainText);
	//CString szPostUrl = L"http://192.168.1.124:4006/typt2?actionValue=" + szPostAESParam;
	//CString szOutXml = HttpsPost(szPostUrl, L"");
	//if (szOutXml.GetLength() == 0) return szOutXml;
	//CString szDecodeOutXml = m_AESHelper.CombinedDecode(szOutXml);
	//log.WriteLog(szInterCode, szDecodeOutXml);

	CBase64Helper Base64Helper;
	int nPlainLen = szPlainText.GetLength();
	//char * pPlainText = new char[nPlainLen * 2 + 1];
	//memset(pPlainText, 0, nPlainLen * 2 + 1);

	char * pPlainText = characterHelper.UnicodeToUtf8(szPlainText);
	char * base64code = new char[nPlainLen * 4 + 1];
	memset(base64code, 0, nPlainLen * 4 + 1);
	//int x=XC_UnicodeToAnsi(szPlainText, nPlainLen, pPlainText, nPlainLen * 2 + 1);
	int x = strlen(pPlainText);
	int nBaseLen = Base64Helper.Base64Encode(base64code, pPlainText, x);

	CString szPlainBase64(base64code);

	CString szCurrentDateTime;
	SYSTEMTIME systm;
	GetLocalTime(&systm);
	szCurrentDateTime.Format(_T("%4d-%.2d-%.2d %.2d:%.2d:%.2d"), systm.wYear, systm.wMonth, systm.wDay, systm.wHour, systm.wMinute, systm.wSecond);
	CString szGUID = GUID_Generator();
	//签名
	CString szSignature;
	CHmacsha1 sm9;
	sm9.id = "Client_nbhtxx";
	sm9.key = "S63LsxYpYdD5a3cMBe9yl7YriPKDlje66CUEIY5NKbCTfFikvT4WRcfhLRrCPoDcA3T35xSnCpj7mocpvIqAehaSbgbJB9B7yqea38XJAFESjdQRSKyTsxE5oKP+iPhdVqNt0hoym+4uaCaNMQpiA1A1fjp+zvA4P5iFcxmPEAUwj5oprsyeimnq/b4FHzLDO6c1PaYIJNshZaP1dZZqCIzZgBcfKj/orgMWTEYz4OulxTZqHWA5Eob4VMwloomK09YsUPABbao5pG7UQRlyxSrxs0qJuof6WUHsi4GmRroKBpjQNQnP5Rnba2zaEL9aVdp2oHAMYBbxwmISmGo9TA==";
	sm9.SignatureString(szSignature, szPlainBase64 + szCurrentDateTime + szGUID);
	//int nRes=sm9.VerifySignatureString(szSignature, szPostAESParam + szCurrentDateTime + szGUID);

	int nSigLen = szSignature.GetLength();
	//char *pSing = new char[nSigLen * 2 + 1];
	//memset(pSing, 0, nSigLen * 2 + 1);
	//int len = XC_UnicodeToAnsi(szSignature, nSigLen, pSing, nSigLen * 2 + 1);
	char* pSing = characterHelper.UnicodeToANSI(szSignature);
	char *pSingURL = new char[nSigLen * 4 + 1];
	memset(pSingURL, 0, nSigLen * 4 + 1);
	URLEncode(pSing, strlen(pSing), pSingURL, nSigLen * 4 + 1);
	CString szSingUrlCode = CString(pSingURL);
	/*delete[] pSing;
	pSing = NULL;*/
	free(pSing);
	delete[] pSingURL;
	pSingURL = NULL;
	free(pPlainText);
	pPlainText = NULL;
	//char *pVal = new char[szPlainBase64.GetLength() * 2 + 1];
	//memset(pVal, 0, szPlainBase64.GetLength() * 2 + 1);
	//len = XC_UnicodeToAnsi(szPlainBase64, szPlainBase64.GetLength(), pVal, MAX_SIZE);

	char *pValURL = new char[nBaseLen * 4 + 1];
	memset(pValURL, 0, nBaseLen * 4 + 1);

	URLEncode(base64code, nBaseLen, pValURL, nBaseLen * 4 + 1);
	CString szValUrlCode = CString(pValURL);
	//delete[] pVal;
	//pVal = NULL;
	delete[] base64code;
	base64code = NULL;
	delete[] pValURL;
	pValURL = NULL;
	CString szPostUrl = L"AppId=" + szAppCode
		+ L"&Format=XML"
		+ L"&Version=2.0"
		+ L"&Signature=" + szSingUrlCode
		+ L"&SignatureMethod=SM9"
		+ L"&SignatureVersion=1.0"
		+ L"&TimeStamp=" + szCurrentDateTime
		+ L"&EncryptMethod=1"
		+ L"&SignatureNonce=" + szGUID
		+ L"&Id=" + szToken
		+ L"&AccessKeyId=" + szAESKey
		+ L"&Action=" + szInterCode
		+ L"&ActionVersion=1.2"
		+ L"&Val=" + szValUrlCode;
	log.WriteLog(szInterCode, szPostUrl, L"debug");
	CString szOutXml = HttpsPost(CHttpsClient::szApiUrl, szPostUrl);
	if (L"newCostRenew_userApp" != szInterCode) log.WriteLog(szInterCode, szOutXml, L"debug");
	if (GetNodeValue(szOutXml, L"Code") == L"1002")
	{

	}
	return szOutXml;
}

CString CHttpsClient::DowdSoftAPI(CString szSoftCode, CString szSoftVer)
{
	//CString szPostParam = L"<REQUEST_INFO><SOFT_CODE>" + szSoftCode + "</SOFT_CODE><SOFT_VERSION>" + szSoftVer + "</SOFT_VERSION><QY>" + CCLoginWnd::szQY + L"</QY><SOFT_TYPE>1</SOFT_TYPE><TOKEN>" + CHttpsClient::szLoginToken + L"</TOKEN></REQUEST_INFO>";
	//CString szResXml = PostServeAPIforUpdate(L"typt", L"khdsj_sj", L"11111111", szPostParam);
	//CString szCode = GetNodeValue(szResXml, L"Code");
	//CString szCODE = GetNodeValue(szResXml, L"CODE");
	//if (szCode == L"0000" && szCODE == L"0000")
	//{
	//	return szResXml;
	//}
	return L"";
}

CString CHttpsClient::GetIP()
{
	CString rtn = HttpsGetRz(L"http://2019.ip138.com/ic.asp");
	CString res = getXmlValue(L"[", L"]", rtn);
	if (res.GetLength() == 0)
	{
		rtn = HttpsGetRz(L"https://ifconfig.me/");
		res = getXmlValue(L"<strong id=\"ip_address\">", L"</strong>", rtn);
	}
	return res;
}
