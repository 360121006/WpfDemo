#include "stdafx.h"
#include "CINIHelper.h"

void CINIHelper::Init()
{
	m_unMaxSection = 512;
	m_unSectionNameMaxSize = 33; // 32位UID串     
}

CINIHelper::CINIHelper()
{
	Init();
}

CINIHelper::CINIHelper(LPCTSTR szFileName)
{
	// (1) 绝对路径，需检验路径是否存在     
	// (2) 以"./"开头，则需检验后续路径是否存在     
	// (3) 以"../"开头，则涉及相对路径的解析     

	Init();

	// 相对路径     
	m_szFileName.Format(_T("%s"), szFileName);
}

CINIHelper::~CINIHelper()
{

}

//设置文件名
void CINIHelper::SetFileName(LPCTSTR szFileName)
{
	m_szFileName.Format(_T(".//%s"), szFileName);
}

//获取文件名
DWORD CINIHelper::GetProfileSectionNames(CStringArray &strArray)
{
	int nAllSectionNamesMaxSize = m_unMaxSection*m_unSectionNameMaxSize + 1;
	TCHAR *pszSectionNames = new TCHAR[nAllSectionNamesMaxSize];
	DWORD dwCopied = 0;
	dwCopied = GetPrivateProfileSectionNames(pszSectionNames, nAllSectionNamesMaxSize, m_szFileName);

	strArray.RemoveAll();

	TCHAR *pSection = pszSectionNames;
	do
	{
		CString szSection(pSection);
		if (szSection.GetLength() < 1)
		{
			delete[] pszSectionNames;
			return dwCopied;
		}
		strArray.Add(szSection);

		pSection = pSection + szSection.GetLength() + 1; // next section name     
	} while (pSection && pSection<pszSectionNames + nAllSectionNamesMaxSize);

	delete[] pszSectionNames;
	pszSectionNames = NULL;
	return dwCopied;
}
//获取KeyValue，返回值为DWORD
DWORD CINIHelper::GetProfileString(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, CString& szKeyValue)
{
	DWORD dwCopied = 0;
	dwCopied = GetPrivateProfileString(lpszSectionName, lpszKeyName,L"",
		szKeyValue.GetBuffer(MAX_PATH), MAX_PATH, m_szFileName);
	szKeyValue.ReleaseBuffer();

	return dwCopied;
}
//获取KeyValue，返回值为int
int CINIHelper::GetProfileInt(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName)
{
	int nKeyValue = GetPrivateProfileInt(lpszSectionName, lpszKeyName, 0, m_szFileName);

	return nKeyValue;
}
//设置节、键、值：传入参数 lpszSectionName-节点  lpszKeyName-键名   lpszKeyValue-值
BOOL CINIHelper::SetProfileString(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, LPCTSTR lpszKeyValue)
{
	return WritePrivateProfileString(lpszSectionName, lpszKeyName, lpszKeyValue, m_szFileName);
}
//设置节、键、值：传入参数 lpszSectionName-节点  lpszKeyName-键名   nKeyValue-值
BOOL CINIHelper::SetProfileInt(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, int nKeyValue)
{
	CString szKeyValue;
	szKeyValue.Format(_T("%d"), nKeyValue);

	return WritePrivateProfileString(lpszSectionName, lpszKeyName, szKeyValue, m_szFileName);
}
//根据节点名删除节点
BOOL CINIHelper::DeleteSection(LPCTSTR lpszSectionName)
{
	return WritePrivateProfileSection(lpszSectionName, NULL, m_szFileName);
}
//根据节点名和键名，删除键值对
BOOL CINIHelper::DeleteKey(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName)
{
	return WritePrivateProfileString(lpszSectionName, lpszKeyName, NULL, m_szFileName);
}