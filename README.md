# Steeltoe NetCoreToolService

[![Build Status](https://github.com/SteeltoeOSS/NetCoreToolService/actions/workflows/build-and-stage.yml/badge.svg?branch=main)](https://github.com/SteeltoeOSS/NetCoreToolService/actions/workflows/build-and-stage.yml?query=branch%3Amain)

## Generate Kubernetes Manifest

```
# default manifest
$ ytt -f kubernetes

# sample custom manifest, see kubernetes/defaults.yaml for available parameters
$ ytt -f kubernetes -v replica_count=5
```
