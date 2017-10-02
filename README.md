# Distributed Call-Graph Analysis

This repository contains a distributed Call-Graph analysis for C# built on top of [The .NET Compiler Platform ("Roslyn")](https://github.com/dotnet/roslyn) and [Orleans - Distributed Virtual Actor Model](https://github.com/dotnet/orleans). The analysis is designed to scale with the size of the input and can be deployed on [Microsoft Azure: Cloud Computing Platform & Services](https://azure.microsoft.com/). The reliance on a cloud cluster provides a degree of elasticity for CPU, memory, and storage resources.

For more detailed information take a look to the following documents:

* Toward Full Elasticity in Distributed Static Analysis: The Case of Callgraph Analysis [[Paper]](http://dx.doi.org/10.1145/3106237.3106261) [[PDF]](http://lafhis.dc.uba.ar/users/~diegog/papers/fse17.pdf)
* Technical Report [[PDF]](https://www.doc.ic.ac.uk/~livshits/papers/tr/scalable_tr.pdf).

## Benchmarks

### Synthetic

The synthetic benchmarks are located inside [SyntheticBenchmarks](https://github.com/edgardozoppi/call-graph-orleans/tree/master/SyntheticBenchmarks) folder. It contains automatically generated projects with 100 - 1,000 - 10,000 - 100,000 and 1,000,000 methods.

### Real-life projects

We have evaluated our analysis with the following projects taken directly from GitHub.

* [ShareX](https://github.com/ShareX/ShareX)

  Repository: https://github.com/ShareX/ShareX.git  
  Branch: master  
  Commit: 0697738ec89363f092a49b1c6021dc8d6324ee1d

* [ILSpy](https://github.com/icsharpcode/ILSpy)

  Repository: https://github.com/icsharpcode/ILSpy.git  
  Branch: master  
  Commit: 2726336b3a56d343457b453d9be76b793ea2ebc0

* [Azure-PowerShell](https://github.com/Azure/azure-powershell)

  Repository: https://github.com/Azure/azure-powershell.git  
  Branch: dev  
  Commit: 4485b9b42df290edc19351d134b92e75abb04329

## How to build the tool
 
Open Solution.sln and build with Visual Studio. Using Visual Studio 2017 or above is recommended.

### Prerequisites

Install [Visual Studio 2017](https://www.visualstudio.com/downloads/) with the following workloads:

* ASP.NET and web development
* Azure development