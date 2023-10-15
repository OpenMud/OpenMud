dotnet build --no-restore --configuration Release
dotnet pack .\OpenMud.Mudpiler.Compiler.Project.Cli\OpenMud.Mudpiler.Compiler.Project.Cli.csproj -c Release -o out
dotnet nuget push -s https://api.nuget.org/v3/index.json -k  ${{ secrets.NUGET_TOKEN }} .\out\*.nupkg