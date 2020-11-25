FROM mcr.microsoft.com/dotnet/core/runtime:3.1

COPY exampleapp/bin/Release/netcoreapp3.1/publish/ /App
COPY exampleapp/libpitaya.so /App
COPY ./csharp.toml /App

WORKDIR /App
ENTRYPOINT [ "dotnet", "exampleapp.dll" ]
