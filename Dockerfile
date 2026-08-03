FROM dhi.io/dotnet:10-sdk AS build

WORKDIR /src

COPY dotnetcore/InfServerNetCore.sln ./dotnetcore/
COPY dotnetcore/ ./dotnetcore/

RUN dotnet restore dotnetcore/InfServerNetCore.sln
RUN dotnet publish dotnetcore/InfServerNetCore.sln --no-restore -c Release -o /app

FROM dhi.io/dotnet:10-debian

WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["./ZoneServer"]
