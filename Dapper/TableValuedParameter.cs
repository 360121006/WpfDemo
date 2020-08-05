using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace Dapper
{
	// Token: 0x0200000F RID: 15
	internal sealed class TableValuedParameter : SqlMapper.ICustomQueryParameter
	{
		// Token: 0x060000DC RID: 220 RVA: 0x00008919 File Offset: 0x00006B19
		public TableValuedParameter(DataTable table) : this(table, null)
		{
		}

		// Token: 0x060000DD RID: 221 RVA: 0x00008923 File Offset: 0x00006B23
		public TableValuedParameter(DataTable table, string typeName)
		{
			this.table = table;
			this.typeName = typeName;
		}

		// Token: 0x060000DE RID: 222 RVA: 0x0000893C File Offset: 0x00006B3C
		static TableValuedParameter()
		{
			PropertyInfo property = typeof(SqlParameter).GetProperty("TypeName", BindingFlags.Instance | BindingFlags.Public);
			if (property != null && property.PropertyType == typeof(string) && property.CanWrite)
			{
				TableValuedParameter.setTypeName = (Action<SqlParameter, string>)Delegate.CreateDelegate(typeof(Action<SqlParameter, string>), property.GetSetMethod());
			}
		}

		// Token: 0x060000DF RID: 223 RVA: 0x000089A8 File Offset: 0x00006BA8
		void SqlMapper.ICustomQueryParameter.AddParameter(IDbCommand command, string name)
		{
			IDbDataParameter dbDataParameter = command.CreateParameter();
			dbDataParameter.ParameterName = name;
			TableValuedParameter.Set(dbDataParameter, this.table, this.typeName);
			command.Parameters.Add(dbDataParameter);
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x000089E4 File Offset: 0x00006BE4
		internal static void Set(IDbDataParameter parameter, DataTable table, string typeName)
		{
			parameter.Value = SqlMapper.SanitizeParameterValue(table);
			if (string.IsNullOrEmpty(typeName) && table != null)
			{
				typeName = table.GetTypeName();
			}
			if (!string.IsNullOrEmpty(typeName))
			{
				SqlParameter sqlParameter = parameter as SqlParameter;
				if (sqlParameter != null)
				{
					Action<SqlParameter, string> action = TableValuedParameter.setTypeName;
					if (action != null)
					{
						action(sqlParameter, typeName);
					}
					sqlParameter.SqlDbType = SqlDbType.Structured;
				}
			}
		}

		// Token: 0x04000044 RID: 68
		private readonly DataTable table;

		// Token: 0x04000045 RID: 69
		private readonly string typeName;

		// Token: 0x04000046 RID: 70
		private static readonly Action<SqlParameter, string> setTypeName;
	}
}
