param
(
    [string] $cloudService,
    [string] $publishSettings,
    [string] $subscription,
    [string] $role,
    [string] $setting,
    [string] $value
)

# param checking code removed for brevity

Import-AzurePublishSettingsFile $publishSettings -ErrorAction Stop | Out-Null

function SaveNewSettingInXmlFile($cloudService, [xml]$configuration, $setting, [string]$value)
{
    # get the <Role name="Customer.Api"> or <Role name="Customer.NewsletterSubscription.Api"> or <Role name="Identity.Web"> element
    $roleElement = $configuration.ServiceConfiguration.Role | ? { $_.name -eq $role }

    if (-not($roleElement))
    {
        Throw "Could not find role $role in existing cloud config"
    }

    # get the existing AzureServiceBusQueueConfig.ConnectionString element
    $settingElement = $roleElement.ConfigurationSettings.Setting | ? { $_.name -eq $setting }

    if (-not($settingElement))
    {
        Throw "Could not find existing element in cloud config with name $setting"
    }

    if ($settingElement.value -eq $value)
    {
        Write-Host "No change detected, so will not update cloud config"
        return $null
    }

    # update the value 
    $settingElement.value = $value

    # write configuration out to a file
    $filename = $cloudService + ".cscfg"
    $configuration.Save("$pwd\$filename")

    return $filename
}

Write-Host "Updating setting for $cloudService" -ForegroundColor Green

Select-AzureSubscription -SubscriptionName $subscription -ErrorAction Stop  

# get the current settings from Azure
$deployment = Get-AzureDeployment $cloudService -ErrorAction Stop

# save settings with new value to a .cscfg file
$filename = SaveNewSettingInXmlFile $cloudService $deployment.Configuration $setting $value

if (-not($filename)) # there was no change to the cloud config so we can exit nicely
{
    return
}

# change the settings in Azure
Set-AzureDeployment -Config -ServiceName $cloudService -Configuration "$pwd\$filename" -Slot Production

# clean up - delete .cscfg file
Remove-Item ("$pwd\$filename")

Write-Host "done"