dotnet build -c Release ./Zen.Logging.Standard.csproj
dotnet pack -c Release ./Zen.Logging.Standard.csproj

set "PYTHON_EXE=python"

set "PYTHON_SCRIPT=read_proj_version.py"

set "PROJ_FILE=./Zen.Logging.Standard.csproj"

FOR /F "delims=" %%i IN ('%PYTHON_EXE% "%PYTHON_SCRIPT%" "%PROJ_FILE%"') DO (
    set "VERSION=%%i"
)

echo "%VERSION%"

dotnet nuget push ./bin/Release/Zen.Logging.Standard.%VERSION%.nupkg --skip-duplicate --api-key %DBACCESS_NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
