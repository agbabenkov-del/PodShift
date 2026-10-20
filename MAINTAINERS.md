# Maintainer checklist

This file is intentionally short and practical.

## Before a release

- Update `CHANGELOG.md`.
- Update the version in `PodShift.csproj` and the footer if needed.
- Add/update `RELEASE_NOTES_vX.Y...md`.
- Confirm GitHub Actions publish succeeds.
- Manually test Connect and Disconnect on real AirPods hardware.
- Use commit subject `Release vX.Y...` to trigger automated prerelease packaging.
- Verify the GitHub Release contains `PodShift-win-x64.zip` and `SHA256SUMS.txt`.

## Before merging Bluetooth changes

- Keep the Windows Settings fallback.
- Do not report success before Windows confirms the target state.
- Document any new native API calls.
- Record hardware and Windows versions used for testing.
