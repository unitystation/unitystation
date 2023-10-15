using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using CodeScan;
using ILVerify;

// psst
// You know ECMA-335 right? The specification for the CLI that .NET runs on?
// Yeah, you need it to understand a lot of this code. So get a copy.
// You know the cool thing?
// ISO has a version that has correct PDF metadata so there's an actual table of contents.
// Right here: https://standards.iso.org/ittf/PubliclyAvailableStandards/c058046_ISO_IEC_23271_2012(E).zip

namespace UnitystationLauncher.ContentScanning
{
	/// <summary>
	///     Manages the type white/black list of types and namespaces, and verifies assemblies against them.
	/// </summary>
	public partial class AssemblyTypeChecker
	{
		// Used to be in Sandbox.yml, moved out of there to facilitate faster loading.
		private const string SystemAssemblyName = "mscorlib"; //TODO check security
		//UnityEngine.dll
		//mscorlib
		//System.Runtime

		/// <summary>
		///     Completely disables type checking, allowing everything.
		/// </summary>
		public bool DisableTypeCheck { get; set; }

		public DumpFlags Dump { get; set; } = DumpFlags.None;
		public bool VerifyIl { get; set; }

		public readonly Task<SandboxConfig> _config;


		public AssemblyTypeChecker()
		{
			VerifyIl = true;
			DisableTypeCheck = false;

			_config = Task.Run(LoadConfig);
		}


	 private sealed class Resolver : IResolver
    {
        private readonly DirectoryInfo _managedPath;

        private readonly DirectoryInfo BackupDlls = new DirectoryInfo(@"Q:\Fast programmes\ss13 development\unitystation\Tools\CodeScanning\CodeScan\CodeScan\bin\Debug\net7.0\MissingAssemblies");
        private readonly Dictionary<string, PEReader> _dictionaryLookup = new Dictionary<string, PEReader>();

        public void Dispose()
        {
            foreach (var Lookup in _dictionaryLookup)
            {
                Lookup.Value.Dispose();
            }
        }

        PEReader IResolver.ResolveAssembly(AssemblyName assemblyName)
        {
            if (assemblyName.Name == null)
            {
	            ThisMain.Errors.Add("Unable to find " + assemblyName.FullName);
                throw new FileNotFoundException("Unable to find " + assemblyName.FullName);
            }

            if (_dictionaryLookup.TryGetValue(assemblyName.Name, out var assembly))
            {
                return assembly;
            }

            FileInfo[] files = _managedPath.GetFiles("*.dll"); // Change the file extension to match your DLLs

            foreach (FileInfo file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file.Name);
                if (string.Equals(fileName, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Found DLL for assembly '{assemblyName.Name}': {file.FullName}");
                    _dictionaryLookup[assemblyName.Name] =
                        new PEReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
                    return _dictionaryLookup[assemblyName.Name];
                }
            }

	        files = BackupDlls.GetFiles("*.dll"); // Change the file extension to match your DLLs

            foreach (FileInfo file in files)
            {
	            string fileName = Path.GetFileNameWithoutExtension(file.Name);
	            if (string.Equals(fileName, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
	            {
		            Console.WriteLine($"Found DLL for assembly '{assemblyName.Name}': {file.FullName}");
		            _dictionaryLookup[assemblyName.Name] =
			            new PEReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
		            return _dictionaryLookup[assemblyName.Name];
	            }
            }

            ThisMain.Errors.Add("Unable to find it " + assemblyName.FullName);
            throw new FileNotFoundException("Unable to find it " + assemblyName.FullName);
        }

        public Resolver(DirectoryInfo inManagedPath)
        {
            _managedPath = inManagedPath;
        }

        PEReader IResolver.ResolveModule(AssemblyName referencingAssembly, string fileName)
        {
            //TODO idk This is never used anywhere
            throw new NotImplementedException(
                $"idk How IResolver.ResolveModule(AssemblyName {referencingAssembly}, string {fileName}) , And it's never been called so.. ");
        }
		}

		private Resolver CreateResolver(DirectoryInfo ManagedPath)
		{
			return new Resolver(ManagedPath);
		}

		/// <summary>
		///     Check the assembly for any illegal types. Any types not on the white list
		///     will cause the assembly to be rejected.
		/// </summary>
		/// <param name="diskPath"></param>
		/// <param name="managedPath"></param>
		/// <param name="otherAssemblies"></param>
		/// <param name="info"></param>
		/// <param name="Errors"></param>
		/// <param name="assembly">Assembly to load.</param>
		/// <returns></returns>
		public bool CheckAssembly(FileInfo diskPath, DirectoryInfo managedPath, List<string> otherAssemblies,
			Action<string> info, Action<string> Errors)
		{
			using var assembly = diskPath.OpenRead();
			var fullStopwatch = Stopwatch.StartNew();

			var resolver = CreateResolver(managedPath);
			using var peReader = new PEReader(assembly, PEStreamOptions.LeaveOpen);
			var reader = peReader.GetMetadataReader();

			var asmName = reader.GetString(reader.GetAssemblyDefinition().Name);

			if (peReader.PEHeaders.CorHeader?.ManagedNativeHeaderDirectory is {Size: not 0})
			{
				Errors.Invoke($"Assembly {asmName} contains native code.");
				return false;
			}

			VerifyIl = false;
			if (VerifyIl)
			{
				if (DoVerifyIL(asmName, resolver, peReader, reader, info, Errors) == false)
				{
					Errors.Invoke($"Assembly {asmName} Has invalid IL code");
					return false;
				}
			}


			var errors = new ConcurrentBag<SandboxError>();

			var types = GetReferencedTypes(reader, errors);
			var members = GetReferencedMembers(reader, errors);
			var inherited = GetExternalInheritedTypes(reader, errors);
			info.Invoke($"References loaded... {fullStopwatch.ElapsedMilliseconds}ms");

			if (DisableTypeCheck)
			{
				resolver.Dispose();
				peReader.Dispose();
				return true;
			}

#pragma warning disable RA0004
			var loadedConfig = _config.Result;
#pragma warning restore RA0004

			loadedConfig.MultiAssemblyOtherReferences.Clear();
			loadedConfig.MultiAssemblyOtherReferences.AddRange(otherAssemblies);

			// We still do explicit type reference scanning, even though the actual whitelists work with raw members.
			// This is so that we can simplify handling of generic type specifications during member checking:
			// we won't have to check that any types in their type arguments are whitelisted.
			foreach (var type in types)
			{
				if (IsTypeAccessAllowed(loadedConfig, type, out _) == false)
				{
					errors.Add(new SandboxError($"Access to type not allowed: {type} asmName {asmName}"));
				}
			}

			info.Invoke($"Types... {fullStopwatch.ElapsedMilliseconds}ms");

			CheckInheritance(loadedConfig, inherited, errors);

			info.Invoke($"Inheritance... {fullStopwatch.ElapsedMilliseconds}ms");

			CheckNoUnmanagedMethodDefs(reader, errors);

			info.Invoke($"Unmanaged methods... {fullStopwatch.ElapsedMilliseconds}ms");

			CheckNoTypeAbuse(reader, errors);

			info.Invoke($"Type abuse... {fullStopwatch.ElapsedMilliseconds}ms");

			CheckMemberReferences(loadedConfig, members, errors, asmName);

			errors = new ConcurrentBag<SandboxError>(errors.OrderBy(x => x.Message));

			foreach (var error in errors)
			{
				Errors.Invoke($"Sandbox violation: {error.Message}");
			}

			info.Invoke($"Checked assembly in {fullStopwatch.ElapsedMilliseconds}ms");
			resolver.Dispose();
			peReader.Dispose();
			return errors.IsEmpty;
		}

		private bool DoVerifyIL(
			string name,
			IResolver resolver,
			PEReader peReader,
			MetadataReader reader,
			Action<string> info,
			Action<string> logErrors)
		{
			info.Invoke($"{name}: Verifying IL...");
			var sw = Stopwatch.StartNew();
			var bag = new ConcurrentBag<VerificationResult>();


			var UesParallel = false;

			if (UesParallel)
			{
				var partitioner = Partitioner.Create(reader.TypeDefinitions);
				Parallel.ForEach(partitioner.GetPartitions(Environment.ProcessorCount), handle =>
				{
					var ver = new Verifier(resolver);
					ver.SetSystemModuleName(new AssemblyName(SystemAssemblyName));
					while (handle.MoveNext())
					{
						foreach (var result in ver.Verify(peReader, handle.Current, verifyMethods: true))
						{
							bag.Add(result);
						}
					}
				});
			}
			else
			{
				var ver = new Verifier(resolver);
				//mscorlib
				ver.SetSystemModuleName(new AssemblyName(SystemAssemblyName));
				foreach (var Definition in reader.TypeDefinitions)
				{
					var Errors = ver.Verify(peReader, Definition, verifyMethods: true);
					foreach (var Error in Errors)
					{
						bag.Add(Error);
					}
				}
			}

			var loadedCfg = _config.Result;

			var verifyErrors = false;
			foreach (var res in bag)
			{
				if (loadedCfg.AllowedVerifierErrors.Contains(res.Code))
				{
					continue;
				}

				var formatted = res.Args == null ? res.Message : string.Format(res.Message, res.Args);
				var msg = $"{name}: ILVerify: {formatted}";

				if (!res.Method.IsNil)
				{
					var method = reader.GetMethodDefinition(res.Method);
					var methodName = FormatMethodName(reader, method);

					msg = $"{msg}, method: {methodName}";
				}

				if (!res.Type.IsNil)
				{
					var type = GetTypeFromDefinition(reader, res.Type);
					msg = $"{msg}, type: {type}";
				}


				verifyErrors = true;
				logErrors.Invoke(msg);
			}

			info.Invoke($"{name}: Verified IL in {sw.Elapsed.TotalMilliseconds}ms");

			if (verifyErrors)
			{
				return false;
			}

			return true;
		}

		private static string FormatMethodName(MetadataReader reader, MethodDefinition method)
		{
			var methodSig = method.DecodeSignature(new TypeProvider(), 0);
			var type = GetTypeFromDefinition(reader, method.GetDeclaringType());

			return
				$"{type}.{reader.GetString(method.Name)}({string.Join(", ", methodSig.ParameterTypes)}) Returns {methodSig.ReturnType} ";
		}

		[SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")]
		private static void CheckNoUnmanagedMethodDefs(MetadataReader reader, ConcurrentBag<SandboxError> errors)
		{
			foreach (var methodDefHandle in reader.MethodDefinitions)
			{
				var methodDef = reader.GetMethodDefinition(methodDefHandle);
				var implAttr = methodDef.ImplAttributes;
				var attr = methodDef.Attributes;

				if ((implAttr & MethodImplAttributes.Unmanaged) != 0 ||
				    (implAttr & MethodImplAttributes.CodeTypeMask) is not (MethodImplAttributes.IL
				    or MethodImplAttributes.Runtime))
				{
					var err = $"Method has illegal MethodImplAttributes: {FormatMethodName(reader, methodDef)}";
					errors.Add(new SandboxError(err));
				}

				if ((attr & (MethodAttributes.PinvokeImpl | MethodAttributes.UnmanagedExport)) != 0)
				{
					var err = $"Method has illegal MethodAttributes: {FormatMethodName(reader, methodDef)}";
					errors.Add(new SandboxError(err));
				}
			}
		}

		private static void CheckNoTypeAbuse(MetadataReader reader, ConcurrentBag<SandboxError> errors)
		{
			foreach (var typeDefHandle in reader.TypeDefinitions)
			{
				var typeDef = reader.GetTypeDefinition(typeDefHandle);
				if ((typeDef.Attributes & TypeAttributes.ExplicitLayout) != 0)
				{
					// The C# compiler emits explicit layout types for some array init logic. These have no fields.
					// Only ban explicit layout if it has fields.

					var type = GetTypeFromDefinition(reader, typeDefHandle);

					if (typeDef.GetFields().Count > 0)
					{
						var err = $"Explicit layout type {type} may not have fields.";
						errors.Add(new SandboxError(err));
					}
				}
			}
		}

		private void CheckMemberReferences(
			SandboxConfig sandboxConfig,
			List<ScanningTypes.MMemberRef> members,
			ConcurrentBag<SandboxError> errors,
			string asmName
			)
		{
			bool IsParallel = true;

			if (IsParallel)
			{
				Parallel.ForEach(members, memberRef =>
				{
					MType baseType = memberRef.ParentType;
					while (!(baseType is ScanningTypes.MTypeReferenced))
					{
						switch (baseType)
						{
							case ScanningTypes.MTypeGeneric generic:
							{
								baseType = generic.GenericType;

								break;
							}
							case ScanningTypes.MTypeWackyArray:
							{
								// Members on arrays (not to be confused with vectors) are all fine.
								// See II.14.2 in ECMA-335.
								return;
							}
							case ScanningTypes.MTypeDefined:
							{
								// Valid for this to show up, safe to ignore.
								return;
							}
							default:
							{
								throw new ArgumentOutOfRangeException();
							}
						}
					}

					var baseTypeReferenced = (ScanningTypes.MTypeReferenced) baseType;

					if (IsTypeAccessAllowed(sandboxConfig, baseTypeReferenced, out var typeCfg) == false)
					{
						// Technically this error isn't necessary since we have an earlier pass
						// checking all referenced types. That should have caught this
						// We still need the typeCfg so that's why we're checking. Might as well.
						errors.Add(new SandboxError($"Access to type not allowed: {baseTypeReferenced} in Assembly {asmName}"));
						return;
					}

					if (typeCfg.All)
					{
						// Fully whitelisted for the type, we good.
						return;
					}

					switch (memberRef)
					{
						case ScanningTypes.MMemberRefField mMemberRefField:
						{
							foreach (var field in typeCfg.FieldsParsed)
							{
								if (field.Name == mMemberRefField.Name &&
								    mMemberRefField.FieldType.WhitelistEquals(field.FieldType))
								{
									return; // Found
								}
							}

							errors.Add(new SandboxError($"Access to field not allowed: {mMemberRefField}"));
							break;
						}
						case ScanningTypes.MMemberRefMethod mMemberRefMethod:
							foreach (var parsed in typeCfg.MethodsParsed)
							{
								bool notParamMismatch = true;

								if (parsed.Name == mMemberRefMethod.Name &&
								    mMemberRefMethod.ReturnType.WhitelistEquals(parsed.ReturnType) &&
								    mMemberRefMethod.ParameterTypes.Length == parsed.ParameterTypes.Count &&
								    mMemberRefMethod.GenericParameterCount == parsed.GenericParameterCount)
								{
									for (var i = 0; i < mMemberRefMethod.ParameterTypes.Length; i++)
									{
										var a = mMemberRefMethod.ParameterTypes[i];
										var b = parsed.ParameterTypes[i];

										if (a.WhitelistEquals(b) == false)
										{
											notParamMismatch = false;
											break;
										}
									}

									if (notParamMismatch)
									{
										return; // Found
									}
								}
							}

							errors.Add(new SandboxError($"Access to method not allowed: {mMemberRefMethod} in Assembly {asmName}"));
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(memberRef));
					}
				});
			}
			else
			{
				foreach (var memberRef in members)
				{
					MType baseType = memberRef.ParentType;
					while (!(baseType is ScanningTypes.MTypeReferenced))
					{
						switch (baseType)
						{
							case ScanningTypes.MTypeGeneric generic:
							{
								baseType = generic.GenericType;

								break;
							}
							case ScanningTypes.MTypeWackyArray:
							{
								// Members on arrays (not to be confused with vectors) are all fine.
								// See II.14.2 in ECMA-335.
								continue;
							}
							case ScanningTypes.MTypeDefined:
							{
								// Valid for this to show up, safe to ignore.
								continue;
							}
							default:
							{
								throw new ArgumentOutOfRangeException();
							}
						}
					}

					var baseTypeReferenced = (ScanningTypes.MTypeReferenced) baseType;

					if (IsTypeAccessAllowed(sandboxConfig, baseTypeReferenced, out var typeCfg) == false)
					{
						// Technically this error isn't necessary since we have an earlier pass
						// checking all referenced types. That should have caught this
						// We still need the typeCfg so that's why we're checking. Might as well.
						errors.Add(new SandboxError($"Access to type not allowed: {baseTypeReferenced} in Assembly {asmName}"));
						continue;
					}

					if (typeCfg.All)
					{
						// Fully whitelisted for the type, we good.
						continue;
					}

					switch (memberRef)
					{
						case ScanningTypes.MMemberRefField mMemberRefField:
						{
							foreach (var field in typeCfg.FieldsParsed)
							{
								if (field.Name == mMemberRefField.Name &&
								    mMemberRefField.FieldType.WhitelistEquals(field.FieldType))
								{
									continue; // Found
								}
							}

							errors.Add(new SandboxError($"Access to field not allowed: {mMemberRefField} in Assembly {asmName}"));
							break;
						}
						case ScanningTypes.MMemberRefMethod mMemberRefMethod:
							bool notParamMismatch = true;
							foreach (var parsed in typeCfg.MethodsParsed)
							{
								if (parsed.Name == mMemberRefMethod.Name &&
								    mMemberRefMethod.ReturnType.WhitelistEquals(parsed.ReturnType) &&
								    mMemberRefMethod.ParameterTypes.Length == parsed.ParameterTypes.Count &&
								    mMemberRefMethod.GenericParameterCount == parsed.GenericParameterCount)
								{
									for (var i = 0; i < mMemberRefMethod.ParameterTypes.Length; i++)
									{
										var a = mMemberRefMethod.ParameterTypes[i];
										var b = parsed.ParameterTypes[i];

										if (!a.WhitelistEquals(b))
										{
											notParamMismatch = false;
											break;
										}
									}

									if (notParamMismatch)
									{
										break; // Found
									}

									break;
								}
							}

							if (notParamMismatch == false)
							{
								continue;
							}

							errors.Add(new SandboxError($"Access to method not allowed: {mMemberRefMethod} in Assembly {asmName}"));
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(memberRef));
					}
				}
			}
		}

		private void CheckInheritance(
			SandboxConfig sandboxConfig,
			List<(MType type, MType parent, ArraySegment<MType> interfaceImpls)> inherited,
			ConcurrentBag<SandboxError> errors)
		{
			// This inheritance whitelisting primarily serves to avoid content doing funny stuff
			// by e.g. inheriting Type.
			foreach (var (_, baseType, interfaces) in inherited)
			{
				if (CanInherit(baseType) == false)
				{
					errors.Add(new SandboxError($"Inheriting of type not allowed: {baseType}"));
				}

				foreach (var @interface in interfaces)
				{
					if (CanInherit(@interface) == false)
					{
						errors.Add(new SandboxError($"Implementing of interface not allowed: {@interface}"));
					}
				}

				bool CanInherit(MType inheritType)
				{
					ScanningTypes.MTypeReferenced realBaseType;
					switch (inheritType)
					{
						case ScanningTypes.MTypeGeneric generic:
							realBaseType = (ScanningTypes.MTypeReferenced) generic.GenericType;
							break;
						case ScanningTypes.MTypeReferenced referenced:
							realBaseType = referenced;
							break;
						default:
							if (inheritType == null) return true;
							ThisMain.Errors.Add(inheritType?.ToString());
							throw new InvalidOperationException(); // Can't happen.
					}

					if (IsTypeAccessAllowed(sandboxConfig, realBaseType, out var cfg) == false)
					{
						return false;
					}

					return cfg.Inherit != InheritMode.Block && (cfg.Inherit == InheritMode.Allow || cfg.All);
				}
			}
		}

		private bool IsTypeAccessAllowed(SandboxConfig sandboxConfig, ScanningTypes.MTypeReferenced type,
			[NotNullWhen(true)] out TypeConfig cfg)
		{
			if (type.Namespace == null)
			{
				if (type.ResolutionScope is ScanningTypes.MResScopeType parentType)
				{
					if (IsTypeAccessAllowed(sandboxConfig, (ScanningTypes.MTypeReferenced) parentType.Type,
						    out var parentCfg) == false)
					{
						cfg = null;
						return false;
					}

					if (parentCfg.All)
					{
						// Enclosing type is namespace-whitelisted so we don't have to check anything else.
						cfg = TypeConfig.DefaultAll;
						return true;
					}

					// Found enclosing type, checking if we are allowed to access this nested type.
					// Also pass it up in case of multiple nested types.
					if (parentCfg.NestedTypes != null && parentCfg.NestedTypes.TryGetValue(type.Name, out cfg))
					{
						return true;
					}

					cfg = null;
					return false;
				}

				if (type.ResolutionScope is ScanningTypes.MResScopeAssembly mResScopeAssembly &&
				    (sandboxConfig.MultiAssemblyOtherReferences.Contains(mResScopeAssembly.Name) || sandboxConfig.EditorOnlyAssemblies.Contains(mResScopeAssembly.Name)))
				{
					cfg = TypeConfig.DefaultAll; //TODO DEBUG!!!
					return true;
				}

				// Types without namespaces or nesting parent are not allowed at all.
				cfg = null;
				return false;
			}

			// Check if in whitelisted namespaces.
			foreach (var whNamespace in sandboxConfig.WhitelistedNamespaces)
			{
				if (type.Namespace.StartsWith(whNamespace))
				{
					cfg = TypeConfig.DefaultAll;
					return true;
				}
			}

			if (type.ResolutionScope is ScanningTypes.MResScopeAssembly resScopeAssembly &&
			    (sandboxConfig.MultiAssemblyOtherReferences.Contains(resScopeAssembly.Name) || sandboxConfig.EditorOnlyAssemblies.Contains(resScopeAssembly.Name)))
			{
				cfg = TypeConfig.DefaultAll; //TODO DEBUG!!!
				return true;
			}


			if (sandboxConfig.Types.TryGetValue(type.Namespace, out var nsDict) == false)
			{
				cfg = null;
				return false;
			}

			return nsDict.TryGetValue(type.Name, out cfg);
		}

		private List<ScanningTypes.MTypeReferenced> GetReferencedTypes(MetadataReader reader,
			ConcurrentBag<SandboxError> errors)
		{
			return reader.TypeReferences.Select(typeRefHandle =>
				{
					try
					{
						return ParseTypeReference(reader, typeRefHandle);
					}
					catch (UnsupportedMetadataException e)
					{
						errors.Add(new SandboxError(e));
						return null;
					}
				})
				.Where(p => p != null)
				.ToList()!;
		}

		private List<ScanningTypes.MMemberRef> GetReferencedMembers(MetadataReader reader,
			ConcurrentBag<SandboxError> errors)
		{
			bool Parallel = true;
			if (Parallel)
			{
				return reader.MemberReferences.AsParallel()
					.Select(memRefHandle =>
					{
						var memRef = reader.GetMemberReference(memRefHandle);
						var memName = reader.GetString(memRef.Name);
						MType parent;
						switch (memRef.Parent.Kind)
						{
							// See II.22.25 in ECMA-335.
							case HandleKind.TypeReference:
							{
								// Regular type reference.
								try
								{
									parent = ParseTypeReference(reader, (TypeReferenceHandle) memRef.Parent);
								}
								catch (UnsupportedMetadataException u)
								{
									errors.Add(new SandboxError(u));
									return null;
								}

								break;
							}
							case HandleKind.TypeDefinition:
							{
								try
								{
									parent = GetTypeFromDefinition(reader, (TypeDefinitionHandle) memRef.Parent);
								}
								catch (UnsupportedMetadataException u)
								{
									errors.Add(new SandboxError(u));
									return null;
								}

								break;
							}
							case HandleKind.TypeSpecification:
							{
								var typeSpec = reader.GetTypeSpecification((TypeSpecificationHandle) memRef.Parent);
								// Generic type reference.
								var provider = new TypeProvider();
								parent = typeSpec.DecodeSignature(provider, 0);

								if (parent.IsCoreTypeDefined())
								{
									// Ensure this isn't a self-defined type.
									// This can happen due to generics since MethodSpec needs to point to MemberRef.
									return null;
								}

								break;
							}
							case HandleKind.ModuleReference:
							{
								errors.Add(new SandboxError(
									$"Module global variables and methods are unsupported. Name: {memName}"));
								return null;
							}
							case HandleKind.MethodDefinition:
							{
								errors.Add(new SandboxError($"Vararg calls are unsupported. Name: {memName}"));
								return null;
							}
							default:
							{
								errors.Add(new SandboxError(
									$"Unsupported member ref parent type: {memRef.Parent.Kind}. Name: {memName}"));
								return null;
							}
						}

						ScanningTypes.MMemberRef memberRef;

						switch (memRef.GetKind())
						{
							case MemberReferenceKind.Method:
							{
								var sig = memRef.DecodeMethodSignature(new TypeProvider(), 0);

								memberRef = new ScanningTypes.MMemberRefMethod(
									parent,
									memName,
									sig.ReturnType,
									sig.GenericParameterCount,
									sig.ParameterTypes);

								break;
							}
							case MemberReferenceKind.Field:
							{
								var fieldType = memRef.DecodeFieldSignature(new TypeProvider(), 0);
								memberRef = new ScanningTypes.MMemberRefField(parent, memName, fieldType);
								break;
							}
							default:
								throw new ArgumentOutOfRangeException();
						}

						return memberRef;
					})
					.Where(p => p != null)
					.ToList()!;
			}
			else
			{
				return reader.MemberReferences.Select(memRefHandle =>
					{
						var memRef = reader.GetMemberReference(memRefHandle);
						var memName = reader.GetString(memRef.Name);
						MType parent;
						switch (memRef.Parent.Kind)
						{
							// See II.22.25 in ECMA-335.
							case HandleKind.TypeReference:
							{
								// Regular type reference.
								try
								{
									parent = ParseTypeReference(reader, (TypeReferenceHandle) memRef.Parent);
								}
								catch (UnsupportedMetadataException u)
								{
									errors.Add(new SandboxError(u));
									return null;
								}

								break;
							}
							case HandleKind.TypeDefinition:
							{
								try
								{
									parent = GetTypeFromDefinition(reader, (TypeDefinitionHandle) memRef.Parent);
								}
								catch (UnsupportedMetadataException u)
								{
									errors.Add(new SandboxError(u));
									return null;
								}

								break;
							}
							case HandleKind.TypeSpecification:
							{
								var typeSpec = reader.GetTypeSpecification((TypeSpecificationHandle) memRef.Parent);
								// Generic type reference.
								var provider = new TypeProvider();
								parent = typeSpec.DecodeSignature(provider, 0);

								if (parent.IsCoreTypeDefined())
								{
									// Ensure this isn't a self-defined type.
									// This can happen due to generics since MethodSpec needs to point to MemberRef.
									return null;
								}

								break;
							}
							case HandleKind.ModuleReference:
							{
								errors.Add(new SandboxError(
									$"Module global variables and methods are unsupported. Name: {memName}"));
								return null;
							}
							case HandleKind.MethodDefinition:
							{
								errors.Add(new SandboxError($"Vararg calls are unsupported. Name: {memName}"));
								return null;
							}
							default:
							{
								errors.Add(new SandboxError(
									$"Unsupported member ref parent type: {memRef.Parent.Kind}. Name: {memName}"));
								return null;
							}
						}

						ScanningTypes.MMemberRef memberRef;

						switch (memRef.GetKind())
						{
							case MemberReferenceKind.Method:
							{
								var sig = memRef.DecodeMethodSignature(new TypeProvider(), 0);

								memberRef = new ScanningTypes.MMemberRefMethod(
									parent,
									memName,
									sig.ReturnType,
									sig.GenericParameterCount,
									sig.ParameterTypes);

								break;
							}
							case MemberReferenceKind.Field:
							{
								var fieldType = memRef.DecodeFieldSignature(new TypeProvider(), 0);
								memberRef = new ScanningTypes.MMemberRefField(parent, memName, fieldType);
								break;
							}
							default:
								throw new ArgumentOutOfRangeException();
						}

						return memberRef;
					})
					.Where(p => p != null)
					.ToList()!;
			}
		}

		private List<(MType type, MType parent, ArraySegment<MType> interfaceImpls)> GetExternalInheritedTypes(
			MetadataReader reader,
			ConcurrentBag<SandboxError> errors)
		{
			var list = new List<(MType, MType, ArraySegment<MType>)>();
			foreach (var typeDefHandle in reader.TypeDefinitions)
			{
				var typeDef = reader.GetTypeDefinition(typeDefHandle);
				ArraySegment<MType> interfaceImpls;
				ScanningTypes.MTypeDefined type = GetTypeFromDefinition(reader, typeDefHandle);

				if (!ParseInheritType(type, typeDef.BaseType, out var parent, reader, errors))
				{
					continue;
				}

				var interfaceImplsCollection = typeDef.GetInterfaceImplementations();
				if (interfaceImplsCollection.Count == 0)
				{
					interfaceImpls = Array.Empty<MType>();
				}
				else
				{
					interfaceImpls = new MType[interfaceImplsCollection.Count];
					var i = 0;
					foreach (var implHandle in interfaceImplsCollection)
					{
						var interfaceImpl = reader.GetInterfaceImplementation(implHandle);

						if (ParseInheritType(type, interfaceImpl.Interface, out var implemented, reader, errors))
						{
							interfaceImpls.ToArray()[i++] = implemented;
						}
					}

					interfaceImpls = interfaceImpls[..i];
				}

				list.Add((type, parent, interfaceImpls));
			}

			return list;
		}

		private bool ParseInheritType(MType ownerType, EntityHandle handle, [NotNullWhen(true)] out MType type,
			MetadataReader reader, ConcurrentBag<SandboxError> errors)
		{
			type = default;

			switch (handle.Kind)
			{
				case HandleKind.TypeDefinition:
					// Definition to type in same assembly, allowed without hassle.
					return false;

				case HandleKind.TypeReference:
					// Regular type reference.
					try
					{
						type = ParseTypeReference(reader, (TypeReferenceHandle) handle);
						return true;
					}
					catch (UnsupportedMetadataException u)
					{
						errors.Add(new SandboxError(u));
						return false;
					}

				case HandleKind.TypeSpecification:
					var typeSpec = reader.GetTypeSpecification((TypeSpecificationHandle) handle);
					// Generic type reference.
					var provider = new TypeProvider();
					type = typeSpec.DecodeSignature(provider, 0);

					if (type.IsCoreTypeDefined())
					{
						// Ensure this isn't a self-defined type.
						// This can happen due to generics.
						return false;
					}

					break;

				default:
					errors.Add(new SandboxError(
						$"Unsupported BaseType of kind {handle.Kind} on type {ownerType}"));
					return false;
			}

			type = default!;
			return false;
		}


		private sealed class SandboxError
		{
			public string Message;

			public SandboxError(string message)
			{
				Message = message;
			}

			public SandboxError(UnsupportedMetadataException ume) : this($"Unsupported metadata: {ume.Message}")
			{
			}
		}


		/// <exception href="UnsupportedMetadataException">
		///     Thrown if the metadata does something funny we don't "support" like type forwarding.
		/// </exception>
		internal static ScanningTypes.MTypeReferenced ParseTypeReference(MetadataReader reader,
			TypeReferenceHandle handle)
		{
			var typeRef = reader.GetTypeReference(handle);
			var name = reader.GetString(typeRef.Name);
			var nameSpace = NilNullString(reader, typeRef.Namespace);
			ScanningTypes.MResScope resScope;

			// See II.22.38 in ECMA-335
			if (typeRef.ResolutionScope.IsNil)
			{
				throw new UnsupportedMetadataException(
					$"Null resolution scope on type Name: {nameSpace}.{name}. This indicates exported/forwarded types");
			}

			switch (typeRef.ResolutionScope.Kind)
			{
				case HandleKind.AssemblyReference:
				{
					// Different assembly.
					var assemblyRef =
						reader.GetAssemblyReference((AssemblyReferenceHandle) typeRef.ResolutionScope);
					var assemblyName = reader.GetString(assemblyRef.Name);
					resScope = new ScanningTypes.MResScopeAssembly(assemblyName);
					break;
				}
				case HandleKind.TypeReference:
				{
					// Nested type.
					var enclosingType = ParseTypeReference(reader, (TypeReferenceHandle) typeRef.ResolutionScope);
					resScope = new ScanningTypes.MResScopeType(enclosingType);
					break;
				}
				case HandleKind.ModuleReference:
				{
					// Same-assembly-different-module
					throw new UnsupportedMetadataException(
						$"Cross-module reference to type {nameSpace}.{name}. ");
				}
				default:
					// Edge cases not handled:
					// https://github.com/dotnet/runtime/blob/b2e5a89085fcd87e2fa9300b4bb00cd499c5845b/src/libraries/System.Reflection.Metadata/tests/Metadata/Decoding/DisassemblingTypeProvider.cs#L130-L132
					throw new UnsupportedMetadataException(
						$"TypeRef to {typeRef.ResolutionScope.Kind} for type {nameSpace}.{name}");
			}

			return new ScanningTypes.MTypeReferenced(resScope, name, nameSpace);
		}

		public sealed class UnsupportedMetadataException : Exception
		{
			public UnsupportedMetadataException()
			{
			}

			public UnsupportedMetadataException(string message) : base(message)
			{
			}

			public UnsupportedMetadataException(string message, Exception inner) : base(message, inner)
			{
			}
		}

		public static string NilNullString(MetadataReader reader, StringHandle handle)
		{
			return handle.IsNil ? null : reader.GetString(handle);
		}

		internal sealed class TypeProvider : ISignatureTypeProvider<MType, int>
		{
			public MType GetSZArrayType(MType elementType)
			{
				return new ScanningTypes.MTypeSZArray(elementType);
			}

			public MType GetArrayType(MType elementType, ArrayShape shape)
			{
				return new ScanningTypes.MTypeWackyArray(elementType, shape);
			}

			public MType GetByReferenceType(MType elementType)
			{
				return new ScanningTypes.MTypeByRef(elementType);
			}

			public MType GetGenericInstantiation(MType genericType, ImmutableArray<MType> typeArguments)
			{
				return new ScanningTypes.MTypeGeneric(genericType, typeArguments);
			}

			public MType GetPointerType(MType elementType)
			{
				return new ScanningTypes.MTypePointer(elementType);
			}

			public MType GetPrimitiveType(PrimitiveTypeCode typeCode)
			{
				return new ScanningTypes.MTypePrimitive(typeCode);
			}

			public MType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
			{
				return AssemblyTypeChecker.GetTypeFromDefinition(reader, handle);
			}

			public MType GetTypeFromReference(MetadataReader inReader, TypeReferenceHandle inHandle, byte inRawTypeKind)
			{
				return ParseTypeReference(inReader, inHandle);
			}

			public MType GetFunctionPointerType(MethodSignature<MType> signature)
			{
				throw new NotImplementedException();
			}

			public MType GetGenericMethodParameter(int genericContext, int index)
			{
				return new ScanningTypes.MTypeGenericMethodPlaceHolder(index);
			}

			public MType GetGenericTypeParameter(int genericContext, int index)
			{
				return new ScanningTypes.MTypeGenericTypePlaceHolder(index);
			}

			public MType GetModifiedType(MType modifier, MType unmodifiedType, bool isRequired)
			{
				return new ScanningTypes.MTypeModified(unmodifiedType, modifier, isRequired);
			}

			public MType GetPinnedType(MType elementType)
			{
				throw new NotImplementedException();
			}

			public MType GetTypeFromSpecification(MetadataReader reader, int genericContext,
				TypeSpecificationHandle handle,
				byte rawTypeKind)
			{
				return reader.GetTypeSpecification(handle).DecodeSignature(this, 0);
			}
		}

		private static ScanningTypes.MTypeDefined GetTypeFromDefinition(MetadataReader reader,
			TypeDefinitionHandle handle)
		{
			var typeDef = reader.GetTypeDefinition(handle);
			var name = reader.GetString(typeDef.Name);
			var ns = NilNullString(reader, typeDef.Namespace);
			ScanningTypes.MTypeDefined enclosing = null;
			if (typeDef.IsNested)
			{
				enclosing = GetTypeFromDefinition(reader, typeDef.GetDeclaringType());
			}

			return new ScanningTypes.MTypeDefined(name, ns, enclosing);
		}

		[Flags]
		public enum DumpFlags : byte
		{
			None = 0,
			Types = 1,
			Members = 2,
			Inheritance = 4,

			All = Types | Members | Inheritance
		}
	}
}