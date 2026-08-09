# Builds the itch.io upload zip.
#
# PowerShell's Compress-Archive writes Windows path separators into the zip entry
# names. The ZIP spec requires forward slashes, and itch.io unzips on Linux, so a
# backslash there becomes part of the filename rather than a directory separator:
# you end up with one root-level file called "Build\WebGL.loader.js" and every
# request for Build/WebGL.loader.js returns 404. Build the entries by hand instead.

Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression

$src = 'C:\Users\olive\Documents\Unity Projects & Builds\TSA\Builds\WebGL'
$zip = 'C:\Users\olive\Documents\Unity Projects & Builds\TSA\Builds\CyberStation-WebGL-itch.zip'

if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }

$sep = [char]92        # backslash
$fwd = [char]47        # forward slash
$prefixLength = $src.Length + 1

$archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $src -Recurse -File) {
        $relative = $file.FullName.Substring($prefixLength)
        $entryName = $relative.Replace($sep, $fwd)
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive, $file.FullName, $entryName,
            [System.IO.Compression.CompressionLevel]::Optimal)
    }
}
finally {
    $archive.Dispose()
}

Write-Output ("Rebuilt: {0}" -f $zip)
Write-Output ("Size: {0:N1} MB" -f ((Get-Item -LiteralPath $zip).Length / 1MB))
Write-Output ''
Write-Output '=== Entry names ==='

$read = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    foreach ($entry in $read.Entries) {
        $flag = if ($entry.FullName.Contains($sep)) { 'BACKSLASH - BAD' } else { 'ok' }
        Write-Output ("  [{0}] {1}" -f $flag, $entry.FullName)
    }
}
finally {
    $read.Dispose()
}
