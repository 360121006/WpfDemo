///////////////////////////////////////////////////////////////////////////// 
// �ļ���: <ZipImplement.h> 
// ˵��:ѹ����ѹ���ļ��� 
///////////////////////////////////////////////////////////////////////////// 

#pragma once 
//#include "stdafx.h"
#include <atlconv.h>
#include "Zip.h" 
#include "Unzip.h" 
class CZipImplement 
{ 
public: 
	CZipImplement(void); 
	~CZipImplement(void); 

private: 
	HZIP hz;//Zip�ļ���� 
	ZRESULT zr;//��������ֵ 
	ZIPENTRY ze;//Zip�ļ���� 
	CString m_FolderPath;//folder·�� 
	CString  m_FolderName;//folder��Ҫ��ѹ�����ļ����� 

private: 
	//ʵ�ֱ����ļ��� 
	void BrowseFile(CString &strFile); 

	//��ȡ���·�� 
	void GetRelativePath(CString& pFullPath, CString& pSubString); 

	//����·�� 
	BOOL CreatedMultipleDirectory(wchar_t* direct); 


public: 
	void getFiles(string path, vector<string>& files);
	//ѹ���ļ��нӿ� 
	BOOL Zip_PackFiles(CString& pFilePath, CString& mZipFileFullPath, CStringA strPW = ""); 

	BOOL Zip_PackSbFiles(CString& pFilePath, CString& mZipFileFullPath, CStringA strPW="");

	//��ѹ���ļ��нӿ� 
	BOOL Zip_UnPackFiles(CString &mZipFileFullPath, CString& mUnPackPath, CStringA strPW = ""); 

	BOOL Zip_PackFile(CString& pFilePath, CString& mZipFileFullPath, CStringA strPW);
public: 
	//��̬�����ṩ�ļ���·����� 
	static BOOL FolderExist(CString& strPath); 
};
