using System;
using System.Data;

namespace Dapper
{
	// Token: 0x0200000A RID: 10
	internal class FeatureSupport
	{
		// Token: 0x06000041 RID: 65 RVA: 0x00003830 File Offset: 0x00001A30
		public static FeatureSupport Get(IDbConnection connection)
		{
			if (string.Equals((connection != null) ? connection.GetType().Name : null, "npgsqlconnection", StringComparison.OrdinalIgnoreCase))
			{
				return FeatureSupport.Postgres;
			}
			return FeatureSupport.Default;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x0000385B File Offset: 0x00001A5B
		private FeatureSupport(bool arrays)
		{
			this.Arrays = arrays;
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000043 RID: 67 RVA: 0x0000386A File Offset: 0x00001A6A
		public bool Arrays { get; }

		// Token: 0x0400001F RID: 31
		private static readonly FeatureSupport Default = new FeatureSupport(false);

		// Token: 0x04000020 RID: 32
		private static readonly FeatureSupport Postgres = new FeatureSupport(true);
	}
}
