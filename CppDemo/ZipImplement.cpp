///////////////////////////////////////////////////////////////////////////// 
// 文件名: <ZipImplement.cpp> 
// 说明:压缩解压缩文件夹 
///////////////////////////////////////////////////////////////////////////// 
#include "stdafx.h"
#include "zipimplement.h" 
#include <direct.h> 
#include <vector> 
#include <xstring> 

CZipImplement::CZipImplement(void) 
{ 
} 

CZipImplement::~CZipImplement(void) 
{ 
} 

void CZipImplement::getFiles(string path, vector<string>& files)
{
	//文件句柄  
	long   hFile = 0;
	//文件信息  
	struct _finddata_t fileinfo;
	string p;
	if ((hFile = _findfirst(p.assign(path).append("\\*").c_str(), &fileinfo)) != -1)
	{
		do
		{
			//如果是目录,迭代之  
			//如果不是,加入列表  
			if ((fileinfo.attrib &  _A_SUBDIR))
			{
				if (strcmp(fileinfo.name, ".") != 0 && strcmp(fileinfo.name, "..") != 0)
					getFiles(p.assign(path).append("\\").append(fileinfo.name), files);
			}
			else
			{
				files.push_back(p.assign(path).append("\\").append(fileinfo.name));
			}
		} while (_findnext(hFile, &fileinfo) == 0);
		_findclose(hFile);
	}
}

///////////////////////////////////////////////////////////////////////////// 
// 函数说明: 实现压缩文件夹操作 
// 参数说明: [in]： 
//					pFilePath			要被压缩的文件夹 
//					mZipFileFullPath	压缩后的文件名和路径 
//					strPW				压缩密码，可空，默认为空，表示不带密码压缩
// 返回值: 参数有误的情况下返回FALSE,压缩成功后返回TRUE 
///////////////////////////////////////////////////////////////////////////// 
BOOL CZipImplement::Zip_PackFiles(CString& pFilePath, CString& mZipFileFullPath, CStringA strPW) 
{ 
    //参数错误 
    if ((pFilePath == L"") || (mZipFileFullPath == L"")) 
    { 
        //路径异常返回 
        return FALSE ; 
    } 

    if(!CZipImplement::FolderExist(pFilePath)) 
    { 
        //要被压缩的文件夹不存在 
        return FALSE ; 
    } 

    CString tZipFilePath = mZipFileFullPath.Left(mZipFileFullPath.ReverseFind('\\') + 1); 
    if(!CZipImplement::FolderExist(tZipFilePath)) 
   { 
        //ZIP文件存放的文件夹不存在创建它 
		CStringW strWZipFilePath(tZipFilePath);
		if (FALSE == CreatedMultipleDirectory((LPWSTR)(LPCWSTR)strWZipFilePath)) 
		{ 
			//创建目录失败 
			return FALSE; 
		} 
    } 

    //获得文件夹的名字 
    if(pFilePath.Right(1) == L"\\") 
    { 
        this->m_FolderPath = pFilePath.Left(pFilePath.GetLength() - 1); 
        m_FolderName = m_FolderPath.Right(m_FolderPath.GetLength() - m_FolderPath.ReverseFind('\\') - 1); 
    } 
    else 
    { 
        this->m_FolderPath = pFilePath; 
       m_FolderName = pFilePath.Right(pFilePath.GetLength() - pFilePath.ReverseFind('\\') - 1); 
    } 

    /************************************************************************/ 

    //创建ZIP文件
	if (strPW.IsEmpty())
	{
		hz=CreateZip(mZipFileFullPath,0); 
	}
	else
	{
		hz=CreateZip(mZipFileFullPath,(LPCSTR)strPW); 
	}    
    if(hz == 0) 
    { 
        //创建Zip文件失败 
        return FALSE; 
    } 

    //递归文件夹,将获取的问价加入ZIP文件 
    BrowseFile(pFilePath); 
    //关闭ZIP文件 
    CloseZip(hz); 

    /************************************************************************/ 

    CFileFind tFFind; 
    if (!tFFind.FindFile(mZipFileFullPath)) 
    { 
        //压缩失败(未发现压缩后的文件) 
        return FALSE; 
    } 

    return TRUE; 
} 

BOOL CZipImplement::Zip_PackSbFiles(CString& pFilePath, CString& mZipFileFullPath, CStringA strPW)
{
	//参数错误 
	if ((pFilePath == L"") || (mZipFileFullPath == L""))
	{
		//路径异常返回 
		return FALSE;
	}

	if (!CZipImplement::FolderExist(pFilePath))
	{
		//要被压缩的文件夹不存在 
		return FALSE;
	}

	CString tZipFilePath = mZipFileFullPath.Left(mZipFileFullPath.ReverseFind('\\') + 1);
	if (!CZipImplement::FolderExist(tZipFilePath))
	{
		//ZIP文件存放的文件夹不存在创建它 
		CStringW strWZipFilePath(tZipFilePath);
		if (FALSE == CreatedMultipleDirectory((LPWSTR)(LPCWSTR)strWZipFilePath))
		{
			//创建目录失败 
			return FALSE;
		}
	}


	/************************************************************************/

	//创建ZIP文件
	if (strPW.IsEmpty())
	{
		hz = CreateZip(mZipFileFullPath, 0);
	}
	else
	{
		hz = CreateZip(mZipFileFullPath, (LPCSTR)strPW);
	}
	if (hz == 0)
	{
		//创建Zip文件失败 
		return FALSE;
	}

	string strPath = CT2A(pFilePath);
	vector<string> files;
	getFiles(strPath, files);
	for (size_t i = 0; i < files.size(); i++)
	{
		CString fileName(files[i].c_str());
		this->m_FolderPath = fileName.Left(fileName.ReverseFind(L'\\'));
		m_FolderName = fileName.Right(fileName.GetLength() - fileName.ReverseFind('\\') - 1);
		ZipAdd(hz, m_FolderName, fileName);
	}

	//关闭ZIP文件 
	CloseZip(hz);

	/************************************************************************/

	CFileFind tFFind;
	if (!tFFind.FindFile(mZipFileFullPath))
	{
		//压缩失败(未发现压缩后的文件) 
		return FALSE;
	}

	return TRUE;
}

BOOL CZipImplement::Zip_PackFile(CString& strPath, CString& mZipFileFullPath, CStringA strPW)
{
	//创建ZIP文件
	if (strPW.IsEmpty())
	{
		hz = CreateZip(mZipFileFullPath, 0);
	}
	else
	{
		hz = CreateZip(mZipFileFullPath, (LPCSTR)strPW);
	}
	if (hz == 0)
	{
		//创建Zip文件失败 
		return FALSE;
	}
	this->m_FolderPath = strPath.Left(strPath.ReverseFind(L'\\'));
	m_FolderName = strPath.Right(strPath.GetLength() - strPath.ReverseFind('\\') - 1);
	CString subPath;
	GetRelativePath(strPath, subPath);
	//将文件添加到ZIP文件 
	ZipAdd(hz, m_FolderName, strPath);
	CloseZip(hz);
	return TRUE;
}
///////////////////////////////////////////////////////////////////////////// 
// 函数说明: 解压缩文件夹 
// 参数说明: [in]：
//					mUnPackPath			解压后文件存放的路径 
//					mZipFileFullPath	ZIP文件的路径 
//					strPW				解压密码，可空，默认为空，表示压缩包没有密码
///////////////////////////////////////////////////////////////////////////// 
BOOL CZipImplement::Zip_UnPackFiles(CString &mZipFileFullPath, CString& mUnPackPath, CStringA strPW) 
{ 
    //参数错误 
    if ((mUnPackPath.IsEmpty()) || (mZipFileFullPath.IsEmpty())) 
    { 
        //路径异常返回 
        return FALSE ; 
    } 

    CFileFind tFFind; 
    if (!tFFind.FindFile(mZipFileFullPath)) 
    { 
        //压缩失败(未发现压缩文件) 
        return FALSE; 
    } 

    //如果解压缩的路径不存在 试图创建它 
    CString tZipFilePath = mUnPackPath; 
    if(!CZipImplement::FolderExist(tZipFilePath)) 
    { 
        //解压后存放的文件夹不存在 创建它         
		CStringW strWZipFilePath(tZipFilePath);
        if (FALSE == CreatedMultipleDirectory((LPWSTR)(LPCWSTR)strWZipFilePath)) 
        { 
            //创建目录失败 
            return FALSE; 
        } 
    } 
    /************************************************************************/ 
    //打开ZIP文件
	if (strPW.IsEmpty())
	{
		hz=OpenZip(mZipFileFullPath,0);
	}
	else
	{
		hz=OpenZip(mZipFileFullPath,(LPCSTR)strPW);
	}     
    if(hz == 0) 
    { 
        //打开Zip文件失败 
        return FALSE; 
    } 

    zr=SetUnzipBaseDir(hz,mUnPackPath); 
    if(zr != ZR_OK) 
    { 
        //打开Zip文件失败 
        CloseZip(hz); 
        return FALSE;       
    } 

    zr=GetZipItem(hz,-1,&ze); 
    if(zr != ZR_OK) 
    { 
        //获取Zip文件内容失败 
        CloseZip(hz); 
        return FALSE;       
    } 

    int numitems=ze.index; 
    for (int i=0; i<numitems; i++) 
    { 
        zr=GetZipItem(hz,i,&ze); 
        zr=UnzipItem(hz,i,ze.name); 
        if(zr != ZR_OK) 
        { 
            //获取Zip文件内容失败 
         //   CloseZip(hz); 
           // return FALSE;      
			continue;
        } 
    } 

    CloseZip(hz); 
    return TRUE; 
} 

//////////////////////////////////////////////////////////////////////////// 
// 函数说明: 检查指定的文件夹是否存在 
// 参数说明: [in]：strPath 检查的文件夹 (此方法会主动向路径末尾添加*.*) 
// 返回值:BOOL类型,存在返回TRUE,否则为FALSE 
///////////////////////////////////////////////////////////////////////////// 
BOOL CZipImplement::FolderExist(CString& strPath) 
{ 
    CString sCheckPath = strPath; 

    if(sCheckPath.Right(1) != L"\\") 
        sCheckPath += L"\\"; 

    sCheckPath += L"*.*"; 

    WIN32_FIND_DATA wfd; 
    BOOL rValue = FALSE; 

    HANDLE hFind = FindFirstFile(sCheckPath, &wfd); 

    if ((hFind!=INVALID_HANDLE_VALUE) && 
        (wfd.dwFileAttributes&FILE_ATTRIBUTE_DIRECTORY) || (wfd.dwFileAttributes&FILE_ATTRIBUTE_ARCHIVE)) 
    { 
        //如果存在并类型是文件夹 
        rValue = TRUE; 
    } 

    FindClose(hFind); 
    return rValue; 
} 

///////////////////////////////////////////////////////////////////////////// 
// 函数说明: 遍历文件夹 
// 参数说明: [in]：strFile 遍历的文件夹(此方法会主动向路径末尾添加*.*) 
// 返回值:BOOL类型,存在返回TRUE,否则为FALSE 
///////////////////////////////////////////////////////////////////////////// 
void CZipImplement::BrowseFile(CString &strFile) 
{ 
    CFileFind ff; 
    CString szDir = strFile; 

    if(szDir.Right(1) != L"\\") 
        szDir += L"\\"; 

    szDir += L"*.*"; 

    BOOL res = ff.FindFile(szDir); 
    while(res) 
    { 
        res = ff.FindNextFile(); 
        if(ff.IsDirectory() && !ff.IsDots()) 
        { 
            //如果是一个子目录，用递归继续往深一层找 
            CString strPath = ff.GetFilePath(); 

            CString subPath; 
            GetRelativePath(strPath,subPath); 
            //将文件添加到ZIP文件 
            ZipAddFolder(hz,subPath); 
            BrowseFile(strPath); 
        } 
        else if(!ff.IsDirectory() && !ff.IsDots()) 
        { 
            //显示当前访问的文件(完整路径) 
            CString strPath = ff.GetFilePath(); 
            CString subPath; 

            GetRelativePath(strPath,subPath); 
			CCharacterHelper m_characterhelper;
			
            //将文件添加到ZIP文件 
            ZipAdd(hz,subPath,strPath); 
        } 
    } 

    //关闭 
    ff.Close(); 
} 

///////////////////////////////////////////////////////////////////////////// 
// 函数说明: 获取相对路径 
// 参数说明: [in]：pFullPath 当前文件的完整路径 [out] : 解析后的相对路径 
///////////////////////////////////////////////////////////////////////////// 
void CZipImplement::GetRelativePath(CString& pFullPath,CString& pSubString) 
{ 
    pSubString = pFullPath.Right(pFullPath.GetLength() - this->m_FolderPath.GetLength() + this->m_FolderName.GetLength()); 
} 

///////////////////////////////////////////////////////////////////////////// 
// 函数说明: 创建多级目录 
// 参数说明: [in]： 路径字符串 
// 返回值: BOOL 成功True 失败False 
///////////////////////////////////////////////////////////////////////////// 
BOOL CZipImplement::CreatedMultipleDirectory(wchar_t* direct) 
{ 
    std::wstring Directoryname = direct; 

    if (Directoryname[Directoryname.length() - 1] !=  '\\') 
    { 
        Directoryname.append(1, '\\'); 
    } 
    std::vector< std::wstring> vpath; 
    std::wstring strtemp; 
    BOOL  bSuccess = FALSE; 
    for (int i = 0; i < Directoryname.length(); i++) 
    { 
        if ( Directoryname[i] != '\\') 
        { 
            strtemp.append(1,Directoryname[i]);    
        } 
        else 
        { 
            vpath.push_back(strtemp); 
            strtemp.append(1, '\\'); 
        } 
    } 
    std::vector<std::wstring>:: const_iterator vIter; 
    for (vIter = vpath.begin();vIter != vpath.end(); vIter++) 
	{
		USES_CONVERSION;
		bSuccess = CreateDirectoryA( W2A(vIter->c_str()), NULL ) ? true :false;
	
	}

    return bSuccess; 
}
