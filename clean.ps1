# Get all obj and bin directories
$foldersToDelete = Get-ChildItem -Path . -Include bin,obj -Recurse -Directory

# Delete each folder
foreach ($folder in $foldersToDelete) {
    try {
        Remove-Item -Path $folder.FullName -Recurse -Force
        Write-Host "Deleted: $($folder.FullName)" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to delete: $($folder.FullName)" -ForegroundColor Red
        Write-Host $_.Exception.Message
    }
}

Write-Host "`nCleanup complete!" -ForegroundColor Cyan