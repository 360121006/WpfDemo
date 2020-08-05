using System;
using System.Xml;

namespace Dapper
{
	// Token: 0x02000014 RID: 20
	internal sealed class XmlDocumentHandler : XmlTypeHandler<XmlDocument>
	{
		// Token: 0x06000110 RID: 272 RVA: 0x00008D01 File Offset: 0x00006F01
		protected override XmlDocument Parse(string xml)
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xml);
			return xmlDocument;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00008D0F File Offset: 0x00006F0F
		protected override string Format(XmlDocument xml)
		{
			return xml.OuterXml;
		}
	}
}
