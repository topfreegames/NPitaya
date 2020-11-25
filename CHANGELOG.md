# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.14.0] - 2020-10-26
### Added
- Now it is possible to implement the MetricsReporter interface through FFI.
- Prometheus metrics server at C# level, with system resources and language environment metrics.
### Changed
- A metrics reporter needs to be manually provided in the PitayaBuilder.
### Removed
- Metrics settings is now removed.

## [0.13.0] - 2020-10-15
### Added
- Add support for gauges in custom metrics.

## [0.12.4] - 2020-10-14
### Added
- First support for .NET using Nupkg.

### Removed
- Support for Unity was removed from the NPitaya Nupkg. It will be considered a new package NPitaya-Unity or reintroducing support
for the current package.
