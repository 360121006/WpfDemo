// CLogHelper.cpp: implementation of the CLogFile class.
//
//////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "CLogHelper.h"
#ifdef _DEBUG
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#define new DEBUG_NEW
#endif

//////////////////////////////////////////////////////////////////////
// Construction/Destruction
//////////////////////////////////////////////////////////////////////
//extern BOOL bIsDebug;
// 判断文件是否存在
BOOL IsFileExist(const CString& FileName)
{
	DWORD dwAttrib = GetFileAttributes(FileName);
	return INVALID_FILE_ATTRIBUTES != dwAttrib && 0 == (dwAttrib & FILE_ATTRIBUTE_DIRECTORY);
}
// 判断文件夹是否存在
BOOL IsDirectoryExist(const CString & FileName)
{
	DWORD dwAttrib = GetFileAttributes(FileName);
	return INVALID_FILE_ATTRIBUTES != dwAttrib && 0 != (dwAttrib & FILE_ATTRIBUTE_DIRECTORY);
}

//////////////////////////////////////////////////////////////////////////
CLogHelper::CLogHelper()
{
	TCHAR chDirectory[MAX_PATH + 1] = { 0 };
	//::GetCurrentDirectory(MAX_PATH, chDirectory);
	HMODULE hModule = ::GetModuleHandle(NULL);
	//TCHAR path[MAX_PATH];
	::GetModuleFileName(hModule, chDirectory, MAX_PATH);
	CString strPath(chDirectory);
	_CurrentDirectory = strPath.Left(strPath.ReverseFind(_T('\\'))) + _T("\\Log\\");
	//_CurrentDirectory = CString(chDirectory) + CString("\\Log\\");

	m_pStdioFile = NULL;
	if (!IsDirectoryExist(_CurrentDirectory))
	{
		CreateDirectory(_CurrentDirectory, NULL);
	}
	DeleteFrontWeekLogFile(_CurrentDirectory);
}

//////////////////////////////////////////////////////////////////////////
CLogHelper::~CLogHelper()
{
	if (m_pStdioFile != NULL)
	{
		_CriSect.Lock();
		m_pStdioFile->Flush();
		m_pStdioFile->Close();
		delete m_pStdioFile;
		m_pStdioFile = NULL;
		_CriSect.Unlock();
	}
}

//////////////////////////////////////////////////////////////////////////
//记录时间
void CLogHelper::WriteLog(CString strLogTilte,CString strLogMessage,CString logLevel)
{
	if (logLevel == L"debug")
	{
		//if (!bIsDebug)
			return;
	}
	CString strNowDate = CTime::GetCurrentTime().Format("%Y%m%d");
	CString strNowTime;// = CTime::GetCurrentTime().Format("[%Y-%m-%d %H:%M:%S ] ");
	SYSTEMTIME st;
	GetLocalTime(&st);
	strNowTime.Format(L"%04d-%02d-%02d %02d:%02d:%02d:%03d",st.wYear,st.wMonth,st.wDay,st.wHour,st.wMinute,st.wSecond,st.wMilliseconds);
	CString strLogM;
	strLogM.Format(L"%s 【%s】-----【%s】--------------- 【%s】\n", strNowTime, logLevel, strLogTilte, strLogMessage);
	_CriSect.Lock();
	TRY
	{
		if (m_pStdioFile == NULL)
		{
			m_pStdioFile = new CStdioFile();
			CString strFileName = _CurrentDirectory + strNowDate + CString(".log");
			if (m_pStdioFile->Open(strFileName,CFile::modeCreate | CFile::modeNoTruncate | CFile::modeReadWrite | CFile::shareDenyNone))
			{
				if (m_pStdioFile->m_pStream != NULL)
				{
					m_pStdioFile->SeekToEnd();
					setlocale(LC_CTYPE, "chs");
					//m_pStdioFile->WriteString(L"+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n");
					m_pStdioFile->WriteString(strLogM);
					m_pStdioFile->Flush();
				}
			}
			m_strNowDate = strNowDate;
		}
		else
		{
			if (m_pStdioFile->m_pStream == NULL)
			{
				delete m_pStdioFile;
				m_pStdioFile = NULL;
			}
			else
			{
				if (m_strNowDate.CompareNoCase(strNowDate) != 0)
				{
					m_pStdioFile->Flush();
					m_pStdioFile->Close();
					delete m_pStdioFile;
					m_pStdioFile = NULL;

					DeleteFrontWeekLogFile(_CurrentDirectory);

					m_pStdioFile = new CStdioFile();
					CString strFileName = _CurrentDirectory + strNowDate + CString(".log");
					if (m_pStdioFile->Open(strFileName,CFile::modeCreate | CFile::modeNoTruncate | CFile::modeReadWrite | CFile::shareDenyNone))
					{
						if (m_pStdioFile->m_pStream != NULL)
						{
							m_pStdioFile->SeekToEnd();
							strLogM.Format(L"%s 【%s】-----【%s】--------------- 【%s】\n", strNowTime, logLevel, strLogTilte, strLogMessage);
							m_pStdioFile->WriteString(strLogM);
							m_pStdioFile->Flush();
						}
					}
					m_strNowDate = strNowDate;
				}
				else
				{
					if (m_pStdioFile->m_pStream != NULL)
					{
						strLogM.Format(L"%s 【%s】-----【%s】--------------- 【%s】\n", strNowTime, logLevel, strLogTilte, strLogMessage);
						m_pStdioFile->WriteString(strLogM);
						m_pStdioFile->Flush();
					}
					m_strNowDate = strNowDate;
				}
			}
		}
	}
		CATCH(CFileException, e)
	{
		//
	}
	AND_CATCH(CException, E)
	{
		//
	}
	END_CATCH

		_CriSect.Unlock();
}
//////////////////////////////////////////////////////////////////////////
//记录行为
//void CLogHelper::WriteXwLog(CString userid, CString param)
//{
//	TCHAR chDirectory[MAX_PATH + 1] = { 0 };
//	//::GetCurrentDirectory(MAX_PATH, chDirectory);
//	HMODULE hModule = ::GetModuleHandle(NULL);
//	//TCHAR path[MAX_PATH];
//	::GetModuleFileName(hModule, chDirectory, MAX_PATH);
//	CString strPath(chDirectory);
//	_CurrentDirectory = strPath.Left(strPath.ReverseFind(_T('\\')));
//	_CurrentDirectory = _CurrentDirectory.Left(_CurrentDirectory.ReverseFind(_T('\\')));
//	_CurrentDirectory += _T("\\Log\\");
//
//	CString strNowDate = L"xw";
//	CString strNowTime;// = CTime::GetCurrentTime().Format("[%Y-%m-%d %H:%M:%S ] ");
//	SYSTEMTIME st;
//	GetLocalTime(&st);
//	strNowTime.Format(L"%04d-%02d-%02d %02d:%02d:%02d", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond);
//	CString strLogM;
//	strLogM.Format(L"{\"userid\":\"%s\",\"param\":\"%s\",\"time\":\"%s\"},", userid, param,strNowTime);
//	_CriSect.Lock();
//	TRY
//	{
//		if (m_pStdioFile == NULL)
//		{
//			m_pStdioFile = new CStdioFile();
//			CString strFileName = _CurrentDirectory + strNowDate + CString(".log");
//			if (m_pStdioFile->Open(strFileName,CFile::modeCreate | CFile::modeNoTruncate | CFile::modeReadWrite | CFile::shareDenyNone))
//			{
//				if (m_pStdioFile->m_pStream != NULL)
//				{
//					m_pStdioFile->SeekToEnd();
//					setlocale(LC_CTYPE, "chs");
//					//m_pStdioFile->WriteString(L"+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n");
//					m_pStdioFile->WriteString(strLogM);
//					m_pStdioFile->Flush();
//				}
//			}
//			m_strNowDate = strNowDate;
//		}
//		else
//		{
//			if (m_pStdioFile->m_pStream == NULL)
//			{
//				delete m_pStdioFile;
//				m_pStdioFile = NULL;
//			}
//			else
//			{
//				if (m_strNowDate.CompareNoCase(strNowDate) != 0)
//				{
//					m_pStdioFile->Flush();
//					m_pStdioFile->Close();
//					delete m_pStdioFile;
//					m_pStdioFile = NULL;
//
//					DeleteFrontWeekLogFile(_CurrentDirectory);
//
//					m_pStdioFile = new CStdioFile();
//					CString strFileName = _CurrentDirectory + strNowDate + CString(".log");
//					if (m_pStdioFile->Open(strFileName,CFile::modeCreate | CFile::modeNoTruncate | CFile::modeReadWrite | CFile::shareDenyNone))
//					{
//						if (m_pStdioFile->m_pStream != NULL)
//						{
//							m_pStdioFile->SeekToEnd();
//							strLogM.Format(L"{\"userid\":\"%s\",\"param\":\"%s\",\"time\":\"%s\"},", userid, param, strNowTime);
//							m_pStdioFile->WriteString(strLogM);
//							m_pStdioFile->Flush();
//						}
//					}
//					m_strNowDate = strNowDate;
//				}
//				else
//				{
//					if (m_pStdioFile->m_pStream != NULL)
//					{
//						strLogM.Format(L"{\"userid\":\"%s\",\"param\":\"%s\",\"time\":\"%s\"},", userid, param, strNowTime);
//						m_pStdioFile->WriteString(strLogM);
//						m_pStdioFile->Flush();
//					}
//					m_strNowDate = strNowDate;
//				}
//			}
//		}
//	}
//		CATCH(CFileException, e)
//	{
//		//
//	}
//	AND_CATCH(CException, E)
//	{
//		//
//	}
//	END_CATCH
//
//		_CriSect.Unlock();
//}

//记录行为
void CLogHelper::WriteXwLog(CString userid, CString nsrsbh, CString param)
{
	if (userid.GetLength() == 0)
	{
		return;
	}
		TCHAR chDirectory[MAX_PATH + 1] = { 0 };
		//::GetCurrentDirectory(MAX_PATH, chDirectory);
		HMODULE hModule = ::GetModuleHandle(NULL);
		//TCHAR path[MAX_PATH];
		::GetModuleFileName(hModule, chDirectory, MAX_PATH);
		CString strPath(chDirectory);
		_CurrentDirectory = strPath.Left(strPath.ReverseFind(_T('\\')));
		_CurrentDirectory = _CurrentDirectory.Left(_CurrentDirectory.ReverseFind(_T('\\')));
		_CurrentDirectory += _T("\\Log\\");

	CString strNowDate = L"xw";
	CString strNowTime;// = CTime::GetCurrentTime().Format("[%Y-%m-%d %H:%M:%S ] ");
	SYSTEMTIME st;
	GetLocalTime(&st);
	strNowTime.Format(L"%04d-%02d-%02d %02d:%02d:%02d", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond);
	CString strLogM;
	strLogM.Format(L"{\"userid\":\"%s\",\"nsrsbh\":\"%s\",\"param\":\"%s\",\"time\":\"%s\"},", userid, nsrsbh, param, strNowTime);
	_CriSect.Lock();
	TRY
	{
		if (m_pStdioFile == NULL)
		{
			m_pStdioFile = new CStdioFile();
			CString strFileName = _CurrentDirectory + strNowDate + CString(".log");
			if (m_pStdioFile->Open(strFileName,CFile::modeCreate | CFile::modeNoTruncate | CFile::modeReadWrite | CFile::shareDenyNone))
			{
				if (m_pStdioFile->m_pStream != NULL)
				{
					m_pStdioFile->SeekToEnd();
					setlocale(LC_CTYPE, "chs");
					//m_pStdioFile->WriteString(L"+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n");
					m_pStdioFile->WriteString(strLogM);
					m_pStdioFile->Flush();
				}
			}
			m_strNowDate = strNowDate;
		}
		else
		{
			if (m_pStdioFile->m_pStream == NULL)
			{
				delete m_pStdioFile;
				m_pStdioFile = NULL;
			}
			else
			{
				if (m_strNowDate.CompareNoCase(strNowDate) != 0)
				{
					m_pStdioFile->Flush();
					m_pStdioFile->Close();
					delete m_pStdioFile;
					m_pStdioFile = NULL;

					DeleteFrontWeekLogFile(_CurrentDirectory);

					m_pStdioFile = new CStdioFile();
					CString strFileName = _CurrentDirectory + strNowDate + CString(".log");
					if (m_pStdioFile->Open(strFileName,CFile::modeCreate | CFile::modeNoTruncate | CFile::modeReadWrite | CFile::shareDenyNone))
					{
						if (m_pStdioFile->m_pStream != NULL)
						{
							m_pStdioFile->SeekToEnd();
							strLogM.Format(L"{\"userid\":\"%s\",\"nsrsbh\":\"%s\",\"param\":\"%s\",\"time\":\"%s\"},", userid, nsrsbh, param, strNowTime);
							m_pStdioFile->WriteString(strLogM);
							m_pStdioFile->Flush();
						}
					}
					m_strNowDate = strNowDate;
				}
				else
				{
					if (m_pStdioFile->m_pStream != NULL)
					{
						strLogM.Format(L"{\"userid\":\"%s\",\"nsrsbh\":\"%s\",\"param\":\"%s\",\"time\":\"%s\"},", userid, nsrsbh, param, strNowTime);
						m_pStdioFile->WriteString(strLogM);
						m_pStdioFile->Flush();
					}
					m_strNowDate = strNowDate;
				}
			}
		}
	}
		CATCH(CFileException, e)
	{
		//
	}
	AND_CATCH(CException, E)
	{
		//
	}
	END_CATCH

		_CriSect.Unlock();
}

//////////////////////////////////////////////////////////////////////////
//删除前周文件
void CLogHelper::DeleteFrontWeekLogFile(CString strDirectory)
{
	HANDLE hFind = NULL;
	WIN32_FIND_DATA FindFileData;
	CString DirPath = strDirectory + CString("*.log");

	CTimeSpan Span(7, 0, 0, 0);
	CTime FrontWeekDate = CTime::GetCurrentTime() - Span;
	CString strFrontWeekFileName = FrontWeekDate.Format("%Y%m%d%H.log");

	CString strFileName;
	hFind = ::FindFirstFile(DirPath, &FindFileData);
	if (hFind == INVALID_HANDLE_VALUE)
	{
		return;
	}
	strFileName = CString(FindFileData.cFileName);
	strFileName.MakeLower();
	if (strFileName < strFrontWeekFileName)
	{
		::DeleteFile(strDirectory + FindFileData.cFileName);
	}

	BOOL bRunFlag = TRUE;
	while (bRunFlag)
	{
		BOOL bFind = ::FindNextFile(hFind, &FindFileData);
		if (!bFind)
		{
			break;
		}
		strFileName = CString(FindFileData.cFileName);
		strFileName.MakeLower();
		if (strFileName < strFrontWeekFileName)
		{
			::DeleteFile(strDirectory + FindFileData.cFileName);
		}
	}
	::FindClose(hFind);
}

//////////////////////////////////////////////////////////////////////////

