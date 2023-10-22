using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Internal.TypeSystem.Ecma;

namespace UnitystationLauncher.ContentScanning

{
	public static class MetaMembersHelper
	{
		public static IEnumerable<string> DumpMetaMembers(Type type)
		{
			var assemblyLoc = type.Assembly.Location;

			// Load assembly with System.Reflection.Metadata.
			using var fs = File.OpenRead(assemblyLoc);
			using var peReader = new PEReader(fs);

			var metaReader = peReader.GetMetadataReader();

			// Find type definition in raw assembly metadata.
			// Is there a better way to do this than iterating??
			TypeDefinition typeDef = default;
			var found = false;
			foreach (var typeDefHandle in metaReader.TypeDefinitions)
			{
				var tempTypeDef = metaReader.GetTypeDefinition(typeDefHandle);
				var name = metaReader.GetString(tempTypeDef.Name);
				var @namespace = AssemblyTypeChecker.NilNullString(metaReader, tempTypeDef.Namespace);
				if (name == type.Name && @namespace == type.Namespace)
				{
					typeDef = tempTypeDef;
					found = true;
					break;
				}
			}

			if (!found)
			{
				throw new InvalidOperationException("Type didn't exist??");
			}

			// Dump the list.
			var provider = new AssemblyTypeChecker.TypeProvider();

			foreach (var fieldHandle in typeDef.GetFields())
			{
				var fieldDef = metaReader.GetFieldDefinition(fieldHandle);

				if ((fieldDef.Attributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public)
				{
					continue;
				}

				var fieldName = metaReader.GetString(fieldDef.Name);
				var fieldType = fieldDef.DecodeSignature(provider, 0);

				yield return $"{fieldType.WhitelistToString()} {fieldName}";
			}

			foreach (var methodHandle in typeDef.GetMethods())
			{
				var methodDef = metaReader.GetMethodDefinition(methodHandle);

				if (!methodDef.Attributes.IsPublic())
				{
					continue;
				}

				var methodName = metaReader.GetString(methodDef.Name);
				var methodSig = methodDef.DecodeSignature(provider, 0);

				var paramString = string.Join(", ", methodSig.ParameterTypes.Select(t => t.WhitelistToString()));
				var genericCount = methodSig.GenericParameterCount;
				var typeParamString = genericCount == 0
					? ""
					: $"<{new string(',', genericCount - 1)}>";

				yield return $"{methodSig.ReturnType.WhitelistToString()} {methodName}{typeParamString}({paramString})";
			}
		}
	}
}