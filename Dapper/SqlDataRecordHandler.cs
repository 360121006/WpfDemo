using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.SqlServer.Server;

namespace Dapper
{
	// Token: 0x0200000C RID: 12
	internal sealed class SqlDataRecordHandler : SqlMapper.ITypeHandler
	{
		// Token: 0x0600004D RID: 77 RVA: 0x000023BD File Offset: 0x000005BD
		public object Parse(Type destinationType, object value)
		{
			throw new NotSupportedException();
		}

		// Token: 0x0600004E RID: 78 RVA: 0x0000398B File Offset: 0x00001B8B
		public void SetValue(IDbDataParameter parameter, object value)
		{
			SqlDataRecordListTVPParameter.Set(parameter, value as IEnumerable<SqlDataRecord>, null);
		}
	}
}
