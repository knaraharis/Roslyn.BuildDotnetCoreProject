param([string] $path = "C:", 
	  [string] $app = "default.dll", 
      [string] $image = "defaultImage",
      [string] $container = "defaultContainer",
      [string] $port = "8080:80")

Write-Progress -Activity 'Spinning new Docker container started' -Status 'Starting' -PercentComplete 5

Set-Location $path

Write-Progress -Activity 'Spinning new Docker container in Progress' -Status 'Progress' -PercentComplete 10

docker build --tag $image .

Write-Progress -Activity 'Spinning new Docker container in Progress' -Status 'Progress' -PercentComplete 50

docker run -d -p $port --name $container --entrypoint "dotnet" $image $app

Write-Progress -Activity 'Spinning new Docker container in Progress' -Status 'Progress' -PercentComplete 100

