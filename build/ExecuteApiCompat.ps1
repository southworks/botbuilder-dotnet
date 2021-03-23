using namespace System.IO.Compression

param
( 
    [string]$Path,
    [Parameter(Mandatory=$True)]
    [string]$Name,
    [string]$Version
)

if (![string]::IsNullOrEmpty($Path)) {
    Set-Location -Path $Path
    $Path = $Path.TrimEnd('\')
} else {
    $Path = Get-Location
}

$ApiCompatPath = "$Path\ApiCompat"
$ZipFile = "ApiCompat.zip"
$ZipPath = "$ApiCompatPath\$ZipFile"
$InstallResult = $false
$ApiCompatDownloadRequestUri = 'https://pkgs.dev.azure.com/dnceng/public/_apis/packaging/feeds/dotnet-eng/nuget/packages/Microsoft.DotNet.ApiCompat/versions/6.0.0-beta.21168.3/content?api-version=6.0-preview.1'

$DownloadLatestPackageVersion = {    
    # Get latest version suffix
    $DllData = nuget search $DllName -PreRelease
    $LatestVersion = [regex]::match($DllData,"(?<=$DllName \| ).*?(?=\s)").Value

    Write-Host ">> Attempting to install latest version $LatestVersion" -ForegroundColor cyan
    
    # Store command into a variable to handle error output from nuget
    $NugetInstallCommand = 'nuget install $DllName -OutputDirectory "$Path\ApiCompat\Contracts" -Version $LatestVersion'
    
    # Run command and store outputs into variables
    Invoke-Expression $NugetInstallCommand -ErrorVariable InstallCommandError -OutVariable InstallCommandOutput 2>&1 >$null
    
    $script:InstallResult = ($InstallCommandOutput -match 'Added package' -or $InstallCommandOutput -match 'already installed')
    if ($InstallResult) {
        Write-Host ">> Successfully installed latest version: $LatestVersion" -ForegroundColor green        
        
        # Update version with latest suffix
        $script:Version = $LatestVersion
    }
}

$DownloadFixedPackageVersions = {
    # Remove version sufix if any
    $script:LocalVersion = $Version
    $script:Version = $Version -replace '-local'
    
    Write-Host ">> Attempting to download GA specific version = $Version" -ForegroundColor cyan
    
    # Install corresponding nuget package to "Contracts" folder    
    # Store command into a variable to handle error output from nuget
    $NugetInstallCommand = 'nuget install $DllName -Version $Version -OutputDirectory "$Path\ApiCompat\Contracts" -Verbosity detailed'
    
    # Run command and store outputs into variables
    Invoke-Expression $NugetInstallCommand -ErrorVariable InstallCommandError -OutVariable InstallCommandOutput 2>&1 >$null
    
    # Check package existance by searching on the output strings that would match only if the package is installed
    $script:InstallResult = ($InstallCommandOutput -match 'Added package' -or $InstallCommandOutput -match 'already installed')
    
    # If GA version doesn't exist, attempt to download specific preview version
    if(!$InstallResult) {
        Write-Host ">> Failed installing GA specific version: $Version. Trying different approach..." -ForegroundColor red
        Write-Host ">> Attempting to install specific preview version = $Version-preview" -ForegroundColor cyan
        
        # Store command into a variable to handle error output from nuget
        $NugetInstallCommand = 'nuget install $DllName -Version "$Version-preview" -OutputDirectory "$Path\ApiCompat\Contracts" -Verbosity detailed'
        
        # Run command and store outputs into variables
        Invoke-Expression $NugetInstallCommand -ErrorVariable InstallCommandError -OutVariable InstallCommandOutput 2>&1 >$null
        $script:InstallResult = ($InstallCommandOutput -match 'Added package' -or $InstallCommandOutput -match 'already installed')
        
        # If specific preview version doesn't exist, attempt to download latest version (including preview)
        if ($InstallResult) {
            Write-Host ">> successfully installed preview version: $Version-preview" -ForegroundColor green
            
            # If previous install is successful, we append -preview to version.
            $script:Version = "$Version-preview"
        } else {
            # If specific versions failed, download latest
            Write-Host ">> Failed installing specific preview version: $Version-preview. Trying different approach..." -ForegroundColor red
            &$DownloadLatestPackageVersion
        }
    } else {
        Write-Host ">> Success" -ForegroundColor green
    }
}

$DownloadApiCompat = {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    cd $ApiCompatPath
    
    # Get Zipfile for ApiCompat
    $DestinationPath= "$Path\ApiCompat"
    $TargetEntry = "netcoreapp3.1"
    
    # If file doesn't exist, download it
    if (!(Test-Path "$ApiCompatPath\ApiCompat.zip" -PathType Leaf)) {
        try { 
            # Download
            Write-Host "Downloading zip"
            
            try {
                # TODO: Maybe we could add some retries here
                (New-Object System.Net.WebClient).DownloadFile($ApiCompatDownloadRequestUri, $ZipPath)
                #Invoke-RestMethod -Method "GET" -Uri $ApiCompatDownloadRequestUri -OutFile ".\$ZipFile"
            } catch { 
                Write-Error "Download attempt failed"
                Write-Error $_
                exit 1 
            }
            
            $Zip = [ZipFile]::OpenRead($ZipPath)
            
            # Extract files
            $Zip.Entries.Where{ $_.FullName -match "$TargetEntry.*[^/]$" }.ForEach{
                $NewFile = [IO.FileInfo]($DestinationPath,$_.FullName -join "/")
                $NewFile.Directory.Create()
                [ZipFileExtensions]::ExtractToFile($_, $NewFile)
            }
            $Zip.Dispose()

            Write-Host "$ZipFile successfully downloaded and extracted" -ForegroundColor green
        } finally {
            # Remove downloaded zip file.
            Remove-Item $ZipFile
        }
    }

    cd $Path
}

$WriteToLog = {
    $ResultMessage = "$DllName LOCAL: $LocalVersion | UPSTREAM: $Version => $ApiCompatResult"
    Write-Host $ResultMessage -ForegroundColor green
    
    # Create a Mutex for all process to be able to share same log file
    $mutexName = "LogFileMutex" #'A unique name shared/used across all processes that need to write to the log file.'
    $mutex = New-Object 'Threading.Mutex' $false, $mutexName
    
    #Grab the mutex. Will block until this process has it.
    $mutex.WaitOne() | Out-Null;
    
    try
    {
        # Now it is safe to write to log file
        Add-Content $OutputDirectory $ResultMessage
    } finally {
        $mutex.ReleaseMutex()
    }
}

# Get specific dll file from built solution
$Dll = Get-ChildItem "$Path\**\$Name\bin\Debug\**\**" -Filter "$Name.dll" | % { $_.FullName }
if ([string]::IsNullOrEmpty($Dll)){
    $Dll = Get-ChildItem "$Path\**\**\$Name\bin\Debug\**\**" -Filter "$Name.dll" | % { $_.FullName }
}

# Prepare copy statement to move dll to "Implementations" folder for comparing later
$DllDestination = if (Test-Path "$ApiCompatPath\Implementations" -PathType Container) { "$ApiCompatPath\Implementations" } else { New-item -Name "Implementations" -Type "directory" -Path $ApiCompatPath }
$CopyLocalDllToDestination = 'Copy-Item $Dll -Destination $DllDestination'
$DllName = [IO.Path]::GetFileNameWithoutExtension($Dll)

if ([string]::IsNullOrEmpty($Version)) {
    &$DownloadLatestPackageVersion
} else {
    &$DownloadFixedPackageVersions
}

# No reason to continue if package could not be installed
if (!$InstallResult) {
    Write-Error "Failed to download package $DllName with version $Version`n"
    exit 0 # TODO: Should be exit != 0?
}

# Package to compare to has been downloaded, proceed to copy local version for comparisson
try { 
    Invoke-Expression $CopyLocalDllToDestination 
} catch { 
    Write-Error ">> Local dll was not found. Try building your project or solution."
    exit 3
}

Write-Host ">> Dll '$Name' successfully copied to $ApiCompatPath\Implementations`n" -ForegroundColor cyan

# Get specific dll file from nuget package
$PackageName = "$DllName.$Version"
$Package = Get-ChildItem "$ApiCompatPath\Contracts\$PackageName\lib\**\*.dll" -Filter "*.dll" -Recurse
$PackageDestination = if (Test-Path "$ApiCompatPath\Contracts\NugetDlls" -PathType Container) { "$ApiCompatPath\Contracts\NugetDlls" } else { New-item -Name "NugetDlls" -Type "directory" -Path $ApiCompatPath\Contracts }
Copy-Item $Package -Destination $PackageDestination


# TODO: Move all these mutex to a function?

# Download ApiCompat
# Create a Mutex to prevent race conditions while downloading apiCompat.zip
# Important notice: All threads should wait for download and extraction to finish before moving past this point.
$mutexName = "DownloadApiCompatMutex" # A unique name shared/used across all processes.
$mutex = New-Object 'Threading.Mutex' $false, $mutexName

# Grab the mutex. Will block until this process has it.
# All processes will wait for the mutex to be freed to prevent cases were 'tools' folder exists but has not finished extracting.
# If download and extraction of ApiCompat.zip is done, the other processes will exit by the if condition and wait a minimum amount of time.
$mutex.WaitOne() | Out-Null;
try {
    # Clean possible orphan files from aborted previous run.
    if (Test-Path $ZipPath -PathType Leaf) {
        Remove-Item $ZipPath
    }

    if (!(Test-Path "$ApiCompatPath\tools")) {
        &$DownloadApiCompat
    }
} finally {
    $mutex.ReleaseMutex()
}

# Run ApiCompat
$ApiCompatResult = (.\ApiCompat\tools\netcoreapp3.1\Microsoft.DotNet.ApiCompat.exe "$ApiCompatPath\Contracts\NugetDlls\$DllName.dll" --impl-dirs "$ApiCompatPath\Implementations\$DllName.dll") -replace 'TypesMustExist', "`nTypesMustExist"
$OutputDirectory = if (Test-Path "$ApiCompatPath\ApiCompatResult.txt") { "$ApiCompatPath\ApiCompatResult.txt" } else { New-item -Name "ApiCompatResult.txt" -Type "file" -Path $ApiCompatPath }
Write-Host ">> Saving ApiCompat output to $OutputDirectory`n" -ForegroundColor cyan

# Add result to txt file for better accessibility
&$WriteToLog