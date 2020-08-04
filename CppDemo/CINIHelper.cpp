#include "stdafx.h"
#include "CINIHelper.h"

void CINIHelper::Init()
{
	m_unMaxSection = 512;
	m_unSectionNameMaxSize = 33; // 32λUID��     
}

CINIHelper::CINIHelper()
{
	Init();
}

CINIHelper::CINIHelper(LPCTSTR szFileName)
{
	// (1) ����·���������·���Ƿ����     
	// (2) ��"./"��ͷ������������·���Ƿ����     
	// (3) ��"../"��ͷ�����漰���·���Ľ���     

	Init();

	// ���·��     
	m_szFileName.Format(_T("%s"), szFileName);
}

CINIHelper::~CINIHelper()
{

}

//�����ļ���
void CINIHelper::SetFileName(LPCTSTR szFileName)
{
	m_szFileName.Format(_T(".//%s"), szFileName);
}

//��ȡ�ļ���
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
//��ȡKeyValue������ֵΪDWORD
DWORD CINIHelper::GetProfileString(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, CString& szKeyValue)
{
	DWORD dwCopied = 0;
	dwCopied = GetPrivateProfileString(lpszSectionName, lpszKeyName,L"",
		szKeyValue.GetBuffer(MAX_PATH), MAX_PATH, m_szFileName);
	szKeyValue.ReleaseBuffer();

	return dwCopied;
}
//��ȡKeyValue������ֵΪint
int CINIHelper::GetProfileInt(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName)
{
	int nKeyValue = GetPrivateProfileInt(lpszSectionName, lpszKeyName, 0, m_szFileName);

	return nKeyValue;
}
//���ýڡ�����ֵ��������� lpszSectionName-�ڵ�  lpszKeyName-����   lpszKeyValue-ֵ
BOOL CINIHelper::SetProfileString(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, LPCTSTR lpszKeyValue)
{
	return WritePrivateProfileString(lpszSectionName, lpszKeyName, lpszKeyValue, m_szFileName);
}
//���ýڡ�����ֵ��������� lpszSectionName-�ڵ�  lpszKeyName-����   nKeyValue-ֵ
BOOL CINIHelper::SetProfileInt(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, int nKeyValue)
{
	CString szKeyValue;
	szKeyValue.Format(_T("%d"), nKeyValue);

	return WritePrivateProfileString(lpszSectionName, lpszKeyName, szKeyValue, m_szFileName);
}
//���ݽڵ���ɾ���ڵ�
BOOL CINIHelper::DeleteSection(LPCTSTR lpszSectionName)
{
	return WritePrivateProfileSection(lpszSectionName, NULL, m_szFileName);
}
//���ݽڵ����ͼ�����ɾ����ֵ��
BOOL CINIHelper::DeleteKey(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName)
{
	return WritePrivateProfileString(lpszSectionName, lpszKeyName, NULL, m_szFileName);
}