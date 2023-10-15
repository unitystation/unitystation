using System;
using System.Collections.Generic;

namespace UnitystationLauncher.ContentScanning
{
	public sealed class TypeConfig
	{
		// Used for type configs where the type config doesn't exist due to a bigger-scoped All whitelisting.
		// e.g. nested types or namespace whitelist.
		public static readonly TypeConfig DefaultAll = new TypeConfig { All = true };

		public bool All { get; set; }
		public InheritMode Inherit { get; set; } = InheritMode.Default;
		public string[] Methods { get; set; }
		[NonSerialized] public WhitelistMethodDefine[] MethodsParsed = Array.Empty<WhitelistMethodDefine>();
		public string[] Fields { get; set; }
		[NonSerialized] public WhitelistFieldDefine[] FieldsParsed = Array.Empty<WhitelistFieldDefine>();
		public Dictionary<string, TypeConfig> NestedTypes { get; set; }
	}
}

