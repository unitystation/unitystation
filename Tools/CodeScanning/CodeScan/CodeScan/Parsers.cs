using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace UnitystationLauncher.ContentScanning
{
	public static class Parsers
	{
		// Contains primary parsing code for method and field declarations in the sandbox whitelist.

		private static readonly Parser<char, PrimitiveTypeCode> VoidTypeParser =
			String("void").ThenReturn(PrimitiveTypeCode.Void);

		private static readonly Parser<char, PrimitiveTypeCode> BooleanTypeParser =
			String("bool").ThenReturn(PrimitiveTypeCode.Boolean);

		private static readonly Parser<char, PrimitiveTypeCode> CharTypeParser =
			String("char").ThenReturn(PrimitiveTypeCode.Char);

		private static readonly Parser<char, PrimitiveTypeCode> SByteTypeParser =
			String("sbyte").ThenReturn(PrimitiveTypeCode.SByte);

		private static readonly Parser<char, PrimitiveTypeCode> ByteTypeParser =
			String("byte").ThenReturn(PrimitiveTypeCode.Byte);

		private static readonly Parser<char, PrimitiveTypeCode> Int16TypeParser =
			String("short").ThenReturn(PrimitiveTypeCode.Int16);

		private static readonly Parser<char, PrimitiveTypeCode> UInt16TypeParser =
			String("ushort").ThenReturn(PrimitiveTypeCode.UInt16);

		private static readonly Parser<char, PrimitiveTypeCode> Int32TypeParser =
			String("int").ThenReturn(PrimitiveTypeCode.Int32);

		private static readonly Parser<char, PrimitiveTypeCode> UInt32TypeParser =
			String("uint").ThenReturn(PrimitiveTypeCode.UInt32);

		private static readonly Parser<char, PrimitiveTypeCode> Int64TypeParser =
			String("long").ThenReturn(PrimitiveTypeCode.Int64);

		private static readonly Parser<char, PrimitiveTypeCode> UInt64TypeParser =
			String("ulong").ThenReturn(PrimitiveTypeCode.UInt64);

		private static readonly Parser<char, PrimitiveTypeCode> IntPtrTypeParser =
			String("nint").ThenReturn(PrimitiveTypeCode.IntPtr);

		private static readonly Parser<char, PrimitiveTypeCode> UIntPtrTypeParser =
			String("nuint").ThenReturn(PrimitiveTypeCode.UIntPtr);

		private static readonly Parser<char, PrimitiveTypeCode> SingleTypeParser =
			String("float").ThenReturn(PrimitiveTypeCode.Single);

		private static readonly Parser<char, PrimitiveTypeCode> DoubleTypeParser =
			String("double").ThenReturn(PrimitiveTypeCode.Double);

		private static readonly Parser<char, PrimitiveTypeCode> StringTypeParser =
			String("string").ThenReturn(PrimitiveTypeCode.String);

		private static readonly Parser<char, PrimitiveTypeCode> ObjectTypeParser =
			String("object").ThenReturn(PrimitiveTypeCode.Object);

		private static readonly Parser<char, PrimitiveTypeCode> TypedReferenceTypeParser =
			String("typedref").ThenReturn(PrimitiveTypeCode.TypedReference);

		private static readonly Parser<char, MType> PrimitiveTypeParser =
			OneOf(
					Try(VoidTypeParser),
					Try(BooleanTypeParser),
					Try(CharTypeParser),
					Try(SByteTypeParser),
					Try(ByteTypeParser),
					Try(Int16TypeParser),
					Try(UInt16TypeParser),
					Try(Int32TypeParser),
					Try(UInt32TypeParser),
					Try(Int64TypeParser),
					Try(UInt64TypeParser),
					Try(IntPtrTypeParser),
					Try(UIntPtrTypeParser),
					Try(SingleTypeParser),
					Try(DoubleTypeParser),
					Try(StringTypeParser),
					Try(ObjectTypeParser),
					TypedReferenceTypeParser)
				.Select(code => (MType) new ScanningTypes.MTypePrimitive(code)).Labelled("Primitive type");

		private static readonly Parser<char, string> NamespacedIdentifier =
			Token(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '`')
				.AtLeastOnceString()
				.Labelled("valid identifier");

		private static readonly Parser<char, IEnumerable<MType>> GenericParametersParser =
			Rec(() => MaybeArrayTypeParser!)
				.Between(SkipWhitespaces)
				.Separated(Char(','))
				.Between(Char('<'), Char('>'));

		private static readonly Parser<char, MType> GenericMethodPlaceholderParser =
			String("!!")
				.Then(Digit.AtLeastOnceString())
				.Select(p =>
					(MType) new ScanningTypes.MTypeGenericMethodPlaceHolder(int.Parse(p,
						CultureInfo.InvariantCulture)));

		private static readonly Parser<char, MType> GenericTypePlaceholderParser =
			String("!")
				.Then(Digit.AtLeastOnceString())
				.Select(p =>
					(MType) new ScanningTypes.MTypeGenericTypePlaceHolder(int.Parse(p, CultureInfo.InvariantCulture)));

		private static readonly Parser<char, MType> GenericPlaceholderParser = Try(GenericTypePlaceholderParser)
			.Or(Try(GenericMethodPlaceholderParser)).Labelled("Generic placeholder");

		private static readonly Parser<char, ScanningTypes.MTypeParsed> TypeNameParser =
			Parser.Map(
				(a, b) => b.Aggregate(new ScanningTypes.MTypeParsed(a),
					(parsed, s) => new ScanningTypes.MTypeParsed(s, parsed)),
				NamespacedIdentifier,
				Char('/').Then(NamespacedIdentifier).Many());

		private static readonly Parser<char, MType> ConstructedObjectTypeParser =
			Parser.Map((arg1, arg2) =>
				{
					MType type = arg1;
					if (arg2.HasValue)
					{
						type = new ScanningTypes.MTypeGeneric(type, arg2.Value.ToImmutableArray());
					}

					return type;
				},
				TypeNameParser,
				GenericParametersParser.Optional());

		private static readonly Parser<char, MType> MaybeArrayTypeParser = Parser.Map(
			(a, b) => b.Aggregate(a, (type, _) => new ScanningTypes.MTypeSZArray(type)),
			Try(GenericPlaceholderParser).Or(Try(PrimitiveTypeParser)).Or(ConstructedObjectTypeParser),
			String("[]").Many());

		private static readonly Parser<char, MType> ByRefTypeParser =
			String("ref")
				.Then(SkipWhitespaces)
				.Then(MaybeArrayTypeParser)
				.Select(t => (MType) new ScanningTypes.MTypeByRef(t))
				.Labelled("ByRef type");

		private static readonly Parser<char, MType> TypeParser = Try(ByRefTypeParser).Or(MaybeArrayTypeParser);

		private static readonly Parser<char, MType[]> MethodParamsParser =
			TypeParser
				.Between(SkipWhitespaces)
				.Separated(Char(','))
				.Between(Char('('), Char(')'))
				.Select(p => p.ToArray());

		internal static readonly Parser<char, int> MethodGenericParameterCountParser =
			Try(Char(',').Many().Select(p => p.Count() + 1).Between(Char('<'), Char('>'))).Or(Return(0));

		internal static readonly Parser<char, WhitelistMethodDefine> MethodParser =
			Parser.Map(
				(a, b, d, c) => new WhitelistMethodDefine(b, a, c.ToList(), d),
				SkipWhitespaces.Then(TypeParser),
				SkipWhitespaces.Then(NamespacedIdentifier),
				MethodGenericParameterCountParser,
				SkipWhitespaces.Then(MethodParamsParser));

		internal static readonly Parser<char, WhitelistFieldDefine> FieldParser = Parser.Map(
			(a, b) => new WhitelistFieldDefine(b, a),
			MaybeArrayTypeParser.Between(SkipWhitespaces),
			NamespacedIdentifier);
	}
}