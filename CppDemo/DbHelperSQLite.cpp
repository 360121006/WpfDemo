#include "stdafx.h"
#include "DbHelperSQLite.h"

//DbHelperSQLite();
//DbHelperSQLite(CString szDbFile, CString szDbPassWord);
//DbHelperSQLite& DbHelperSQLite::operator=(const DbHelperSQLite& dbhelpersqlite);

DbHelperSQLite::DbHelperSQLite()
{

}

DbHelperSQLite::DbHelperSQLite(CString szDbFile, CString szDbPassWord)
{
	UniToUTF8(szDbFile, this->cDbFile);
	WideCharToMultiByte(CP_ACP, 0, szDbPassWord, -1, this->cDbPassWord, MAX_PATH, NULL, NULL);
}

/*
* �������ܣ� ִ��SQL��䣬����Ӱ��ļ�¼��
* ���������SQL���
* �����������
* ����ֵ ��Ӱ��ļ�¼��
*/
int DbHelperSQLite::ExecuteSql(CString szSql)
{
	int nSqlLeng = szSql.GetLength();
	if (nSqlLeng == 0)return 0;
	CppSQLite3DB db;
	try
	{
		db.open(cDbFile, cDbPassWord);
		int nLen = WideCharToMultiByte(CP_UTF8, 0, szSql, -1, NULL, 0, NULL, NULL);
		char *pcSql = new char[nLen + 1];
		memset(pcSql, 0, nLen + 1);
		UniToUTF8(szSql, pcSql);
		int rows = db.execDML(pcSql);
		db.close();
		delete[] pcSql;
		pcSql = NULL;
		return rows;
	}
	catch (CppSQLite3Exception& e)
	{
		db.close();
		throw e;
	}
}


/*
* �������ܣ� ִ�ж���SQL��䣬ʵ�����ݿ�����
* ���������SQL���
* �����������
* ����ֵ ����
*/
void DbHelperSQLite::ExecuteSqlTran(CStringArray& szArrSql)
{
	if (szArrSql.GetCount() == 0)return;
	CppSQLite3DB db;
	try
	{
		db.open(cDbFile, cDbPassWord);
		db.execDML("begin transaction;");
		for (int i = 0; i < szArrSql.GetCount(); i++)
		{
			int nSqlLeng = szArrSql[i].GetLength();
			if (nSqlLeng == 0)continue;
			char *pcSql = new char[2 * nSqlLeng + 1];
			memset(pcSql, 0, 2 * nSqlLeng + 1);
			//WideCharToMultiByte(CP_ACP, 0, szArrSql[i], -1, pcSql, nSqlLeng, NULL, NULL);
			UniToUTF8(szArrSql[i], pcSql);
			int rows = db.execDML(pcSql);
			delete[] pcSql;
			pcSql = NULL;
		}
		db.execDML("commit transaction;");
		db.close();
	}
	catch (CppSQLite3Exception& e)
	{
		db.close();
		throw e;
	}
}

/*
* �������ܣ� ִ�в�ѯ��䣬����CppSQLite3Query
* ���������SQL���
* �����������
* ����ֵ ��CppSQLite3Query
*/
CppSQLite3Query DbHelperSQLite::execQuery(CString szSql)
{
	CppSQLite3DB db;
	try
	{
		db.open(cDbFile, cDbPassWord);
		char *pcSql = new char[2 * szSql.GetLength() + 1];
		memset(pcSql, 0, 2 * szSql.GetLength() + 1);
		//WideCharToMultiByte(CP_ACP, 0, szSql, -1, pcSql, szSql.GetLength(), NULL, NULL);
		UniToUTF8(szSql, pcSql);
		CppSQLite3Query query = db.execQuery(pcSql);
		db.close();
		delete[] pcSql;
		pcSql = NULL;
		return query;
	}
	catch (CppSQLite3Exception& e)
	{
		db.close();
		throw e;
	}
}

/*
* �������ܣ� ִ�в�ѯ��䣬����CppSQLite3Table
* ���������SQL���
* �����������
* ����ֵ ��CppSQLite3Table
*/
CppSQLite3Table DbHelperSQLite::execTable(CString szSql)
{
	CppSQLite3DB db;
	try
	{
		db.open(cDbFile, cDbPassWord);
		int nLen = WideCharToMultiByte(CP_UTF8, 0, szSql, -1, NULL, 0, NULL, NULL);
		char *pcSql = new char[nLen + 1];
		memset(pcSql, 0, nLen + 1);
		UniToUTF8(szSql, pcSql);
		CppSQLite3Table tb = db.getTable(pcSql);
		db.close();
		delete[] pcSql;
		pcSql = NULL;
		return tb;
	}
	catch (CppSQLite3Exception& e)
	{
		db.close();
		throw e;
	}
}

CString DbHelperSQLite::ConvertUTF8ToANSI(const char* cUTF8)
{
	int nLen = ::MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, cUTF8, -1, NULL, 0);
	//������Ҫ��unicode����     
	WCHAR * wszANSI = new WCHAR[nLen + 1];
	memset(wszANSI, 0, nLen * 2 + 2);
	nLen = MultiByteToWideChar(CP_UTF8, 0, cUTF8, -1, wszANSI, nLen);    //��utf8ת��unicode    
	nLen = WideCharToMultiByte(CP_ACP, 0, wszANSI, -1, NULL, 0, NULL, NULL);        //�õ�Ҫ��ansi����     
	char *cANSI = new char[nLen + 1];
	memset(cANSI, 0, nLen + 1);
	WideCharToMultiByte(CP_ACP, 0, wszANSI, -1, cANSI, nLen, NULL, NULL);          //��unicodeת��ansi     
	CString szANSI(cANSI);
	delete[] wszANSI;
	delete[] cANSI;
	wszANSI = NULL;
	cANSI = NULL;
	return szANSI;
}

DbHelperSQLite::~DbHelperSQLite()
{

}

int DbHelperSQLite::UniToUTF8(LPCTSTR pUniString, char *szUtf8)
{
	int nLen = WideCharToMultiByte(CP_UTF8, 0, pUniString, -1, NULL, 0, NULL, NULL);
	char *szUtf8Temp = new char[nLen + 1];
	memset(szUtf8Temp, 0, nLen + 1);
	WideCharToMultiByte(CP_UTF8, 0, pUniString, -1, szUtf8Temp, nLen, NULL, NULL);
	sprintf_s(szUtf8, nLen, "%s", szUtf8Temp);
	delete[] szUtf8Temp;
	return nLen;
}