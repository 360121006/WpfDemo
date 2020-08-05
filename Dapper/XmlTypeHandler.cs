using System;
using System.Data;

namespace Dapper
{
	// Token: 0x02000013 RID: 19
	internal abstract class XmlTypeHandler<T> : SqlMapper.StringTypeHandler<T>
	{
		// Token: 0x0600010E RID: 270 RVA: 0x00008CE7 File Offset: 0x00006EE7
		public override void SetValue(IDbDataParameter parameter, T value)
		{
			base.SetValue(parameter, value);
			parameter.DbType = DbType.Xml;
		}
	}
}
