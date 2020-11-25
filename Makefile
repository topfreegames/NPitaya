.PHONY: setup
setup:
	@git submodule update --init
	@cd lib/pitaya-rs; git submodule update --init; cargo build --release

.PHONY: build
build: clean
	@dotnet build

.PHONY: pack
pack: build
	@dotnet pack NPitaya --configuration Release

.PHONY: clean
clean:
	@dotnet clean

.PHONY: push
publish:
	@dotnet nuget push NPitaya/bin/Release/*.nupkg -k $(NUGET_API_KEY) -s https://api.nuget.org/v3/index.json