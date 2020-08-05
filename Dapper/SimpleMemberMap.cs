using System;
using System.Reflection;

namespace Dapper
{
	// Token: 0x0200000B RID: 11
	internal sealed class SimpleMemberMap : SqlMapper.IMemberMap
	{
		// Token: 0x06000045 RID: 69 RVA: 0x0000388A File Offset: 0x00001A8A
		public SimpleMemberMap(string columnName, PropertyInfo property)
		{
			if (columnName == null)
			{
				throw new ArgumentNullException("columnName");
			}
			if (property == null)
			{
				throw new ArgumentNullException("property");
			}
			this.ColumnName = columnName;
			this.Property = property;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x000038C2 File Offset: 0x00001AC2
		public SimpleMemberMap(string columnName, FieldInfo field)
		{
			if (columnName == null)
			{
				throw new ArgumentNullException("columnName");
			}
			if (field == null)
			{
				throw new ArgumentNullException("field");
			}
			this.ColumnName = columnName;
			this.Field = field;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x000038FA File Offset: 0x00001AFA
		public SimpleMemberMap(string columnName, ParameterInfo parameter)
		{
			if (columnName == null)
			{
				throw new ArgumentNullException("columnName");
			}
			if (parameter == null)
			{
				throw new ArgumentNullException("parameter");
			}
			this.ColumnName = columnName;
			this.Parameter = parameter;
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000048 RID: 72 RVA: 0x0000392C File Offset: 0x00001B2C
		public string ColumnName { get; }

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000049 RID: 73 RVA: 0x00003934 File Offset: 0x00001B34
		public Type MemberType
		{
			get
			{
				FieldInfo field = this.Field;
				Type result;
				if ((result = ((field != null) ? field.FieldType : null)) == null)
				{
					PropertyInfo property = this.Property;
					if ((result = ((property != null) ? property.PropertyType : null)) == null)
					{
						ParameterInfo parameter = this.Parameter;
						if (parameter == null)
						{
							return null;
						}
						result = parameter.ParameterType;
					}
				}
				return result;
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600004A RID: 74 RVA: 0x00003973 File Offset: 0x00001B73
		public PropertyInfo Property { get; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600004B RID: 75 RVA: 0x0000397B File Offset: 0x00001B7B
		public FieldInfo Field { get; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x0600004C RID: 76 RVA: 0x00003983 File Offset: 0x00001B83
		public ParameterInfo Parameter { get; }
	}
}
