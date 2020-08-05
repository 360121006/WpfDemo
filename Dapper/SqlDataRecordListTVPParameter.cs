using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using Microsoft.SqlServer.Server;

namespace Dapper
{
	// Token: 0x0200000D RID: 13
	internal sealed class SqlDataRecordListTVPParameter : SqlMapper.ICustomQueryParameter
	{
		// Token: 0x06000050 RID: 80 RVA: 0x0000399A File Offset: 0x00001B9A
		public SqlDataRecordListTVPParameter(IEnumerable<SqlDataRecord> data, string typeName)
		{
			this.data = data;
			this.typeName = typeName;
		}

		// Token: 0x06000051 RID: 81 RVA: 0x000039B0 File Offset: 0x00001BB0
		static SqlDataRecordListTVPParameter()
		{
			PropertyInfo property = typeof(SqlParameter).GetProperty("TypeName", BindingFlags.Instance | BindingFlags.Public);
			if (property != null && property.PropertyType == typeof(string) && property.CanWrite)
			{
				SqlDataRecordListTVPParameter.setTypeName = (Action<SqlParameter, string>)Delegate.CreateDelegate(typeof(Action<SqlParameter, string>), property.GetSetMethod());
			}
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00003A1C File Offset: 0x00001C1C
		void SqlMapper.ICustomQueryParameter.AddParameter(IDbCommand command, string name)
		{
			IDbDataParameter dbDataParameter = command.CreateParameter();
			dbDataParameter.ParameterName = name;
			SqlDataRecordListTVPParameter.Set(dbDataParameter, this.data, this.typeName);
			command.Parameters.Add(dbDataParameter);
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00003A58 File Offset: 0x00001C58
		internal static void Set(IDbDataParameter parameter, IEnumerable<SqlDataRecord> data, string typeName)
        {
            if (data != null)
            {
                parameter.Value = data;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
			SqlParameter sqlParameter = parameter as SqlParameter;
			if (sqlParameter != null)
			{
				sqlParameter.SqlDbType = SqlDbType.Structured;
				sqlParameter.TypeName = typeName;
			}
		}

		// Token: 0x04000026 RID: 38
		private readonly IEnumerable<SqlDataRecord> data;

		// Token: 0x04000027 RID: 39
		private readonly string typeName;

		// Token: 0x04000028 RID: 40
		private static readonly Action<SqlParameter, string> setTypeName;
	}
}
