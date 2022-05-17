#if NET_4_6
namespace System.Runtime.CompilerServices
{
	/// <summary>
	/// Resolves issues with record initializers.
	/// See https://stackoverflow.com/questions/64749385/predefined-type-system-runtime-compilerservices-isexternalinit-is-not-defined
	/// for an explanation behind this.
	/// Remove this when unity has upgraded to .NET 6/7
	/// </summary>
	public static class IsExternalInit {}
}
#endif