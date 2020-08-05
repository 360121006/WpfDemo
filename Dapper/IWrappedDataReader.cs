using System;
using System.Data;

namespace Dapper
{
	// Token: 0x02000011 RID: 17
	public interface IWrappedDataReader : IDataReader, IDisposable, IDataRecord
	{
		// Token: 0x1700001B RID: 27
		// (get) Token: 0x060000E8 RID: 232
		IDataReader Reader { get; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x060000E9 RID: 233
		IDbCommand Command { get; }
	}
}
