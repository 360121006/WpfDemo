using System;
using System.Reflection;

namespace Dapper
{
	// Token: 0x02000004 RID: 4
	public sealed class CustomPropertyTypeMap : SqlMapper.ITypeMap
	{
		// Token: 0x06000011 RID: 17 RVA: 0x0000236F File Offset: 0x0000056F
		public CustomPropertyTypeMap(Type type, Func<Type, string, PropertyInfo> propertySelector)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (propertySelector == null)
			{
				throw new ArgumentNullException("propertySelector");
			}
			this._type = type;
			this._propertySelector = propertySelector;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x000023A7 File Offset: 0x000005A7
		public ConstructorInfo FindConstructor(string[] names, Type[] types)
		{
			return this._type.GetConstructor(new Type[0]);
		}

		// Token: 0x06000013 RID: 19 RVA: 0x000023BA File Offset: 0x000005BA
		public ConstructorInfo FindExplicitConstructor()
		{
			return null;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000023BD File Offset: 0x000005BD
		public SqlMapper.IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06000015 RID: 21 RVA: 0x000023C4 File Offset: 0x000005C4
		public SqlMapper.IMemberMap GetMember(string columnName)
		{
			PropertyInfo propertyInfo = this._propertySelector(this._type, columnName);
			if (!(propertyInfo != null))
			{
				return null;
			}
			return new SimpleMemberMap(columnName, propertyInfo);
		}

		// Token: 0x0400000D RID: 13
		private readonly Type _type;

		// Token: 0x0400000E RID: 14
		private readonly Func<Type, string, PropertyInfo> _propertySelector;
	}
}
