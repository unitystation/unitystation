param(
	[ValidateSet("Debug", "Release")]
	[string] $Configuration = "Debug",
	[string] $FilesToMoveToManagedPath
)

$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDirectory = Join-Path $scriptDirectory "CodeScan"
$projectPath = Join-Path $projectDirectory "CodeScan.csproj"
$targetFramework = "net7.0"
$outputDirectory = Join-Path $projectDirectory "bin\$Configuration\$targetFramework"
$runtimeIdentifiers = @("win-x64", "linux-x64", "osx-x64")

if (-not $FilesToMoveToManagedPath) {
	$candidates = @(
		(Join-Path $outputDirectory "FilesToMoveToManaged.json"),
		(Join-Path (Join-Path $projectDirectory "bin\Debug\$targetFramework") "FilesToMoveToManaged.json"),
		(Join-Path $projectDirectory "FilesToMoveToManaged.json"),
		(Join-Path $scriptDirectory "FilesToMoveToManaged.json")
	)
	$FilesToMoveToManagedPath = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}

if (-not $FilesToMoveToManagedPath -or -not (Test-Path -LiteralPath $FilesToMoveToManagedPath -PathType Leaf)) {
	throw "FilesToMoveToManaged.json was not found. Pass -FilesToMoveToManagedPath or place it in the project/bin output directory."
}

$FilesToMoveToManagedPath = (Resolve-Path -LiteralPath $FilesToMoveToManagedPath).Path
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

foreach ($runtimeIdentifier in $runtimeIdentifiers) {
	$stagingDirectory = Join-Path $outputDirectory (".package-{0}-{1}" -f $runtimeIdentifier, [guid]::NewGuid())
	$archiveRoot = Join-Path $stagingDirectory "archive"
	$publishedDirectory = Join-Path $archiveRoot $runtimeIdentifier
	$stagedZip = Join-Path $outputDirectory (".{0}-{1}.zip" -f $runtimeIdentifier, [guid]::NewGuid())
	$destinationZip = Join-Path $outputDirectory "$runtimeIdentifier.zip"

	try {
		New-Item -ItemType Directory -Path $publishedDirectory -Force | Out-Null
		New-Item -ItemType Directory -Path $archiveRoot -Force | Out-Null

		Write-Host "Publishing $runtimeIdentifier ($Configuration)..."
		& dotnet publish $projectPath `
			-c $Configuration `
			-f $targetFramework `
			-r $runtimeIdentifier `
			--self-contained true `
			-o $publishedDirectory
		if ($LASTEXITCODE -ne 0) {
			throw "dotnet publish failed for $runtimeIdentifier with exit code $LASTEXITCODE"
		}

		Copy-Item -LiteralPath $FilesToMoveToManagedPath -Destination (Join-Path $archiveRoot "FilesToMoveToManaged.json")
		Compress-Archive -Path (Join-Path $archiveRoot "*") -DestinationPath $stagedZip -CompressionLevel Optimal
		Move-Item -LiteralPath $stagedZip -Destination $destinationZip -Force

		Write-Host "Updated $destinationZip"
	}
	finally {
		if (Test-Path -LiteralPath $stagedZip) {
			Remove-Item -LiteralPath $stagedZip -Force
		}
		if (Test-Path -LiteralPath $stagingDirectory) {
			$resolvedStagingDirectory = (Resolve-Path -LiteralPath $stagingDirectory).Path
			$resolvedOutputDirectory = (Resolve-Path -LiteralPath $outputDirectory).Path
			if (-not $resolvedStagingDirectory.StartsWith($resolvedOutputDirectory, [StringComparison]::OrdinalIgnoreCase)) {
				throw "Refusing to remove staging directory outside the package output: $resolvedStagingDirectory"
			}
			Remove-Item -LiteralPath $resolvedStagingDirectory -Recurse -Force
		}
	}
}

Write-Host "All runtime archives are up to date in $outputDirectory"
