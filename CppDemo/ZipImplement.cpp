///////////////////////////////////////////////////////////////////////////// 
// �ļ���: <ZipImplement.cpp> 
// ˵��:ѹ����ѹ���ļ��� 
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
	//�ļ����  
	long   hFile = 0;
	//�ļ���Ϣ  
	struct _finddata_t fileinfo;
	string p;
	if ((hFile = _findfirst(p.assign(path).append("\\*").c_str(), &fileinfo)) != -1)
	{
		do
		{
			//�����Ŀ¼,����֮  
			//�������,�����б�  
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
// ����˵��: ʵ��ѹ���ļ��в��� 
// ����˵��: [in]�� 
//					pFilePath			Ҫ��ѹ�����ļ��� 
//					mZipFileFullPath	ѹ������ļ�����·�� 
//					strPW				ѹ�����룬�ɿգ�Ĭ��Ϊ�գ���ʾ��������ѹ��
// ����ֵ: �������������·���FALSE,ѹ���ɹ��󷵻�TRUE 
///////////////////////////////////////////////////////////////////////////// 
BOOL CZipImplement::Zip_PackFiles(CString& pFilePath, CString& mZipFileFullPath, CStringA strPW) 
{ 
    //�������� 
    if ((pFilePath == L"") || (mZipFileFullPath == L"")) 
    { 
        //·���쳣���� 
        return FALSE ; 
    } 

    if(!CZipImplement::FolderExist(pFilePath)) 
    { 
        //Ҫ��ѹ�����ļ��в����� 
        return FALSE ; 
    } 

    CString tZipFilePath = mZipFileFullPath.Left(mZipFileFullPath.ReverseFind('\\') + 1); 
    if(!CZipImplement::FolderExist(tZipFilePath)) 
   { 
        //ZIP�ļ���ŵ��ļ��в����ڴ����� 
		CStringW strWZipFilePath(tZipFilePath);
		if (FALSE == CreatedMultipleDirectory((LPWSTR)(LPCWSTR)strWZipFilePath)) 
		{ 
			//����Ŀ¼ʧ�� 
			return FALSE; 
		} 
    } 

    //����ļ��е����� 
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

    //����ZIP�ļ�
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
        //����Zip�ļ�ʧ�� 
        return FALSE; 
    } 

    //�ݹ��ļ���,����ȡ���ʼۼ���ZIP�ļ� 
    BrowseFile(pFilePath); 
    //�ر�ZIP�ļ� 
    CloseZip(hz); 

    /************************************************************************/ 

    CFileFind tFFind; 
    if (!tFFind.FindFile(mZipFileFullPath)) 
    { 
        //ѹ��ʧ��(δ����ѹ������ļ�) 
        return FALSE; 
    } 

    return TRUE; 
} 

BOOL CZipImplement::Zip_PackSbFiles(CString& pFilePath, CString& mZipFileFullPath, CStringA strPW)
{
	//�������� 
	if ((pFilePath == L"") || (mZipFileFullPath == L""))
	{
		//·���쳣���� 
		return FALSE;
	}

	if (!CZipImplement::FolderExist(pFilePath))
	{
		//Ҫ��ѹ�����ļ��в����� 
		return FALSE;
	}

	CString tZipFilePath = mZipFileFullPath.Left(mZipFileFullPath.ReverseFind('\\') + 1);
	if (!CZipImplement::FolderExist(tZipFilePath))
	{
		//ZIP�ļ���ŵ��ļ��в����ڴ����� 
		CStringW strWZipFilePath(tZipFilePath);
		if (FALSE == CreatedMultipleDirectory((LPWSTR)(LPCWSTR)strWZipFilePath))
		{
			//����Ŀ¼ʧ�� 
			return FALSE;
		}
	}


	/************************************************************************/

	//����ZIP�ļ�
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
		//����Zip�ļ�ʧ�� 
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

	//�ر�ZIP�ļ� 
	CloseZip(hz);

	/************************************************************************/

	CFileFind tFFind;
	if (!tFFind.FindFile(mZipFileFullPath))
	{
		//ѹ��ʧ��(δ����ѹ������ļ�) 
		return FALSE;
	}

	return TRUE;
}

BOOL CZipImplement::Zip_PackFile(CString& strPath, CString& mZipFileFullPath, CStringA strPW)
{
	//����ZIP�ļ�
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
		//����Zip�ļ�ʧ�� 
		return FALSE;
	}
	this->m_FolderPath = strPath.Left(strPath.ReverseFind(L'\\'));
	m_FolderName = strPath.Right(strPath.GetLength() - strPath.ReverseFind('\\') - 1);
	CString subPath;
	GetRelativePath(strPath, subPath);
	//���ļ���ӵ�ZIP�ļ� 
	ZipAdd(hz, m_FolderName, strPath);
	CloseZip(hz);
	return TRUE;
}
///////////////////////////////////////////////////////////////////////////// 
// ����˵��: ��ѹ���ļ��� 
// ����˵��: [in]��
//					mUnPackPath			��ѹ���ļ���ŵ�·�� 
//					mZipFileFullPath	ZIP�ļ���·�� 
//					strPW				��ѹ���룬�ɿգ�Ĭ��Ϊ�գ���ʾѹ����û������
///////////////////////////////////////////////////////////////////////////// 
BOOL CZipImplement::Zip_UnPackFiles(CString &mZipFileFullPath, CString& mUnPackPath, CStringA strPW) 
{ 
    //�������� 
    if ((mUnPackPath.IsEmpty()) || (mZipFileFullPath.IsEmpty())) 
    { 
        //·���쳣���� 
        return FALSE ; 
    } 

    CFileFind tFFind; 
    if (!tFFind.FindFile(mZipFileFullPath)) 
    { 
        //ѹ��ʧ��(δ����ѹ���ļ�) 
        return FALSE; 
    } 

    //�����ѹ����·�������� ��ͼ������ 
    CString tZipFilePath = mUnPackPath; 
    if(!CZipImplement::FolderExist(tZipFilePath)) 
    { 
        //��ѹ���ŵ��ļ��в����� ������         
		CStringW strWZipFilePath(tZipFilePath);
        if (FALSE == CreatedMultipleDirectory((LPWSTR)(LPCWSTR)strWZipFilePath)) 
        { 
            //����Ŀ¼ʧ�� 
            return FALSE; 
        } 
    } 
    /************************************************************************/ 
    //��ZIP�ļ�
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
        //��Zip�ļ�ʧ�� 
        return FALSE; 
    } 

    zr=SetUnzipBaseDir(hz,mUnPackPath); 
    if(zr != ZR_OK) 
    { 
        //��Zip�ļ�ʧ�� 
        CloseZip(hz); 
        return FALSE;       
    } 

    zr=GetZipItem(hz,-1,&ze); 
    if(zr != ZR_OK) 
    { 
        //��ȡZip�ļ�����ʧ�� 
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
            //��ȡZip�ļ�����ʧ�� 
         //   CloseZip(hz); 
           // return FALSE;      
			continue;
        } 
    } 

    CloseZip(hz); 
    return TRUE; 
} 

//////////////////////////////////////////////////////////////////////////// 
// ����˵��: ���ָ�����ļ����Ƿ���� 
// ����˵��: [in]��strPath �����ļ��� (�˷�����������·��ĩβ���*.*) 
// ����ֵ:BOOL����,���ڷ���TRUE,����ΪFALSE 
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
        //������ڲ��������ļ��� 
        rValue = TRUE; 
    } 

    FindClose(hFind); 
    return rValue; 
} 

///////////////////////////////////////////////////////////////////////////// 
// ����˵��: �����ļ��� 
// ����˵��: [in]��strFile �������ļ���(�˷�����������·��ĩβ���*.*) 
// ����ֵ:BOOL����,���ڷ���TRUE,����ΪFALSE 
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
            //�����һ����Ŀ¼���õݹ��������һ���� 
            CString strPath = ff.GetFilePath(); 

            CString subPath; 
            GetRelativePath(strPath,subPath); 
            //���ļ���ӵ�ZIP�ļ� 
            ZipAddFolder(hz,subPath); 
            BrowseFile(strPath); 
        } 
        else if(!ff.IsDirectory() && !ff.IsDots()) 
        { 
            //��ʾ��ǰ���ʵ��ļ�(����·��) 
            CString strPath = ff.GetFilePath(); 
            CString subPath; 

            GetRelativePath(strPath,subPath); 
			CCharacterHelper m_characterhelper;
			
            //���ļ���ӵ�ZIP�ļ� 
            ZipAdd(hz,subPath,strPath); 
        } 
    } 

    //�ر� 
    ff.Close(); 
} 

///////////////////////////////////////////////////////////////////////////// 
// ����˵��: ��ȡ���·�� 
// ����˵��: [in]��pFullPath ��ǰ�ļ�������·�� [out] : ����������·�� 
///////////////////////////////////////////////////////////////////////////// 
void CZipImplement::GetRelativePath(CString& pFullPath,CString& pSubString) 
{ 
    pSubString = pFullPath.Right(pFullPath.GetLength() - this->m_FolderPath.GetLength() + this->m_FolderName.GetLength()); 
} 

///////////////////////////////////////////////////////////////////////////// 
// ����˵��: �����༶Ŀ¼ 
// ����˵��: [in]�� ·���ַ��� 
// ����ֵ: BOOL �ɹ�True ʧ��False 
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
