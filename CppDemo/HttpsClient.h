/*
* Copyright (c) 2017 宁波爱信诺航天信息有限公司
* All rights reserved.
*
* 文件名称：HttpsClient.h
* 文件标识：HTTPS  POST GET 操作类
* 摘 要：简要描述本文件的内容
*
* 当前版本：1.0
* 作 者：叶阊
* 完成日期：2017年05月5日
*/
#pragma once
#include "afxtempl.h"
#import "msxml3.dll" 
#include "Common.h"
#include "tinystr.h"
#include "tinyxml.h"
//typedef struct _Data
//{
//	CString  szToken;
//	CString  szKey;
//}DATA;
//
//DATA g_data;
class CHttpsClient
{
	
public:
	CHttpsClient();
	~CHttpsClient();
	CString HttpsGetRz(LPCTSTR strurl);
	CString HttpPostforLogin(LPCTSTR strurl, LPCTSTR strParam, LPCTSTR strToken);
	CString CommonHttpRequest(char * Method, LPCTSTR strurl, LPCTSTR strParam, LPCTSTR strApiKey, LPCTSTR MethodURL, LPCTSTR acceptType, LPCTSTR contentType=L"application/xml");
	CString SocketHttpsRequest(char * Method, LPCTSTR strurl, LPCTSTR strParam, LPCTSTR strApiKey, LPCTSTR MethodURL, LPCTSTR acceptType);
	CString GetWeather();
	CString GetMacByCmd();
	CString GetOsInfo();
	CString HttpsPost(LPCTSTR strurl, LPCTSTR strParam);
	CString HttpsPost(LPCTSTR strurl, BYTE* strParam);
	bool UploadFile(LPCTSTR strURL, LPCTSTR strLocalFileName);
	bool DownloadFile(const CString &strURL, const CString &strFN);
	CString Service(LPCTSTR strurl, LPCTSTR postData);
	void SaveFile(CString filename, CString content);
	CString ReadFile(CString filename);
	void WriteLog(CString filename, CString content);
	CString GetNodeValue(CString strXML, CString nodeName);
	CString getXmlValue(CString startWord, CString endWord, CString XmlContent);
	CString PostServeAPI(CString szAppCode, CString szInterCode, CString szToke, CString szContent);
	CString OnePostServeAPI(CString szVal);
	CString PostWebServeAPI(CString szURL, CString szContent, CString szPage);
	int URLEncode(const char* str, int strSize, char* result, const int resultSize);
	CString DowdSoftAPI(CString szSoftCode, CString szSoftVer);
	CString GetIP();
	CString GUID_Generator();
	CString PostServeAPIforUpdate(CString szAppCode, CString szInterCode, CString szToke, CString szContent);
	static CString  szToken;
	static CString  szLoginToken;
	static CString  szAESKey;
	static CString  szApiUrl;
	static CString  szCentApiUrl;
	static CString  szStaticUrl;
	//CCharacterHelper m_characterHelper;
};

