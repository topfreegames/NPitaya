start-test-deps:
	@docker-compose up -d

stop-test-deps:
	@docker-compose down

test: start-test-deps
	@dotnet test NPitaya.Tests
	@$(MAKE) stop-test-deps

.PHONY: clean
clean:
	@dotnet clean NPitaya
	@rm -rf NPitaya/bin/Release
	@rm -rf NPitaya/bin/Debug

.PHONY: build
build: clean
	@dotnet build NPitaya --configuration Release

.PHONY: pack
pack: build
	@dotnet pack NPitaya --configuration Release

.PHONY: push
push:
	@dotnet nuget push NPitaya/bin/Release/*.nupkg -k $(NUGET_API_KEY) -s https://api.nuget.org/v3/index.json
