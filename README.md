# Steeltoe NetCoreToolService

[![Build Status](https://dev.azure.com/SteeltoeOSS/Steeltoe/_apis/build/status/Initializr/SteeltoeOSS.NetCoreToolService?branchName=main)](https://dev.azure.com/SteeltoeOSS/Steeltoe/_build/latest?definitionId=45&branchName=main)

## Generate Kubernetes Manifest

```
# default manifest
$ ytt -f kubernetes

# sample custom manifest, see kubernetes/defaults.yaml for available parameters
$ ytt -f kubernetes -v replica_count=5
```
