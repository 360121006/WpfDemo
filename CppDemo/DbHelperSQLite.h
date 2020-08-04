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
	* 函数介绍： 执行SQL语句，返回影响的记录数
	* 输入参数：SQL语句
	* 输出参数：无
	* 返回值 ：影响的记录数
	*/
	int ExecuteSql(CString szSql);

	/*
	* 函数介绍： 执行多条SQL语句，实现数据库事务。
	* 输入参数：SQL语句
	* 输出参数：无
	* 返回值 ：无
	*/
	void ExecuteSqlTran(CStringArray& szArrSql);
	/*
	* 函数介绍： 执行查询语句，返回CppSQLite3Query
	* 输入参数：SQL语句
	* 输出参数：无
	* 返回值 ：CppSQLite3Query
	*/
	CppSQLite3Query execQuery(CString szSql);
	/*
	* 函数介绍： 执行查询语句，返回CppSQLite3Table
	* 输入参数：SQL语句
	* 输出参数：无
	* 返回值 ：CppSQLite3Table
	*/
	CppSQLite3Table execTable(CString szSql);

	CString ConvertUTF8ToANSI(const char* cUTF8);
	~DbHelperSQLite();
private:
	char cDbFile[255];// 数据库全局路径
	char cDbPassWord[16];//数据库密码

	int UniToUTF8(LPCTSTR pUniString, char *szUtf8);
};

