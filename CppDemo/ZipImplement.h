///////////////////////////////////////////////////////////////////////////// 
// 文件名: <ZipImplement.h> 
// 说明:压缩解压缩文件夹 
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
	HZIP hz;//Zip文件句柄 
	ZRESULT zr;//操作返回值 
	ZIPENTRY ze;//Zip文件入口 
	CString m_FolderPath;//folder路径 
	CString  m_FolderName;//folder将要被压缩的文件夹名 

private: 
	//实现遍历文件夹 
	void BrowseFile(CString &strFile); 

	//获取相对路径 
	void GetRelativePath(CString& pFullPath, CString& pSubString); 

	//创建路径 
	BOOL CreatedMultipleDirectory(wchar_t* direct); 


public: 
	void getFiles(string path, vector<string>& files);
	//压缩文件夹接口 
	BOOL Zip_PackFiles(CString& pFilePath, CString& mZipFileFullPath, CStringA strPW = ""); 

	BOOL Zip_PackSbFiles(CString& pFilePath, CString& mZipFileFullPath, CStringA strPW="");

	//解压缩文件夹接口 
	BOOL Zip_UnPackFiles(CString &mZipFileFullPath, CString& mUnPackPath, CStringA strPW = ""); 

	BOOL Zip_PackFile(CString& pFilePath, CString& mZipFileFullPath, CStringA strPW);
public: 
	//静态方法提供文件夹路径检查 
	static BOOL FolderExist(CString& strPath); 
};
