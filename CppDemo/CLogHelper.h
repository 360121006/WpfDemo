// CLogHelper.h: interface for the CLogFile class.
//
//////////////////////////////////////////////////////////////////////

#if !defined(AFX_LOGFILE_H__7700A691_FFA2_4B5B_8C86_45D51A018FEF__INCLUDED_)
#define AFX_LOGFILE_H__7700A691_FFA2_4B5B_8C86_45D51A018FEF__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
//////////////////////////////////////////////////////////////////////////
#include <afxmt.h>
#include <locale.h>
//////////////////////////////////////////////////////////////////////////
class CLogHelper
{
public:
	CLogHelper();
	virtual ~CLogHelper();

	void WriteLog(CString strLogTilte, CString strLogMessage, CString logLevel = L"info");

protected:
	CString _CurrentDirectory; //µÿ÷∑
	CCriticalSection _CriSect;
	CStdioFile *m_pStdioFile;
	CString m_strNowDate;

	//void DeleteFrontWeekLogFile(CString strDirectory);
public:
	void WriteXwLog(CString userid, CString nsrsbh, CString param);
	//void WriteLog(CString strLogTilte, CString strLogMessage);
	void DeleteFrontWeekLogFile(CString strDirectory);
};

//////////////////////////////////////////////////////////////////////////
#endif // !defined(AFX_LOGFILE_H__7700A691_FFA2_4B5B_8C86_45D51A018FEF__INCLUDED_)

