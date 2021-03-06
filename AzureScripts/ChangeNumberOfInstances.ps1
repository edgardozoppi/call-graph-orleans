###
### THIS SCRIPT REQUIRES POWERSHELL 64bits
### Even tough it looks weird the path for the 64bits version is C:\Windows\System32\WindowsPowerShell 
### or %SystemRoot%\WindowsPowerShell\v1.0\powershell.exe
##
## This script is used to change the number of Worker Roles
## You need to be logged to azure in order to run this script
## Al alternative option, that allows automatic connection, is to provide a certificate 
##

param (
    [int] $InstanceCount = 1
	)


workflow Update-CloudServiceScale
{
    param(
       
                [parameter(Mandatory=$true)]
                [String]$SubscriptionId,

                [parameter(Mandatory=$true)]
                [String]$SubscriptionName,
	
				[parameter(Mandatory=$true)]
				[String]$PfxFilePath, 

#                [parameter(Mandatory=$true)]
#                [String]$PfxPassword,   
		
                [parameter(Mandatory=$true)]
                [String]$Thumbprint,    

                # cloud service name for scale up/down
                [Parameter(Mandatory = $true)] 
                [String]$ServiceName,

                [Parameter(Mandatory = $true)]
                [String]$Slot,

                [Parameter(Mandatory = $true)]
                [String]$InstanceCount
    )

    # Check if Windows Azure Powershell is avaiable
    if ((Get-Module -ListAvailable Azure) -eq $null)
    {
        throw "Windows Azure Powershell not found! Please install from http://www.windowsazure.com/en-us/downloads/#cmd-line-tools"
    }

    $Start = [System.DateTime]::Now
    "Starting: " + $Start.ToString("HH:mm:ss.ffffzzz")

	<# Add this if you have a certificate#>
    #$SecurePwd = ConvertTo-SecureString -String "$PfxPassword" -Force -AsPlainText
    $importedCert = Import-PfxCertificate -FilePath $PfxFilePath  -CertStoreLocation Cert:\CurrentUser\My  -Exportable  
	#-Password $SecurePwd 
    #$MyCert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList($PfxFilePath, $SecurePwd, "Exportable")

	$Thumbprint = $importedCert.Thumbprint

	$MyCert = Get-Item cert:\\CurrentUser\My\$Thumbprint

	#$MyCert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 
	<##>

    inlinescript
    {
       Set-AzureSubscription -SubscriptionName "$using:SubscriptionName" -SubscriptionId $using:SubscriptionId -Certificate $using:MyCert 
       Select-Azuresubscription -SubscriptionName "$using:SubscriptionName" 

       $Deployment = Get-AzureDeployment -Slot $using:Slot -ServiceName $using:ServiceName
       if ($Deployment -ne $null -AND $Deployment.DeploymentId  -ne $null)
       {
           $Roles = Get-AzureRole -ServiceName $using:ServiceName -Slot $using:Slot

           foreach ($Role in $Roles)
           {
			   if($Role.RoleName -eq "OrleansSilosInAzure")
			   {
					$RoleDetails = Get-AzureRole -ServiceName $using:ServiceName -Slot $using:Slot -RoleName $Role.RoleName
					Write-Output (" {0} current " -f $Role.RoleName)
					$RoleDetails
					if ($RoleDetails.InstanceCount -eq $using:InstanceCount)
					{
						Write-Output ("Role {0} already has instance count {1}." -f $Role.RoleName, $using:InstanceCount)
					}else
					{
						Write-Output ("Role {0} changing instance count from {1} to {2}." -f $Role.RoleName, $RoleDetails.InstanceCount, $using:InstanceCount)
						Set-AzureRole -ServiceName $using:ServiceName -Slot $using:Slot -RoleName $Role.RoleName -Count $using:InstanceCount 
						Write-Output ("Role {0} changed instance count from {1} to {2}." -f $Role.RoleName, $RoleDetails.InstanceCount, $using:InstanceCount)
					}
			  }
           }
        }
    }
    
    $Finish = [System.DateTime]::Now
    $TotalUsed = $Finish.Subtract($Start).TotalSeconds
   
    Write-Output ("Updated cloud service {0} slot {1} to instance count {2} in subscription {3} in {4} seconds." -f $ServiceName, $Slot, $InstanceCount, $SubscriptionName, $TotalUsed)
    "Finished " + $Finish.ToString("HH:mm:ss.ffffzzz")
} 


$basedir =  "$home\Source\Repos\Call-Graph-Builder-DotNet\AzureScripts"
##### import-module $basedir\WindowsPowerShell\Modules\Update-CloudServiceScale\Update-CloudServiceScale.psm1

# The number of Workers(Silos) 

$SubscriptionName ="Microsoft Azure Sponsorship"
$SubscriptionId = "7b8850f5-cecf-4f64-8f94-b4f06da33403"
# This is the ID of the certificate I added to the Azure subscription 
$Thumbprint = "7A5FE607CBB727B70440DBB64FBEF4BD5E91C6B6"

## MSR Subscription 
## $SubscriptionId = "6412f21b-b221-49c4-8728-96719adc2306"
## $SubscriptionName = "Ben Livshits"

## Roslyn Subscription 
##$SubscriptionId = "fea3a3c9-c4a3-4743-9835-1502a54705e9"
##$SubscriptionName = "Internal Consumption"


# This is the file with a certificaque of the subscription 
# Maybe there is a way to avoid this by logging in azure before
#$PfxFilePath =  $basedir+"\azure-internal-consumption.pfx"
#$PfxPassword = "Diego2015Ben$"

$PfxFilePath =  $basedir+"\mgmtcert.pfx"
$PfxPassword = "Edgar"
# We Need to create a pfx for Ben's subscripiton

$ServiceName = "orleansservicedg"

$Slot = "Production"

###Import-AzurePublishSettingsFile .\mySubscription.publishsettings

#Update-CloudServiceScale  -SubscriptionID $SubscriptionId -SubscriptionName $SubscriptionName  -PfxFilePath $PfxFilePath -PfxPassword $PfxPassword -ServiceName $ServiceName -Slot $SLot -InstanceCount $InstanceCount
Update-CloudServiceScale  -SubscriptionID $SubscriptionId -SubscriptionName $SubscriptionName  -PfxFilePath $PfxFilePath -Thumbprint $Thumbprint -ServiceName $ServiceName -Slot $SLot -InstanceCount $InstanceCount



