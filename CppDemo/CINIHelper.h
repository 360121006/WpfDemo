//#include <afxmt.h>
//class  CINIHelper
//{
//public:
//	 CINIHelper();
//	 virtual~ CINIHelper();
//	 //���sectionֵ
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
	//�����ļ���
	void SetFileName(LPCTSTR szFileName);

	//���ýڡ�����ֵ��������� lpszSectionName-�ڵ�  lpszKeyName-����   nKeyValue-ֵ  
	BOOL SetProfileInt(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, int nKeyValue);

	//���ýڡ�����ֵ��������� lpszSectionName-�ڵ�  lpszKeyName-����   lpszKeyValue-ֵ
	BOOL SetProfileString(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, LPCTSTR lpszKeyValue);

	//��ȡ�ļ���
	DWORD GetProfileSectionNames(CStringArray& strArray); // ����section����  

    //��ȡKeyValue������ֵΪint
	int GetProfileInt(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName);

	//��ȡKeyValue������ֵΪDWORD
	DWORD GetProfileString(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName, CString& szKeyValue);

	//���ݽڵ���ɾ���ڵ�
	BOOL DeleteSection(LPCTSTR lpszSectionName);

	//���ݽڵ����ͼ�����ɾ����ֵ��
	BOOL DeleteKey(LPCTSTR lpszSectionName, LPCTSTR lpszKeyName);

private:
	CString  m_szFileName; // .//Config.ini, ������ļ������ڣ���exe��һ����ͼWriteʱ���������ļ�     

	UINT m_unMaxSection; // ���֧�ֵ�section��(256)     
	UINT m_unSectionNameMaxSize; // section���Ƴ��ȣ�������Ϊ32(Null-terminated)     

	void Init();
};

#endif     