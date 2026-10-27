# CodeScan

CodeScan is a .NET command-line utility that inspects managed assemblies in a Unity player build. Its main purpose is to enforce a sandbox policy: it reads assembly metadata and rejects references to types, methods, fields, and inheritance relationships that are not allowed by the policy. It does not load the scanned assemblies as executable .NET code.

The implementation is in `ThisMain.cs` and `AssemblyTypeChecker.cs`. Policy models and the whitelist signature parser are in `ScanConfigMode.cs`, `TypeConfig.cs`, `InheritMode.cs`, `WhitelistMethodDefine.cs`, `WhitelistFieldDefine.cs`, and `Parsers.cs`. Metadata representations used during scanning are in `ScanningTypes.cs`.

This is based off how SS14 handles their own codescans, and was adapted in its current form by Bod for Unitystation.

## Run and build

The project targets .NET 7 and produces an executable (`CodeScan.csproj`). The adjacent `HOWTO.txt` gives the current build commands:

```text
dotnet build -c Debug -r linux-x64
dotnet build -c Debug -r win-x64
dotnet build -c Debug -r osx-x64
```

At runtime, the tool expects `FilesToMoveToManaged.json` one directory above the executable directory. That file is deserialized as a list of filename substrings. The code's build output includes a copy of this file; when packaging or running from a different layout, preserve the expected relative location.

The player build path is selected from arguments. The first argument containing `>` is treated as the path after removing `>`; absent such an argument, the source contains a developer-specific default path. The path is expected to point at the build root, which contains one of:

| OS      | Managed assembly directory                |
| ------- | ----------------------------------------- |
| Windows | `Unitystation_Data/Managed`               |
| Linux   | `Unitystation_Data/Managed`               |
| macOS   | `Mac.app/Contents/Resources/Data/Managed` |

The OS must be Windows, Linux, or macOS for `ManagedPath` to be assigned. This is directory-layout logic, not a general player discovery mechanism.

Any argument containing `@` sets `MemoryClearingMode`. Currently that mode has an empty body: the usual assembly scan is skipped, but the program still loads `FilesToMoveToManaged.json` and writes `errors.json` (normally an empty array) at the end.

## Execution flow

1. `ThisMain.Main` determines the executable directory and initializes process-wide error, info, and file lists.
2. It reads `FilesToMoveToManaged.json`. If normal mode is active, it creates an `AssemblyTypeChecker`. The checker starts loading policy asynchronously in its constructor.
3. It computes the player Managed directory and creates a sibling `MissingAssemblies/<OS>` directory next to the CodeScan executable.
4. It copies each Managed-directory file whose name contains any configured `FilesToMoveToManaged` entry into that OS-specific directory, replacing an existing copy. These copied files are excluded from the ordinary scan.
5. It creates the set of assembly names present in the Managed directory, excluding copied files and `.pdb` files. For each remaining file (again excluding `.pdb`), it calls `CheckAssembly`, passing the other assembly names for reference resolution/policy decisions.
6. `CheckAssembly` reads the PE/CLI metadata. It rejects an assembly with a non-empty managed native header, gathers referenced types, member references, and external base/interface types, applies policy checks, and reports violations. IL verification code exists, but `VerifyIl` is set to `false` in the method immediately before its conditional check, so ILVerify is currently not run.
7. At the end, `ThisMain` serializes `Errors` as indented JSON to `errors.json` next to the executable. `Infos` is accumulated but is not written to that file. A failed assembly adds a summary error in addition to any specific errors.

Errors and progress are collected in static lists / callbacks. The scanner doesn't currently set an explicit process exit code based on findings; callers should inspect `errors.json` (and any console output) rather than assume the exit code indicates policy success.

## Policy loading and shape

`AssemblyTypeChecker.Config.cs` downloads the policy from:

```text
https://raw.githubusercontent.com/unitystation/unitystation/develop/CodeScanList.json
```

The root-level `CodeScanList.json` in this checkout is useful for understanding the policy schema, but the runtime's source of truth is the remote `develop` branch. A successful HTTP response is deserialized into `SandboxConfig`; download or parse failures are recorded in the errors list. There is no local-file fallback in this loader.

The policy model contains:

| Property                       | Meaning in the scanner                                                                                                                            |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| `WhitelistedNamespaces`        | Namespace prefixes allowed wholesale. The current check uses `StartsWith`, so entries should be chosen carefully.                                 |
| `Types`                        | Nested namespace and type maps. A type can allow all access or list allowed methods and fields, configure inheritance, and describe nested types. |
| `MultiAssemblyOtherReferences` | Populated at scan time from other assemblies in the Managed folder; types with these assembly scopes are treated as allowed.                      |
| `EditorOnlyAssemblies`         | Assemblies whose scoped types are treated as allowed.                                                                                             |
| `AllowedVerifierErrors`        | Data for ILVerify allowances; currently not exercised by the normal scan because IL verification is disabled there.                               |
| `SystemAssemblyName`           | Present in the config model; the checker also has a hard-coded `mscorlib` constant. Inspect current call sites before relying on this value.      |

Each `TypeConfig` has `All`, `Inherit`, `Methods`, `Fields`, and optional `NestedTypes`. `All: true` permits the type's members wholesale. Otherwise, the type may still be accessible while member use is restricted to the listed signatures. `InheritMode` has three values: `Default` allows inheritance only when `All` is true, `Allow` explicitly allows it, and `Block` denies it even when `All` is true. Nested type access is resolved through the enclosing type's configuration; an enclosing `All` also permits nested types wholesale.

Method and field strings are parsed once after deserialization. The parser accepts primitive aliases (`int`, `string`, `bool`, and related CLR primitives), named types, nested type names separated with `/`, generic constructed types, generic parameter placeholders (`!0` and `!!0`), arrays (`[]`), and `ref` method parameter types. Method entries have the form:

```text
<return-type> <method-name>[<generic-arity>](<parameter-types>)
```

For example, the checked-in policy contains `string Combine(string, string)` under `System.IO.Path`. A by-reference parameter is written with the parser's `ref` keyword (for example `void M(ref int)`), not C#'s `&` notation. Field entries have the form `<field-type> <field-name>`. Matching compares name, return/field type, parameter types, and generic parameter count.

### Example: restricting a type to selected members

This is a small illustrative fragment in the shape of `CodeScanList.json`:

```json
{
  "Types": {
    "System.IO": {
      "Path": {
        "Methods": [
          "string Combine(string, string)",
          "string GetFileName(string)"
        ],
        "Fields": [
          "char DirectorySeparatorChar"
        ]
      }
    }
  }
}
```

This makes `System.IO.Path` a recognized type, but doesn't set `All`. Consequently CodeScan checks every member reference on the type against the explicit signature lists. `Path.Combine(string, string)` matches; an overload such as `Path.GetFullPath(string)` does not and produces an `Access to method not allowed` diagnostic. A field reference must match both the field name and field type.

By comparison, the checked-in policy has `"Stream": { "All": true }` under `System.IO`. That grants access to any member references on that type and, by the inheritance default, permits deriving from it. An empty object such as `"SerializationInfo": {}` recognizes the type but grants no members and does not allow inheritance by default.

### Example: nested and generic signatures

Nested CLR types use `/` in signatures, matching the metadata nesting representation. For example, a nested type can be named `Outer/Inner`. A nested type also needs a matching `NestedTypes` policy entry unless its enclosing type is `All`:

```json
{
  "Types": {
    "Example": {
      "Outer": {
        "NestedTypes": {
          "Inner": { "All": true }
        }
      }
    }
  }
}
```

Generic type definitions are named with CLR arity suffixes such as ``List`1``. A constructed generic signature uses angle brackets, and generic placeholders refer to type (`!0`) or method (`!!0`) parameters. For example, the checked-in policy includes signatures such as `System.Span`1<!0> get_Span()` and `void .ctor(!0[])`. The parser treats arrays as suffixes (`[]`), and method generic arity is expressed after the method name in angle brackets. These are metadata-level signatures: use the existing policy entries as examples when adding overloads, especially for nested generics and by-reference types.

### How an access decision is reached

For a metadata type reference, `IsTypeAccessAllowed` follows this order:

1. If the type is nested, resolve its declaring type first. An allowed parent with `All` automatically allows the nested type; otherwise `NestedTypes` must contain the nested name.
2. If its namespace starts with a configured `WhitelistedNamespaces` entry, allow it as `DefaultAll`.
3. Otherwise, if it has an assembly scope that appears in `MultiAssemblyOtherReferences` or `EditorOnlyAssemblies`, allow it as `TypeConfig.DefaultAll`.
4. Otherwise, find an exact namespace key and exact type-name key in `Types`. Missing either means deny.

For each member reference, CodeScan unwraps constructed generic parent types to find their underlying referenced type. If that type's config has `All`, the member is accepted. Otherwise, fields match a configured name and field type; methods match name, return type, generic parameter count, parameter count, and each parameter type. Type equality is structural through `MType.WhitelistEquals`: arrays compare element types, by-reference types compare their referenced type, and constructed generics compare the generic definition and each argument.

This means policy entries are based on references emitted into the consumer assembly, not a reflection enumeration of every public API. The checker decodes `MemberReference` metadata signatures; unsupported member-reference parent forms are explicitly reported as errors. A missing overload is denied even if a similarly named overload is permitted.

### Example: inheritance is a separate permission

Member access and inheritance are separate checks. Given:

```json
"Example": {
  "Base": {
    "All": true,
    "Inherit": "Block"
  }
}
```

references to `Base` and any members are allowed, but a class deriving from `Base` fails `CheckInheritance`. Conversely, `"Inherit": "Allow"` allows inheritance even if `All` is false, while member use still requires explicit method or field entries. `Default` is the default enum value and permits inheritance only when `All` is true.

### Reading a typical error

An error like:

```text
- Access to method not allowed: System.IO.Path.GetFullPath(string) in Assembly Example.Content
```

means the assembly contains a metadata member reference to that exact signature, CodeScan resolved the parent type as accessible, and no configured method entry matched. To fix it, either add the narrowly scoped signature under `Types` → `System.IO` → `Path` or change the code to use an already permitted overload. If the error instead says `Access to type not allowed`, adding only a method signature won't help: the type itself must first be allowed by a namespace, type entry, or assembly rule.

## What each scan checks

### Type references

The scanner walks metadata TypeReference rows and checks each referenced type against namespace, type, nested-type, and assembly rules. Disallowed types produce a diagnostic with a suggested policy path generated by `BuildFixSuggestionStringForTypes`.

### Member references

MemberReference metadata is decoded into field or method references. If the parent type is allowed with `All`, any member is accepted. Otherwise a field must match one configured field signature, or a method must match one configured method signature. Array member references receive special handling and are ignored by this member allowlist check. Generic wrappers are unwrapped to identify the referenced base type.

### Inheritance and interfaces

External base types and implemented interfaces are checked independently from ordinary type/member access. The referenced type must be allowed, and its inheritance setting must permit inheritance (`Allow`, or `Default` with `All`).

### Unsafe metadata patterns

The scanner also checks method definitions for unmanaged method definitions and checks for type abuse patterns. These checks are implemented in `CheckNoUnmanagedMethodDefs` and `CheckNoTypeAbuse`; their findings become sandbox errors. Unsupported metadata encountered while decoding references is also retained as an error rather than silently ignored.

## Assembly resolution

The metadata resolver first searches the player's Managed directory for an assembly whose filename matches the requested assembly name, then searches a `MissingAssemblies` backup directory. The backup path is currently hard-coded in `AssemblyTypeChecker.cs` to a developer-specific `Q:\...` path, while `ThisMain` copies files into a path relative to the executable. These paths do not line up automatically in a new environment; update the resolver path or deployment layout if missing-assembly resolution is required elsewhere. Resolver module resolution is unimplemented and throws `NotImplementedException`.

The current driver passes all other assembly names to the checker, and the checker treats referenced types scoped to those assemblies as allowed. This permits inter-assembly references within the scanned Managed set. The exact content build/package layout therefore affects the policy boundary.

## Outputs and diagnostics

`errors.json` is a JSON array of strings. Entries may describe a disallowed type, member, inheritance relationship, unmanaged method, unsupported metadata, missing resolution dependency, configuration failure, or a failed assembly summary. Progress messages are accumulated separately in `Infos`; some resolver and copy messages also go to standard output. Errors are not guaranteed to be unique because a violation can be reported at multiple stages.

## Maintenance notes

- Treat `CodeScanList.json` as security policy. Changes to namespace prefixes, `All`, or assembly allowances can substantially broaden access.
- Verify which policy revision the remote `develop` URL serves when investigating a scan; the local checkout may not be the version used at runtime.
- The driver has a hard-coded fallback player path, a hard-coded resolver backup path, and several path assumptions. Keep these in mind when running outside the original development setup.
- `VerifyIl` is enabled in the checker constructor but explicitly disabled inside `CheckAssembly` before the `if (VerifyIl)` block. Re-enable it deliberately only after confirming resolver and verifier behavior and the intended allowed-error policy.
- The project targets `net7.0`; build scripts in `HOWTO.txt` request runtime identifiers for three platforms. Keep the target framework, packaged runtime, input JSON location, and player directory layout consistent.
- `ThisMain.Main` is declared `async` but does not await anything. Policy loading is asynchronous in the checker and is synchronously consumed through `_config.Result` during scanning.


### AI Discourse

This .md file was generated by an LLM because nobody in this fucking project wants to properly sit down and put in two braincells to explain how any of this garbage works.

*Narrator: You can hear the programmer rage in this footnote.*