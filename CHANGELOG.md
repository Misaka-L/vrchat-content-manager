# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Show App version, commit hash and build date in App settings page [`#70`](https://github.com/project-vrcz/content-manager/pull/70).
- Basic Linux Support [`#76`](https://github.com/project-vrcz/content-manager/pull/76)

### Changed

- Adjust http rqeuest pipeline [`#80`](https://github.com/project-vrcz/content-manager/pull/80)
  - Use DecorrelatedJitterV2 as http request retry strategy
  - Increase retry delay
  - Increase MaxConnectionsPerServer to 256 from 10 for AWS S3 HttpClient

[unreleased]: https://github.com/olivierlacan/keep-a-changelog/