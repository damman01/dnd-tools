# Contributing

Thanks for considering a contribution to DnD Tools!

## Development setup

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/cli/overview)

Run the app via `aspire start` (never `dotnet run` directly on the AppHost),
then `aspire wait web`. Build the whole solution with:

```powershell
dotnet build DndTools.slnx
```

## Commit messages

This repo follows [Conventional Commits](https://www.conventionalcommits.org/)
in English, e.g.:

```
feat: add tracker-row editor for resource cards
fix: correct enum casing in card deck JSON import
docs: document the card deck JSON schema
chore: update .gitignore for .NET build artifacts
```

Keep commits small and focused on one logical change.

## Pull requests

1. Fork the repo and create a feature branch off `main`.
2. Make your change, including tests/build verification where applicable.
3. Ensure `dotnet build DndTools.slnx` succeeds without new warnings.
4. Open a PR describing the change and, for UI changes, include a screenshot.

## Reporting issues

Please open a GitHub issue with steps to reproduce, expected vs. actual
behavior, and your environment (OS, .NET SDK version).

## Code of conduct

Be respectful and constructive. Harassment or abusive behavior will not be
tolerated.
