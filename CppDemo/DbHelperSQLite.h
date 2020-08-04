#pragma once
#include"CppSQLite3.h"
class DbHelperSQLite
{
public:
	//DbHelperSQLite();
	//DbHelperSQLite(CString szDbFile, CString szDbPassWord);
	//DbHelperSQLite& DbHelperSQLite::operator=(const DbHelperSQLite& dbhelpersqlite);
	DbHelperSQLite();
	DbHelperSQLite(CString szDbFile, CString szDbPassWord);
	/*
	* �������ܣ� ִ��SQL��䣬����Ӱ��ļ�¼��
	* ���������SQL���
	* �����������
	* ����ֵ ��Ӱ��ļ�¼��
	*/
	int ExecuteSql(CString szSql);

	/*
	* �������ܣ� ִ�ж���SQL��䣬ʵ�����ݿ�����
	* ���������SQL���
	* �����������
	* ����ֵ ����
	*/
	void ExecuteSqlTran(CStringArray& szArrSql);
	/*
	* �������ܣ� ִ�в�ѯ��䣬����CppSQLite3Query
	* ���������SQL���
	* �����������
	* ����ֵ ��CppSQLite3Query
	*/
	CppSQLite3Query execQuery(CString szSql);
	/*
	* �������ܣ� ִ�в�ѯ��䣬����CppSQLite3Table
	* ���������SQL���
	* �����������
	* ����ֵ ��CppSQLite3Table
	*/
	CppSQLite3Table execTable(CString szSql);

	CString ConvertUTF8ToANSI(const char* cUTF8);
	~DbHelperSQLite();
private:
	char cDbFile[255];// ���ݿ�ȫ��·��
	char cDbPassWord[16];//���ݿ�����

	int UniToUTF8(LPCTSTR pUniString, char *szUtf8);
};

