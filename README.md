POC of KEDA Apache Kafka scaler

Use a PowerShell window for scripts below.

# First-time Setup

## Pre-requisites

* Create a Confluent Kafka cluster with a topic named "kedapoc" with 50 partitions
* Install Azure CLI.

## Update kafka.properties with your Kafka cluster credentials

Edit kafka.properties file and specify your own credentials.

## Azure

Variables - change as appropriate:
```
$acr="acrchinhdo4"
$rg="rg-kedapoc4"
$sub="49efc1c0-0f90-4241-a966-f940f2ae2180" # AZ sub ID. Run "az account list" to get list of subscription ids.
$aksCluster="aksPoc4"
$location="eastus2"
$ver="latest"
cd <solution directory>
```

Create resource group, container registry:
```
az login # if needed
az account set --subscription $sub
az group create --name $rg --location $location
az acr create --resource-group $rg --name $acr --sku Basic
az acr login --name $acr
```

Create Azure Kubernetes (AKS) Cluster (below command takes about 5-15 minutes):
```
az aks create --resource-group $rg --name $aksCluster --node-count 1 --node-vm-size "Standard_B2s" --generate-ssh-keys --enable-cluster-autoscaler --min-count 1 --max-count 2 --attach-acr $acr

az aks update -n $aksCluster -g $rg --attach-acr /subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.ContainerRegistry/registries/$acr
```

Create Kubernetes Namespace:
```
az aks get-credentials --resource-group $rg --name $aksCluster
kubectl create namespace kedapoc
kubectl config set-context --current --namespace=kedapoc
```

Install Keda:
```
kubectl apply -f https://github.com/kedacore/keda/releases/download/v2.6.0/keda-2.6.0.yaml
```

# Build & Deploy

## Setup for each PowerShell session

For each new PowerShell session, create the variables in "Set up variables" above. Then run these commands:
```
az acr login --name $acr
az acr list # Get ACR Login Server
$acrLoginServer="acrchinhdo1.azurecr.io" # replace with your ACR Login Server
```

Configure kubectl to connect to Kubernetes cluster:
```
az aks get-credentials --resource-group $rg --name $aksCluster
kubectl config set-context --current --namespace=kedapoc
```

## Build & Deploy Latest Version

Make sure k8s.yml also has correct value for acrLoginServer

```
docker-compose up --build -d --no-start
docker image tag consumer $acrLoginServer/kedapoc-consumer:$ver
docker push $acrLoginServer/kedapoc-consumer:$ver
kubectl delete deployment consumer
kubectl apply -f k8s.yml
kubectl apply -f keda.yml
```

## Test

To send 10 messages to the test topic:
```
cd <Utility Project folder>
dotnet run --msgs 10
```

To monitor pods, run ```kubectl get pods``` or use the Lens GUI.

To monitor the topic lag, on [Confluent Cloud](https://confluent.cloud/login), go to the cluster, Data Integration, Clients, Consumer Lag, then click on the topic.

To see the output of a specific pod:
```
```

# Additional Info

## Other useful commands:

List images in the registry: ```az acr repository list --name $acr --output table```

List images and show tags: ```az acr repository show-tags --name $acr --repository kedapoc-consumer --output table```

Create secrets:
```
kubectl create secret docker-registry acrsecret --namespace kedapoc --docker-server=$acrLoginServer --docker-username=acrchinhdo --docker-password==FeJq1b2dOjiqyb7YRQT5z64Q9SSumIt
```

Stop AKS when not using (to save money): ```az aks stop --resource-group $rg --name $aksCluster```

## Errors

When creating AKS cluster, if getting this error ""The resource with name 'acrchinhdo' and type 'Microsoft.ContainerRegistry/registries' could not be found in subscription '...', try running this command:

```az aks update -n $aksCluster -g $rg --attach-acr /subscriptions/$sub/resourceGroups/$rg/providers/Microsoft.ContainerRegistry/registries/$acr```

## TODOs
* Add CPU/memory limits