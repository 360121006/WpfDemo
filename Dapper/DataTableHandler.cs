using System;
using System.Data;

namespace Dapper
{
	// Token: 0x02000005 RID: 5
	internal sealed class DataTableHandler : SqlMapper.ITypeHandler
	{
		// Token: 0x06000016 RID: 22 RVA: 0x000023F6 File Offset: 0x000005F6
		public object Parse(Type destinationType, object value)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000017 RID: 23 RVA: 0x000023FD File Offset: 0x000005FD
		public void SetValue(IDbDataParameter parameter, object value)
		{
			TableValuedParameter.Set(parameter, value as DataTable, null);
		}
	}
}
