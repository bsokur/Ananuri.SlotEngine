[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'verification-helpers.ps1')

$repository = Split-Path -Parent $PSScriptRoot
Push-Location $repository
try {
    $null = Invoke-DotNet @('restore', 'Ananuri.SlotEngine.sln')
    $null = Invoke-DotNet @('build', 'Ananuri.SlotEngine.sln', '-c', 'Release', '--no-restore')
    $null = Invoke-DotNet @('format', 'Ananuri.SlotEngine.sln', '--verify-no-changes', '--no-restore')
    $null = Invoke-DotNet @('test', 'Ananuri.SlotEngine.sln', '-c', 'Release', '--no-build', '--no-restore')

    $simulator = Join-Path $repository 'tools/Ananuri.SlotEngine.Simulator/bin/Release/net10.0/Ananuri.SlotEngine.Simulator.dll'
    Invoke-CheckedScenario @($simulator, 'demo') @('Stake: 100; payout: 500')
    Invoke-CheckedScenario @($simulator, 'tutorial') $TutorialExpectedLines
    Invoke-CheckedScenario @($simulator, 'enumerate') @(
        'Observations: 64', 'Stake: 6400; payout: 5600', 'RTP: 87.500000%',
        'Payout 0: 54 outcomes', 'Payout 400: 2 outcomes', 'Payout 600: 8 outcomes')

    # Seeded regression expectations supplement independently calculated xUnit fixtures.
    # Intentional sample math changes must update these expectations and the guides together.
    $scenarios = @(
        @{ Name = 'FiveReelGame'; DemoBase = 500; DemoBonus = 0; Evaluations = 1; Draws = 5;
            Base = 135500; Bonus = 0; FreeSpins = 0; Refills = 0; Rtp = '13.550000' },
        @{ Name = 'PaylineGame'; DemoBase = 600; DemoBonus = 0; Evaluations = 1; Draws = 3;
            Base = 862000; Bonus = 0; FreeSpins = 0; Refills = 0; Rtp = '86.200000' },
        @{ Name = 'CascadingGame'; DemoBase = 550; DemoBonus = 75; Evaluations = 11; Draws = 65;
            Base = 378050; Bonus = 547675; FreeSpins = 11175; Refills = 4142; Rtp = '92.572500' },
        @{ Name = 'CollectedSymbolsGame'; DemoBase = 500; DemoBonus = 0; Evaluations = 6; Draws = 21;
            Base = 508500; Bonus = 215500; FreeSpins = 833; Refills = 1488; Rtp = '72.400000' }
    )
    $documentedGameIdentity = Test-GameMathDocumentation $repository
    foreach ($scenario in $scenarios) {
        $package = "samples/$($scenario.Name).json"
        $demoExpected = @(
            "Charged stake: 100; base payout: $($scenario.DemoBase); bonus payout: $($scenario.DemoBonus)",
            "Replay verified: $($scenario.Evaluations) evaluations; $($scenario.Draws) recorded draws.")
        if ($scenario.Name -eq 'CascadingGame') { $demoExpected += $documentedGameIdentity }
        Invoke-CheckedScenario @($simulator, 'game-demo', $package) $demoExpected
        Invoke-CheckedScenario @($simulator, 'game-simulate', '10000', '12345', $package) @(
            "Charged stake: 1000000; base payout: $($scenario.Base); bonus payout: $($scenario.Bonus)",
            "Round RTP: $($scenario.Rtp)%") @(
            "(?m)^Paid rounds: 10000; free spins: $($scenario.FreeSpins);",
            "(?m)^Refills: $($scenario.Refills);")
    }
    foreach ($limit in @('--max-grids-per-round', '--max-spins-per-round')) {
        Invoke-RejectedScenario @($simulator, 'game-demo', 'samples/FirstGame.json', $limit, '1')
    }

    Test-DocumentationLinks $repository
    $null = Invoke-DotNet @('pack', 'src/Ananuri.SlotEngine/Ananuri.SlotEngine.csproj',
        '-c', 'Release', '--no-build', '--no-restore', '-o', 'artifacts')
    Test-PackagedExamples $repository $simulator
    Write-Host 'Verification passed: formatting, tests, sample results, documentation, and NuGet consumer examples.'
}
finally {
    Pop-Location
}
