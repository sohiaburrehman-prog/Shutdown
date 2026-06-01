# Contributing

Thank you for your interest in Shutdown Timer Advanced.

## Important

This project is **proprietary software**. The source is published for transparency and review. See [LICENSE](LICENSE) and [Docs/EULA.md](Docs/EULA.md).

## How you can help

### Bug reports and feature requests

Open a [GitHub Issue](https://github.com/sohiaburrehman-prog/Shutdown/issues) with:

- Windows version (e.g. Windows 11 24H2)
- App version (Settings → About, or Store listing)
- Clear steps to reproduce
- Expected vs actual behavior
- Screenshots or logs if relevant

### Security issues

Do **not** open public issues for security vulnerabilities. Email **sohiab@outlook.com** with subject `[Shutdown Timer Security]`. See [SECURITY.md](SECURITY.md).

### Pull requests

Pull requests are welcome for clear bug fixes and documentation improvements, but may be declined if they:

- Change licensing or legal documents without discussion
- Add telemetry, analytics, or external data collection
- Introduce dependencies with incompatible licenses
- Expand scope beyond an agreed issue

Please open an issue first for substantial changes.

## Development setup

1. Clone the repository
2. Open `ShutdownTimer.sln` in Visual Studio 2022
3. Restore NuGet packages and build for `x64`
4. Run tests: `dotnet test ShutdownTimer.Tests\ShutdownTimer.Tests.csproj`

See [README.md](README.md) and [Docs/README.md](Docs/README.md) for more detail.

## Code standards

- Follow existing MVVM patterns (`CommunityToolkit.Mvvm`)
- Keep WinUI XAML consistent with shared styles in `App.xaml`
- Add or update tests for non-trivial logic changes
- Do not commit build outputs, certificates, or secrets

## Legal

By contributing, you agree that your contributions may be used under the project's proprietary license at the maintainer's discretion, unless otherwise agreed in writing.
