//#include <afxmt.h>
//class  CINIHelper
//{
//public:
//	 CINIHelper();
//	 virtual~ CINIHelper();
//	 //获得section值
//	 int GetSectionValue(CString &strName);
//	 CString GetSectionValueS(CString &strName);
//	 BOOL SetSectionValue(LPCTSTR strValue,CString &strName);
//
//private:
//	CString sFilePath;
//	CString strSection;
//	CString strIniFileName;
//};
//
#ifndef __INIFILE_H__     
#define __INIFILE_H__  

#include <afx.h>
#include "windows.h"


class CINIHelper
{
public:
	CINIHelper();
	CINIHelper(LPCTSTR szFileName);
	virtual ~CINIHelper();
	//设置文件名
	void SetFileName(LPCTSTR szFileName);

	//设置节、键、值：传入参数 lpszSectionName-节点  lpszKeyName-键名   nKeyValue-值  
	BOOL SetProfileInt(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, int nKeyValue);

	//设置节、键、值：传入参数 lpszSectionName-节点  lpszKeyName-键名   lpszKeyValue-值
	BOOL SetProfileString(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, LPCTSTR lpszKeyValue);

	//获取文件名
	DWORD GetProfileSectionNames(CStringArray& strArray); // 返回section数量  

    //获取KeyValue，返回值为int
	int GetProfileInt(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName);

	//获取KeyValue，返回值为DWORD
	DWORD GetProfileString(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, CString& szKeyValue);

	//根据节点名删除节点
	BOOL DeleteSection(LPCTSTR lpszSectionName);

	//根据节点名和键名，删除键值对
	BOOL DeleteKey(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName);

private:
	CString  m_szFileName; // .//Config.ini, 如果该文件不存在，则exe第一次试图Write时将创建该文件     

	UINT m_unMaxSection; // 最多支持的section数(256)     
	UINT m_unSectionNameMaxSize; // section名称长度，这里设为32(Null-terminated)     

	void Init();
};

#endif     