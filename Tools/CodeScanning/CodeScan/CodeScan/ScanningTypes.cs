using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

// ReSharper disable MemberCanBePrivate.Global

namespace System.Runtime.CompilerServices
{
	internal static class IsExternalInit {}
}

namespace UnitystationLauncher.ContentScanning
{
	public static class ScanningTypes
	{
		public class MMemberRef
		{
			public readonly MType ParentType;
			public readonly string Name;

			protected MMemberRef(MType parentType, string name)
			{
				ParentType = parentType;
				Name = name;
			}
		}

		internal sealed class MMemberRefMethod : MMemberRef
		{
			public readonly MType ReturnType;
			public readonly int GenericParameterCount;
			public readonly ImmutableArray<MType> ParameterTypes;

			public MMemberRefMethod(MType parentType, string name, MType returnType,
				int genericParameterCount, ImmutableArray<MType> parameterTypes) : base(parentType, name)
			{
				ReturnType = returnType;
				GenericParameterCount = genericParameterCount;
				ParameterTypes = parameterTypes;
			}

			public override string ToString()
			{
				return $"{ParentType}.{Name}({string.Join(", ", ParameterTypes)}) Returns {ReturnType}";
			}
		}

		internal sealed class MMemberRefField : MMemberRef
		{
			public readonly MType FieldType;

			public MMemberRefField(MType parentType, string name, MType fieldType) : base(parentType, name)
			{
				FieldType = fieldType;
			}

			public override string ToString()
			{
				return $"{ParentType}.{Name} Returns {FieldType}";
			}
		}

		internal sealed record MTypeParsed(string FullName, MTypeParsed NestedParent = null) : MType
		{
			public override string ToString()
			{
				return NestedParent != null ? $"{NestedParent}/{FullName}" : FullName;
			}

			public override bool WhitelistEquals(MType other)
			{
				switch (other)
				{
					case MTypeParsed parsed:
						if (NestedParent != null
						    && (parsed.NestedParent == null ||
						        NestedParent.WhitelistEquals(parsed.NestedParent) == false))
						{
							return false;
						}

						return parsed.FullName == FullName;
					case MTypeReferenced referenced:
						if (NestedParent != null
						    && (referenced.ResolutionScope is not MResScopeType parentRes ||
						        NestedParent.WhitelistEquals(parentRes.Type) == false))
						{
							return false;
						}

						var refFullName = referenced.Namespace == null
							? referenced.Name
							: $"{referenced.Namespace}.{referenced.Name}";
						return FullName == refFullName;
					default:
						return false;
				}
			}
		}

		internal sealed record MTypePrimitive(PrimitiveTypeCode TypeCode) : MType
		{
			public override string ToString()
			{
				return TypeCode switch
				{
					PrimitiveTypeCode.Void => "void",
					PrimitiveTypeCode.Boolean => "bool",
					PrimitiveTypeCode.Char => "char",
					PrimitiveTypeCode.SByte => "int8",
					PrimitiveTypeCode.Byte => "unsigned int8",
					PrimitiveTypeCode.Int16 => "int16",
					PrimitiveTypeCode.UInt16 => "unsigned int16",
					PrimitiveTypeCode.Int32 => "int32",
					PrimitiveTypeCode.UInt32 => "unsigned int32",
					PrimitiveTypeCode.Int64 => "int64",
					PrimitiveTypeCode.UInt64 => "unsigned int64",
					PrimitiveTypeCode.Single => "float32",
					PrimitiveTypeCode.Double => "float64",
					PrimitiveTypeCode.String => "string",
					// ReSharper disable once StringLiteralTypo
					PrimitiveTypeCode.TypedReference => "typedref",
					PrimitiveTypeCode.IntPtr => "native int",
					PrimitiveTypeCode.UIntPtr => "unsigned native int",
					PrimitiveTypeCode.Object => "object",
					_ => "???"
				};
			}

			public override string WhitelistToString()
			{
				return TypeCode switch
				{
					PrimitiveTypeCode.Void => "void",
					PrimitiveTypeCode.Boolean => "bool",
					PrimitiveTypeCode.Char => "char",
					PrimitiveTypeCode.SByte => "sbyte",
					PrimitiveTypeCode.Byte => "byte",
					PrimitiveTypeCode.Int16 => "short",
					PrimitiveTypeCode.UInt16 => "ushort",
					PrimitiveTypeCode.Int32 => "int",
					PrimitiveTypeCode.UInt32 => "uint",
					PrimitiveTypeCode.Int64 => "long",
					PrimitiveTypeCode.UInt64 => "ulong",
					PrimitiveTypeCode.Single => "float",
					PrimitiveTypeCode.Double => "double",
					PrimitiveTypeCode.String => "string",
					// ReSharper disable once StringLiteralTypo
					PrimitiveTypeCode.TypedReference => "typedref",
					// ReSharper disable once StringLiteralTypo
					PrimitiveTypeCode.IntPtr => "nint",
					// ReSharper disable once StringLiteralTypo
					PrimitiveTypeCode.UIntPtr => "unint",
					PrimitiveTypeCode.Object => "object",
					_ => "???"
				};
			}

			public override bool WhitelistEquals(MType other)
			{
				return Equals(other);
			}
		}

		// Normal single dimensional array with zero lower bound.
		internal sealed record MTypeSZArray(MType ElementType) : MType
		{
			public override string ToString()
			{
				return $"{ElementType}[]";
			}

			public override string WhitelistToString()
			{
				return $"{ElementType.WhitelistToString()}[]";
			}

			public override bool WhitelistEquals(MType other)
			{
				return other is MTypeSZArray arr && ElementType.WhitelistEquals(arr.ElementType);
			}
		}

		// Multi-dimension arrays with funny lower and upper bounds.
		internal sealed record MTypeWackyArray(MType ElementType, ArrayShape Shape) : MType
		{
			public override string ToString()
			{
				return $"{ElementType}[TODO]";
			}

			public override string WhitelistToString()
			{
				return $"{ElementType.WhitelistToString()}[TODO]";
			}

			public override bool WhitelistEquals(MType other)
			{
				return other is MTypeWackyArray arr && ShapesEqual(Shape, arr.Shape) &&
				       ElementType.WhitelistEquals(arr);
			}

			private static bool ShapesEqual(in ArrayShape a, in ArrayShape b)
			{
				return a.Rank == b.Rank && a.LowerBounds.SequenceEqual(b.LowerBounds) && a.Sizes.SequenceEqual(b.Sizes);
			}

			public override bool IsCoreTypeDefined()
			{
				return ElementType.IsCoreTypeDefined();
			}
		}

		internal sealed record MTypeByRef(MType ElementType) : MType
		{
			public override string ToString()
			{
				return $"{ElementType}&";
			}

			public override string WhitelistToString()
			{
				return $"ref {ElementType.WhitelistToString()}";
			}

			public override bool WhitelistEquals(MType other)
			{
				return other is MTypeByRef byRef && ElementType.WhitelistEquals(byRef.ElementType);
			}
		}

		internal sealed record MTypePointer(MType ElementType) : MType
		{
			public override string ToString()
			{
				return $"{ElementType}*";
			}

			public override string WhitelistToString()
			{
				return $"{ElementType.WhitelistToString()}*";
			}

			public override bool WhitelistEquals(MType other)
			{
				return other is MTypePointer ptr && ElementType.WhitelistEquals(ptr.ElementType);
			}
		}

		internal sealed record MTypeGeneric(MType GenericType,  ImmutableArray<MType>  TypeArguments) : MType
		{
			public override string ToString()
			{
				return $"{GenericType}<{string.Join(", ", TypeArguments)}>";
			}

			public override string WhitelistToString()
			{
				return
					$"{GenericType.WhitelistToString()}<{string.Join(", ", TypeArguments.Select(t => t.WhitelistToString()))}>";
			}

			public override bool WhitelistEquals(MType other)
			{
				if (!(other is MTypeGeneric generic))
				{
					return false;
				}

				if (TypeArguments.Length != generic.TypeArguments.Length)
				{
					return false;
				}

				for (var i = 0; i < TypeArguments.Length; i++)
				{
					var argA = TypeArguments[i];
					var argB = generic.TypeArguments[i];

					if (!argA.WhitelistEquals(argB))
					{
						return false;
					}
				}

				return GenericType.WhitelistEquals(generic.GenericType);
			}

			public bool Equals(MTypeGeneric otherGeneric)
			{
				return otherGeneric != null && GenericType.Equals(otherGeneric.GenericType) &&
				       TypeArguments.SequenceEqual(otherGeneric.TypeArguments);
			}

			public override int GetHashCode()
			{
				var hc = new HashCode();
				hc.Add(GenericType);
				foreach (var typeArg in TypeArguments)
				{
					hc.Add(typeArg);
				}

				return hc.ToHashCode();
			}

			public override bool IsCoreTypeDefined()
			{
				return GenericType.IsCoreTypeDefined();
			}
		}

		internal sealed record MTypeDefined(string Name, string Namespace, MTypeDefined Enclosing) : MType
		{
			public override string ToString()
			{
				var name = Namespace != null ? $"{Namespace}.{Name}" : Name;

				if (Enclosing != null)
				{
					return $"{Enclosing}/{name}";
				}

				return name;
			}

			public override bool IsCoreTypeDefined()
			{
				return true;
			}
		}

		internal sealed record MTypeReferenced(MResScope ResolutionScope, string Name, string Namespace) : MType
		{
			public override string ToString()
			{
				if (Namespace == null)
				{
					return $"{ResolutionScope}{Name}";
				}

				return $"{ResolutionScope}{Namespace}.{Name}";
			}

			public override string WhitelistToString()
			{
				if (Namespace == null)
				{
					return Name;
				}

				return $"{Namespace}.{Name}";
			}

			public override bool WhitelistEquals(MType other)
			{
				return other switch
				{
					MTypeParsed p => p.WhitelistEquals(this),
					// TODO: ResolutionScope doesn't actually implement equals
					// This is fine since we're not comparing these anywhere
					MTypeReferenced r => r.Namespace == Namespace && r.Name == Name &&
					                     r.ResolutionScope.Equals(ResolutionScope),
					_ => false
				};
			}
		}

		public record MResScope
		{
		}

		internal sealed record MResScopeType(MType Type) : MResScope
		{
			public override string ToString()
			{
				return $"{Type}/";
			}
		}

		internal sealed record MResScopeAssembly(string Name) : MResScope
		{
			public override string ToString()
			{
				return $"[{Name}]";
			}
		}

		internal sealed record MTypeGenericTypePlaceHolder(int Index) : MType
		{
			public override string ToString()
			{
				return $"!{Index}";
			}

			public override bool WhitelistEquals(MType other)
			{
				return Equals(other);
			}
		}

		internal sealed record MTypeGenericMethodPlaceHolder(int Index) : MType
		{
			public override string ToString()
			{
				return $"!!{Index}";
			}

			public override bool WhitelistEquals(MType other)
			{
				return Equals(other);
			}
		}

		internal sealed record MTypeModified(MType UnmodifiedType, MType ModifierType, bool Required) : MType
		{
			public override string ToString()
			{
				var modName = Required ? "modreq" : "modopt";
				return $"{UnmodifiedType} {modName}({ModifierType})";
			}

			public override string WhitelistToString()
			{
				return UnmodifiedType.WhitelistToString();
			}

			public override bool WhitelistEquals(MType other)
			{
				// TODO: This is asymmetric shit.
				return UnmodifiedType.WhitelistEquals(other);
			}
		}
	}
}