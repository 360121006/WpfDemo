using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dapper
{
	// Token: 0x02000007 RID: 7
	public sealed class DefaultTypeMap : SqlMapper.ITypeMap
	{
		// Token: 0x06000025 RID: 37 RVA: 0x00002540 File Offset: 0x00000740
		public DefaultTypeMap(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			this._fields = DefaultTypeMap.GetSettableFields(type);
			this.Properties = DefaultTypeMap.GetSettableProps(type);
			this._type = type;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x0000257C File Offset: 0x0000077C
		internal static MethodInfo GetPropertySetter(PropertyInfo propertyInfo, Type type)
		{
			if (propertyInfo.DeclaringType == type)
			{
				return propertyInfo.GetSetMethod(true);
			}
			return propertyInfo.DeclaringType.GetProperty(propertyInfo.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.DefaultBinder, propertyInfo.PropertyType, (from p in propertyInfo.GetIndexParameters()
			select p.ParameterType).ToArray<Type>(), null).GetSetMethod(true);
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000025F4 File Offset: 0x000007F4
		internal static List<PropertyInfo> GetSettableProps(Type t)
		{
			return (from p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			where DefaultTypeMap.GetPropertySetter(p, t) != null
			select p).ToList<PropertyInfo>();
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002631 File Offset: 0x00000831
		internal static List<FieldInfo> GetSettableFields(Type t)
		{
			return t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList<FieldInfo>();
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002640 File Offset: 0x00000840
		public ConstructorInfo FindConstructor(string[] names, Type[] types)
		{
			foreach (ConstructorInfo constructorInfo in this._type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(delegate(ConstructorInfo c)
			{
				if (c.IsPublic)
				{
					return 0;
				}
				if (!c.IsPrivate)
				{
					return 1;
				}
				return 2;
			}).ThenBy((ConstructorInfo c) => c.GetParameters().Length))
			{
				ParameterInfo[] parameters = constructorInfo.GetParameters();
				if (parameters.Length == 0)
				{
					return constructorInfo;
				}
				if (parameters.Length == types.Length)
				{
					int num = 0;
					while (num < parameters.Length && string.Equals(parameters[num].Name, names[num], StringComparison.OrdinalIgnoreCase))
					{
						if (!(types[num] == typeof(byte[])) || !(parameters[num].ParameterType.FullName == "System.Data.Linq.Binary"))
						{
							Type type = Nullable.GetUnderlyingType(parameters[num].ParameterType) ?? parameters[num].ParameterType;
							if (type != types[num] && !SqlMapper.HasTypeHandler(type) && (!type.IsEnum() || !(Enum.GetUnderlyingType(type) == types[num])) && (!(type == typeof(char)) || !(types[num] == typeof(string))) && (!type.IsEnum() || !(types[num] == typeof(string))))
							{
								break;
							}
						}
						num++;
					}
					if (num == parameters.Length)
					{
						return constructorInfo;
					}
				}
			}
			return null;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x000027FC File Offset: 0x000009FC
		public ConstructorInfo FindExplicitConstructor()
		{
			List<ConstructorInfo> list = (from c in this._type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			where c.GetCustomAttributes(typeof(ExplicitConstructorAttribute), true).Length != 0
			select c).ToList<ConstructorInfo>();
			if (list.Count == 1)
			{
				return list[0];
			}
			return null;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002854 File Offset: 0x00000A54
		public SqlMapper.IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName)
		{
			ParameterInfo[] parameters = constructor.GetParameters();
			return new SimpleMemberMap(columnName, parameters.FirstOrDefault((ParameterInfo p) => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase)));
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002894 File Offset: 0x00000A94
		public SqlMapper.IMemberMap GetMember(string columnName)
		{
			PropertyInfo propertyInfo = this.Properties.FirstOrDefault((PropertyInfo p) => string.Equals(p.Name, columnName, StringComparison.Ordinal)) ?? this.Properties.FirstOrDefault((PropertyInfo p) => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));
			if (propertyInfo == null && DefaultTypeMap.MatchNamesWithUnderscores)
			{
				propertyInfo = (this.Properties.FirstOrDefault((PropertyInfo p) => string.Equals(p.Name, columnName.Replace("_", ""), StringComparison.Ordinal)) ?? this.Properties.FirstOrDefault((PropertyInfo p) => string.Equals(p.Name, columnName.Replace("_", ""), StringComparison.OrdinalIgnoreCase)));
			}
			if (propertyInfo != null)
			{
				return new SimpleMemberMap(columnName, propertyInfo);
			}
			string backingFieldName = "<" + columnName + ">k__BackingField";
			FieldInfo fieldInfo;
			if ((fieldInfo = this._fields.FirstOrDefault((FieldInfo p) => string.Equals(p.Name, columnName, StringComparison.Ordinal))) == null && (fieldInfo = this._fields.FirstOrDefault((FieldInfo p) => string.Equals(p.Name, backingFieldName, StringComparison.Ordinal))) == null)
			{
				fieldInfo = (this._fields.FirstOrDefault((FieldInfo p) => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase)) ?? this._fields.FirstOrDefault((FieldInfo p) => string.Equals(p.Name, backingFieldName, StringComparison.OrdinalIgnoreCase)));
			}
			FieldInfo fieldInfo2 = fieldInfo;
			if (fieldInfo2 == null && DefaultTypeMap.MatchNamesWithUnderscores)
			{
				string effectiveColumnName = columnName.Replace("_", "");
				backingFieldName = "<" + effectiveColumnName + ">k__BackingField";
				FieldInfo fieldInfo3;
				if ((fieldInfo3 = this._fields.FirstOrDefault((FieldInfo p) => string.Equals(p.Name, effectiveColumnName, StringComparison.Ordinal))) == null && (fieldInfo3 = this._fields.FirstOrDefault((FieldInfo p) => string.Equals(p.Name, backingFieldName, StringComparison.Ordinal))) == null)
				{
					fieldInfo3 = (this._fields.FirstOrDefault((FieldInfo p) => string.Equals(p.Name, effectiveColumnName, StringComparison.OrdinalIgnoreCase)) ?? this._fields.FirstOrDefault((FieldInfo p) => string.Equals(p.Name, backingFieldName, StringComparison.OrdinalIgnoreCase)));
				}
				fieldInfo2 = fieldInfo3;
			}
			if (fieldInfo2 != null)
			{
				return new SimpleMemberMap(columnName, fieldInfo2);
			}
			return null;
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600002D RID: 45 RVA: 0x00002A90 File Offset: 0x00000C90
		// (set) Token: 0x0600002E RID: 46 RVA: 0x00002A97 File Offset: 0x00000C97
		public static bool MatchNamesWithUnderscores { get; set; }

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600002F RID: 47 RVA: 0x00002A9F File Offset: 0x00000C9F
		public List<PropertyInfo> Properties { get; }

		// Token: 0x04000015 RID: 21
		private readonly List<FieldInfo> _fields;

		// Token: 0x04000016 RID: 22
		private readonly Type _type;
	}
}
