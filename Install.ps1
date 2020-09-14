param($installPath, $toolsPath, $package, $project)

function setCopyIfNewer($itemPath) {
	$projectItems = $project.ProjectItems
	foreach ($name in $itemPath.Split('/')) {
		$configItem = $projectItems.Item($name)
		$projectItems = $configItem.ProjectItems
	}

	# set 'Copy To Output Directory' to 'Copy if newer'
	$copyToOutput = $configItem.Properties.Item("CopyToOutputDirectory")
	$copyToOutput.Value = 2

	# set 'Build Action' to 'Content'
	$buildAction = $configItem.Properties.Item("BuildAction")
	$buildAction.Value = 2
}

setCopyIfNewer("zbarimg.exe")
