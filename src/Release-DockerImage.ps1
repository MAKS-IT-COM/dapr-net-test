# Set variables
$projectName = "dapr-test"
$harborUrl = "cr.maks-it.com"  # e.g., "harbor.yourdomain.com"
$tag = "latest"  # Customize the tag as needed

# Retrieve username and password from environment variable
$creds = $Env:CR_MAKS_IT -split '\|'
$harborUsername = $creds[0]
$harborPassword = $creds[1]

# Authenticate with Harbor
docker login $harborUrl -u $harborUsername -p $harborPassword

# List of services to build and push with the current context
$services = @{
    "publisher" = "Publisher/Dockerfile"
    "subscriber" = "Subscriber/Dockerfile"
}

$contextPath = "."

foreach ($service in $services.Keys) {
    $dockerfilePath = $services[$service]
    $imageName = "$harborUrl/$projectName/${service}:${tag}"
    
    # Build the Docker image
    Write-Output "Building image $imageName from $dockerfilePath..."
    docker build -t $imageName -f $dockerfilePath $contextPath

    # Push the Docker image
    Write-Output "Pushing image $imageName..."
    docker push $imageName
}

# Logout after pushing images
docker logout $harborUrl