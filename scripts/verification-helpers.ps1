$TutorialExpectedLines = @(
    'tutorial/0: mode=Paid; charged=100; spin payout=500; round payout=500; remaining free spins=2',
    'tutorial/1: mode=Free; charged=0; spin payout=300; round payout=800; remaining free spins=1',
    'tutorial/2: mode=Free; charged=0; spin payout=600; round payout=1400; remaining free spins=0',
    'Replay verified: 3 evaluations; 18 draws.')

function Invoke-DotNet {
    param([string[]]$Arguments)

    Write-Host "Running: dotnet $($Arguments -join ' ')"
    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & dotnet @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousErrorAction }
    foreach ($line in $output) { Write-Host $line }
    if ($exitCode -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $exitCode."
    }
    return $output -join [Environment]::NewLine
}

function Invoke-CheckedScenario {
    param([string[]]$Arguments, [string[]]$ExpectedLines, [string[]]$ExpectedPatterns = @())

    $output = Invoke-DotNet $Arguments
    $lines = $output -split '\r?\n'
    foreach ($expected in $ExpectedLines) {
        if ($lines -cnotcontains $expected) { throw "Missing expected output: $expected" }
    }
    foreach ($pattern in $ExpectedPatterns) {
        if ($output -cnotmatch $pattern) { throw "Output did not match: $pattern" }
    }
}

function Invoke-RejectedScenario {
    param([string[]]$Arguments)

    Write-Host "Checking expected rejection: dotnet $($Arguments -join ' ')"
    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $lines = & dotnet @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $previousErrorAction }
    foreach ($line in $lines) { Write-Host $line }
    $output = $lines -join [Environment]::NewLine
    if ($exitCode -ne 1 -or $output -notmatch 'the round is unfinished') {
        throw 'Expected simulator budget rejection with exit code 1 and unfinished-round explanation.'
    }
    if ($output -match 'Charged stake:|Round RTP:|Scripted payout/stake ratio') {
        throw 'Simulator published completed-run statistics for an unfinished round.'
    }
}

function Get-MarkdownCodeBlocks {
    param([string]$Content, [string]$Language)

    return @([regex]::Matches($Content, '(?ms)^```' + [regex]::Escape($Language) + '\s*\r?\n(.*?)^```\s*$') |
        ForEach-Object { $_.Groups[1].Value })
}

function Test-DocumentationLinks {
    param([string]$Repository)

    $files = @((Get-Item -LiteralPath (Join-Path $Repository 'README.md'))) +
        @(Get-ChildItem -LiteralPath (Join-Path $Repository 'docs') -Filter '*.md' -File -Recurse)
    $checked = 0
    foreach ($file in $files) {
        $content = [regex]::Replace([IO.File]::ReadAllText($file.FullName), '(?ms)^```.*?^```\s*$', '')
        foreach ($link in [regex]::Matches($content, '!?(?:\[[^\]]*\])\((?<target><[^>]+>|[^\s)]+)(?:\s+[^)]*)?\)')) {
            $target = $link.Groups['target'].Value.Trim('<', '>')
            if ($target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:|^//') { continue }
            $parts = $target -split '#', 2
            $relativePath = [Uri]::UnescapeDataString(($parts[0] -split '\?', 2)[0])
            $resolved = if ($relativePath.Length -eq 0) { $file.FullName }
            else { [IO.Path]::GetFullPath((Join-Path $file.DirectoryName $relativePath)) }
            if (-not (Test-Path -LiteralPath $resolved)) { throw "Broken link in $($file.Name): $target" }
            if ($parts.Count -gt 1 -and [IO.Path]::GetExtension($resolved) -eq '.md') {
                $headingContent = [regex]::Replace([IO.File]::ReadAllText($resolved), '(?ms)^```.*?^```\s*$', '')
                $anchors = @([regex]::Matches($headingContent, '(?m)^#{1,6}\s+(.+?)\s*#*\r?$') | ForEach-Object {
                    # GitHub-style anchors for this repository's ordinary Markdown headings.
                    ($_.Groups[1].Value.ToLowerInvariant() -replace '[^\p{L}\p{Nd}_\-\s]', '') -replace '\s', '-'
                })
                if ($anchors -cnotcontains [Uri]::UnescapeDataString($parts[1])) {
                    throw "Broken heading link in $($file.Name): $target"
                }
            }
            $checked++
        }
    }
    Write-Host "Verified $checked local documentation links, including heading targets."
}

function Test-PackageContents {
    param([IO.Compression.ZipArchive]$Archive, [string]$Repository)

    $entries = @($Archive.Entries | ForEach-Object { $_.FullName })
    $required = @('lib/net10.0/Ananuri.SlotEngine.dll', 'lib/net10.0/Ananuri.SlotEngine.xml',
        'README.md', 'global.json', 'docs/reference-vectors.json', 'samples/TutorialExample.cs')
    $required += @(Get-ChildItem -LiteralPath (Join-Path $Repository 'docs') -Filter '*.md' -File |
        ForEach-Object { 'docs/' + $_.Name })
    $required += @('FiveReelGame', 'PaylineGame', 'FirstGame', 'CascadingGame', 'CollectedSymbolsGame') |
        ForEach-Object { "samples/$_.json" }
    foreach ($path in $required) {
        if ($entries -cnotcontains $path) { throw "NuGet package is missing $path." }
        if ($Archive.GetEntry($path).Length -eq 0) { throw "NuGet package contains empty $path." }
    }
    $obsolete = @($entries | Where-Object { $_ -match '(^|/)history/|(^|/)DemoGames\.cs$' })
    if ($obsolete.Count -ne 0) { throw "NuGet package contains obsolete files: $($obsolete -join ', ')." }
    if (@($entries | Where-Object { $_ -like 'samples/*.json' }).Count -ne 5) {
        throw 'NuGet package must contain exactly the five current JSON samples.'
    }
    Write-Host 'Verified NuGet package contents: library, XML API documentation, current guides and samples.'
}

function Test-GameMathDocumentation {
    param([string]$Repository)

    $game = Get-Content -LiteralPath (Join-Path $Repository 'samples/CascadingGame.json') -Raw | ConvertFrom-Json
    $document = [IO.File]::ReadAllText((Join-Path $Repository 'docs/simulation-and-math.md'))
    $scatter = $game.scatters
    $timing = if ($scatter.PSObject.Properties['timing']) { $scatter.timing } else { 'InitialGrid' }
    $multiply = if ($game.PSObject.Properties['multiplyScatterAwards']) { $game.multiplyScatterAwards } else { $false }
    $refills = if ($game.PSObject.Properties['allowScatterRefills']) { $game.allowScatterRefills } else { $false }
    $arrow = [char]0x2192
    $times = [char]0x00D7
    $paid = ($scatter.paidThresholds | ForEach-Object { "$($_.minimumCount) $arrow $($_.freeSpins) + $($_.totalStakeMultiplier)x" }) -join '; '
    $free = ($scatter.freeThresholds | ForEach-Object { "$($_.minimumCount) $arrow $($_.freeSpins) + $($_.totalStakeMultiplier)x" }) -join '; '
    $rows = @(
        '| Setting | Current value |', '| --- | --- |',
        ('| Game ID | `' + $game.gameId + '` |'),
        ('| Math version | `' + $game.mathVersion + '` |'),
        "| Board | $($game.reels.Count) reels $times $($game.visibleRows) rows |",
        "| Payline rows (left to right) | $(($game.paylines | ForEach-Object { $_.rows -join '/' }) -join '; ') |",
        "| Allowed stakes | $($game.allowedStakes -join ', ') |",
        "| Wild symbols / substitution targets | $($game.wilds.symbols -join ', ') / $($game.wilds.substitutesFor -join ', ') |")
    foreach ($symbol in ($game.paytable.symbol | Sort-Object -Unique)) {
        $awards = @($game.paytable | Where-Object symbol -eq $symbol | Sort-Object matchCount)
        $rows += "| Symbol ${symbol}: $($awards.matchCount -join '/') matches, line-stake multiples | $($awards.multiplier -join '/') |"
    }
    $bonus = $game.bonusMultiplier
    $rows += @(
        "| Scatter symbol / timing | $($scatter.symbol) / $timing |",
        "| Paid scatters: minimum count $arrow spins + total-stake cash | $paid |",
        "| Free scatters: minimum count $arrow spins + total-stake cash | $free |",
        "| Multiply scatter cash | $multiply |",
        "| Allow scatter refills | $refills |",
        "| Maximum lifetime awarded free spins | $($game.maximumAwardedFreeSpins) |",
        "| Paid multiplier | $($game.paidMultiplier.strategy): $($game.paidMultiplier.start)x |",
        "| Bonus multiplier | $($bonus.strategy): start $($bonus.start)x, increment $($bonus.increment), maximum $($bonus.maximum)x, persistence $($bonus.persistence), include initial grid $($bonus.includeInitialGrid) |",
        "| Gross round cap, total-stake multiple | $($game.roundWinLimitMultiplier)x |")
    $table = [regex]::Match($document, '(?s)<!-- cascading-rules:start -->\s*(.*?)\s*<!-- cascading-rules:end -->')
    if (-not $table.Success -or ($table.Groups[1].Value -replace '\r\n', "`n") -cne ($rows -join "`n")) {
        throw "The documented CascadingGame table is stale. Expected:`n$($rows -join "`n")"
    }
    $identity = [regex]::Matches($document, '(?m)^`([A-F0-9]{64})`\r?$')
    if ($identity.Count -ne 1) { throw 'Expected one calibration fingerprint in simulation-and-math.md.' }
    Write-Host 'Verified current game settings against the documentation table.'
    return "Game: $($game.gameId); math: $($game.mathVersion); fingerprint: $($identity[0].Groups[1].Value)"
}

function Get-EngineRuleCompilationProgram {
    param([string]$Repository)

    $document = [IO.File]::ReadAllText((Join-Path $Repository 'docs/engine-rules.md'))
    $fragments = @(Get-MarkdownCodeBlocks $document 'csharp')
    if ($fragments.Count -ne 2) { throw 'Expected two host fragments in engine-rules.md.' }
    $imports = [regex]::Matches($fragments[0], '(?m)^using [^;]+;\r?$').Value -join "`n"
    $load = [regex]::Replace($fragments[0], '(?m)^using [^;]+;\r?\n', '')
    return @"
$imports

internal static class DocumentedFragments
{
    static void Main() { }
    static void Load(IRandomDrawSource runtimeDrawSource)
    {
$load
    }
    static void Resume(ISlotEngine engine, Ananuri.SlotEngine.Definitions.GameDefinition game,
        SpinRequest request, IRandomDrawSource draws)
    {
$($fragments[1])
    }
}
"@
}

function Copy-PackageText {
    param([IO.Compression.ZipArchive]$Archive, [string]$Entry, [string]$Destination)

    $reader = [IO.StreamReader]::new($Archive.GetEntry($Entry).Open())
    try { [IO.File]::WriteAllText($Destination, $reader.ReadToEnd()) }
    finally { $reader.Dispose() }
}

function Test-PackagedExamples {
    param([string]$Repository, [string]$Simulator)

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $packagePath = Join-Path $Repository 'artifacts/Ananuri.SlotEngine.0.1.0.nupkg'
    $archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    $work = [IO.Path]::GetFullPath((Join-Path $temporaryRoot ('Ananuri.SlotEngine-verification-' + [Guid]::NewGuid().ToString('N'))))
    try {
        Test-PackageContents $archive $Repository
        $null = New-Item -ItemType Directory -Path $work
        Copy-PackageText $archive 'global.json' (Join-Path $work 'global.json')
        $feed = [Security.SecurityElement]::Escape((Join-Path $Repository 'artifacts'))
        $configPath = Join-Path $work 'NuGet.Config'
        [IO.File]::WriteAllText($configPath, @"
<configuration>
  <packageSources><clear /><add key="verified-package" value="$feed" /></packageSources>
</configuration>
"@)
        $integration = [IO.File]::ReadAllText((Join-Path $Repository 'docs/integration.md'))
        $programs = @(Get-MarkdownCodeBlocks $integration 'csharp')
        if ($programs.Count -ne 2) { throw 'Expected both complete C# programs in integration.md.' }
        $programs += Get-EngineRuleCompilationProgram $Repository
        for ($index = 0; $index -lt $programs.Count; $index++) {
            $hostPath = Join-Path $work "Host$index"
            $null = New-Item -ItemType Directory -Path $hostPath
            $project = Join-Path $hostPath 'Host.csproj'
            [IO.File]::WriteAllText($project, @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors><NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="Ananuri.SlotEngine" Version="0.1.0" /></ItemGroup>
</Project>
'@)
            [IO.File]::WriteAllText((Join-Path $hostPath 'Program.cs'), $programs[$index])
            $runArguments = @('run', '--project', $project, '-c', 'Release', '--no-restore')
            if ($index -eq 0) {
                Copy-PackageText $archive 'samples/TutorialExample.cs' (Join-Path $hostPath 'TutorialExample.cs')
                $samplePath = Join-Path $hostPath 'FirstGame.json'
                Copy-PackageText $archive 'samples/FirstGame.json' $samplePath
                $runArguments += @('--', $samplePath)
            }
            # An isolated cache forces NuGet to consume the package just produced, even
            # while this initial release keeps the same 0.1.0 version during development.
            $null = Invoke-DotNet @('restore', $project, '--configfile', $configPath, '--packages', (Join-Path $work 'packages'))
            $expected = if ($index -eq 0) { $TutorialExpectedLines } elseif ($index -eq 1) { @('Payout: 500') } else { @() }
            Invoke-CheckedScenario $runArguments $expected
        }

        $reference = [IO.File]::ReadAllText((Join-Path $Repository 'docs/configuration-reference.md'))
        $completePackages = @(Get-MarkdownCodeBlocks $reference 'json' | Where-Object { $_ -match '"schemaVersion"' })
        if ($completePackages.Count -ne 1) { throw 'Expected one complete JSON package in configuration-reference.md.' }
        $examplePath = Join-Path $work 'DocumentedGame.json'
        [IO.File]::WriteAllText($examplePath, $completePackages[0])
        Invoke-CheckedScenario @($Simulator, 'game-demo', $examplePath) @(
            'Charged stake: 100; base payout: 500; bonus payout: 0',
            'Replay verified: 1 evaluations; 3 recorded draws.')

        $tutorial = [IO.File]::ReadAllText((Join-Path $Repository 'docs/create-a-game.md'))
        $commands = @(Get-MarkdownCodeBlocks $tutorial 'powershell')
        if ($commands.Count -ne 3 -or $tutorial -match 'samples/MyGame\.json') {
            throw 'Expected three tutorial command blocks with experiments outside the shipped samples.'
        }
        $null = New-Item -ItemType Directory -Path (Join-Path $work 'samples')
        Copy-PackageText $archive 'samples/FirstGame.json' (Join-Path $work 'samples/FirstGame.json')
        Push-Location $work
        try { & ([scriptblock]::Create($commands[0])) }
        finally { Pop-Location }
        $experimentPath = Join-Path $work 'experiments/MyGame.json'
        $experiment = Get-Content -LiteralPath $experimentPath -Raw | ConvertFrom-Json
        $experiment.gameId = 'my-first-game'
        $experiment.mathVersion = 'experiment-1'
        ($experiment.paytable | Where-Object symbol -eq 1).multiplier = 6
        $experiment | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $experimentPath
        Invoke-CheckedScenario @($Simulator, 'tutorial', $experimentPath) @(
            'tutorial/0: mode=Paid; charged=100; spin payout=600; round payout=600; remaining free spins=2',
            'tutorial/1: mode=Free; charged=0; spin payout=300; round payout=900; remaining free spins=1',
            'tutorial/2: mode=Free; charged=0; spin payout=600; round payout=1500; remaining free spins=0',
            'Replay verified: 3 evaluations; 18 draws.')
        if (@(Get-ChildItem -LiteralPath (Join-Path $work 'samples') -Filter '*.json').Count -ne 1) {
            throw 'The tutorial added an experiment to the shipped samples directory.'
        }

        # Exercise default sample lookup independently of the source checkout's cwd.
        Push-Location $work
        try {
            Invoke-CheckedScenario @($Simulator, 'demo') @('Stake: 100; payout: 500')
            Invoke-CheckedScenario @($Simulator, 'tutorial') $TutorialExpectedLines
            Invoke-CheckedScenario @($Simulator, 'enumerate') @('Observations: 64', 'Stake: 6400; payout: 5600')
            Invoke-CheckedScenario @($Simulator, 'game-demo') @(
                'Charged stake: 100; base payout: 550; bonus payout: 75',
                'Replay verified: 11 evaluations; 65 recorded draws.')
        }
        finally { Pop-Location }
    }
    finally {
        $archive.Dispose()
        $resolvedWork = [IO.Path]::GetFullPath($work)
        $temporaryPrefix = $temporaryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        if (-not $resolvedWork.StartsWith($temporaryPrefix, [StringComparison]::OrdinalIgnoreCase) -or
            [IO.Path]::GetFileName($resolvedWork) -notmatch '^Ananuri\.SlotEngine-verification-[a-f0-9]{32}$') {
            throw "Refusing to clean unexpected verification directory: $resolvedWork"
        }
        if (Test-Path -LiteralPath $resolvedWork) { Remove-Item -LiteralPath $resolvedWork -Recurse -Force }
    }
}

