# PodShift Security Policy

PodShift is a small desktop utility that interacts with the local Windows Bluetooth stack. Security reports are welcome, especially if they involve unexpected privilege requirements, unsafe native interop behavior, or handling of local device information.

## Supported versions

Security fixes are applied to the latest published alpha/prerelease and the current `main` branch.

## Reporting a vulnerability

Please avoid publishing exploit details or sensitive local information in a public issue before a fix is available.

For now, open a GitHub issue with a minimal description stating that the report is security-sensitive and avoid including secrets, personal data, or exploit details. The maintainer can then coordinate the next step through GitHub.

PodShift does not intentionally collect or transmit Bluetooth device information. Current Bluetooth operations are performed locally on the Windows machine.
