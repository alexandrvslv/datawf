# DataWF ORM Web Service Code Generation Tool

Produce from Models: WebApi controllers, Log classes, Model Properties Invokers 

## С# Projects use sample:

```XML
 <DotNetCliToolReference Include="DataWF.WebService.Generator" Version="1.0.3" />
...
 <Target Name="PreBuild" BeforeTargets="PreBuildEvent">
    <Exec Command="dotnet wscodegen -m Controllers, Logs, Invokers -p Assembly1 Assembly1 -o $(ProjectDir)Controllers" />
 </Target>  
```