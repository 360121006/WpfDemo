using System;
using System.Reflection;

namespace Dapper
{
	// Token: 0x02000010 RID: 16
	internal static class TypeExtensions
	{
		// Token: 0x060000E1 RID: 225 RVA: 0x00008A3C File Offset: 0x00006C3C
		public static string Name(this Type type)
		{
			return type.Name;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x00008A44 File Offset: 0x00006C44
		public static bool IsValueType(this Type type)
		{
			return type.IsValueType;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00008A4C File Offset: 0x00006C4C
		public static bool IsEnum(this Type type)
		{
			return type.IsEnum;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00008A54 File Offset: 0x00006C54
		public static bool IsGenericType(this Type type)
		{
			return type.IsGenericType;
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x00008A5C File Offset: 0x00006C5C
		public static bool IsInterface(this Type type)
		{
			return type.IsInterface;
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00008A64 File Offset: 0x00006C64
		public static TypeCode GetTypeCode(Type type)
		{
			return Type.GetTypeCode(type);
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00008A6C File Offset: 0x00006C6C
		public static MethodInfo GetPublicInstanceMethod(this Type type, string name, Type[] types)
		{
			return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null, types, null);
		}
	}
}
