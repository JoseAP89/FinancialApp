<#
UpdateDB.ps1

PowerShell helper to push or merge a SQLite database to an Android emulator/device using adb.

Requirements:
- adb on PATH (Android platform-tools)
- App must be debuggable (run-as works)
- For the "update" action: sqlite3 CLI on the dev machine

Usage examples:
  # Replace device DB with a local DB file
  .\UpdateDB.ps1 -Action replace -LocalDbPath "C:\path\to\replacement.db" -Package "com.companyname.financialapp"

  # Merge Accounts table from a packaged DB into the installed DB
  .\UpdateDB.ps1 -Action update -LocalDbPath "C:\path\to\packaged.db" -Package "com.companyname.financialapp"
#>
param(
	[string]$Package = "com.companyname.financialapp",
	[ValidateSet("replace","update")] [string]$Action = "update",
	[string]$LocalDbPath = "",
	[string]$DbName = "PersonalFinanceDB.db",
	[string]$AdbPath = "adb",
	[string]$SqlitePath = "sqlite3"
)

function Run-AdbRedirect {
	param([string[]]$Args, [string]$OutFile)
	$psi = New-Object System.Diagnostics.ProcessStartInfo
	$psi.FileName = $AdbPath
	$psi.Arguments = $Args -join ' '
	$psi.RedirectStandardOutput = $true
	$psi.UseShellExecute = $false
	$psi.CreateNoWindow = $true

	$p = [System.Diagnostics.Process]::Start($psi)
	$fs = [System.IO.File]::Open($OutFile, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
	try {
		$buffer = New-Object byte[] 8192
		while (($read = $p.StandardOutput.BaseStream.Read($buffer, 0, $buffer.Length)) -gt 0) {
			$fs.Write($buffer, 0, $read)
		}
	} finally {
		$fs.Close()
		$p.WaitForExit()
	}
	if ($p.ExitCode -ne 0) { throw "adb process failed with exit code $($p.ExitCode)" }
}

function Run-Adb {
	param([string[]]$Args)
	$rc = & $AdbPath @Args
	if ($LASTEXITCODE -ne 0) { throw "adb failed: $($Args -join ' ')" }
	return $rc
}

# Verify adb exists
if (-not (Get-Command $AdbPath -ErrorAction SilentlyContinue)) {
	Write-Error "adb not found (ensure Android platform-tools are installed and adb is on PATH)."
	exit 2
}

if ($Action -eq 'replace' -and -not (Test-Path $LocalDbPath)) {
	Write-Error "Local DB file required for replace action: supply -LocalDbPath path/to/db"
	exit 2
}
if ($Action -eq 'update' -and -not (Test-Path $LocalDbPath)) {
	Write-Error "Packaged DB file required for update action: supply -LocalDbPath path/to/packaged.db"
	exit 2
}

$timestamp = (Get-Date).ToString('yyyyMMddHHmmss')
$tempDir = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "dbsync_$timestamp")
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
	$remoteRel = "files/$DbName"
	$localPulled = [System.IO.Path]::Combine($tempDir, "installed_$DbName")
	Write-Host "Pulling installed DB from device..."

	# adb exec-out run-as <pkg> cat files/<db>
	Run-AdbRedirect @('exec-out','run-as',$Package,'cat',$remoteRel) $localPulled
	Write-Host "Pulled to $localPulled"

	if ($Action -eq 'replace') {
		Write-Host "Replacing DB on device with local file $LocalDbPath..."
		$tmpName = "push_$($timestamp)_$([System.IO.Path]::GetFileName($LocalDbPath))"
		$remoteTmp = "/data/local/tmp/$tmpName"
		Run-Adb @('push',$LocalDbPath,$remoteTmp)
		# move into app files using run-as
		Run-Adb @('shell','run-as',$Package,'cp',$remoteTmp,$remoteRel)
		Run-Adb @('shell','run-as',$Package,'chmod','600',$remoteRel)
		Run-Adb @('shell','rm',$remoteTmp)
		Write-Host "Replace complete."
		exit 0
	}

	if ($Action -eq 'update') {
		Write-Host "Merging Accounts from packaged DB ($LocalDbPath) into pulled DB..."
		if (-not (Get-Command $SqlitePath -ErrorAction SilentlyContinue)) {
			Write-Error "sqlite3 CLI not found. Install sqlite3 or add it to PATH."
			exit 2
		}

		# Compose SQL and run attach/insert locally
		$sql = "ATTACH '$LocalDbPath' AS src; INSERT OR IGNORE INTO Accounts (Id, Name, Balance) SELECT Id, Name, Balance FROM src.Accounts; DETACH src;"
		& $SqlitePath $localPulled $sql
		if ($LASTEXITCODE -ne 0) { throw "sqlite3 command failed." }

		# Push modified DB back
		$tmpName = "push_$($timestamp)_$DbName"
		$remoteTmp = "/data/local/tmp/$tmpName"
		Run-Adb @('push',$localPulled,$remoteTmp)
		Run-Adb @('shell','run-as',$Package,'cp',$remoteTmp,$remoteRel)
		Run-Adb @('shell','run-as',$Package,'chmod','600',$remoteRel)
		Run-Adb @('shell','rm',$remoteTmp)
		Write-Host "Update complete."
		exit 0
	}

} catch {
	Write-Error $_.Exception.Message
	exit 3
} finally {
	Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
}

Write-Host "Done."
