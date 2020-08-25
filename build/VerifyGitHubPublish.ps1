# Check for string in the logs in the current DevOps pipeline run.
# Note: The task immediately before this one may not get checked because its log may not yet be available.
# Calls the Azure DevOps REST API.
# Enable OAuth token access in the pipeline agent job for $(System.Accesstoken) to populate.
$stringToCheckFor = '201 Created';
Start-Sleep -Milliseconds 1000 # Give time for the last log to become available

$collectionUri = "$env:SYSTEM_COLLECTIONURI";  # e.g. 'https://fuselabs.visualstudio.com'
$teamProjectId = "$env:SYSTEM_TEAMPROJECTID";  # e.g. '86659c66-c9df-418a-a371-7de7aed35064' = SDK_v4

# Get the current build ID.
$buildId = "$env:BUILD_BUILDID";
Write-Host 'Build ID = ' $buildId;

# Get the log containers for the run.
$uri = "$collectionUri/$teamProjectId/_apis/build/builds/$buildId/logs";

$token = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes(":$(System.AccessToken)"));
$header = @{authorization = "Basic $token"};

$runLogContainers = Invoke-RestMethod "$uri" -Method Get -ContentType "application/json" -Headers $header;

# Get the log from each log container.
Write-Host 'Checking the logs:';
$found = $false;
foreach ($container in $runLogContainers.value) {
    $container.id;
    $uri = $container.url;
    $uri;
    $log = Invoke-RestMethod "$uri" -Method Get -ContentType "application/json" -Headers $header;
    
    # Search for our string.
    if (!$found -and $log.Contains($stringToCheckFor)) {
        $found = $true;
        $log;
        $mess = 'String "' + $stringToCheckFor + '" found in log #' + $container.id;
        Write-Host $mess;
    } else {
        ($log -split '\r?\n')[0] + '...';  # Print first line
    }
}

# If not found, throw an error.
if (!$found) {
    Write-Host;
    $mess =  'Publish Compat Results failed. Is there a PR associated with this build? String "' + $stringToCheckFor + '" not found in the logs';
    throw $mess;
}
