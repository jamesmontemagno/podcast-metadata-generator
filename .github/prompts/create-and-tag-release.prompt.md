---
description: "Create and publish a new release for this repo with a v-prefixed semver tag"
name: "Create And Tag Release"
argument-hint: "version (e.g. 1.2.3)"
agent: "agent"
---
Create and tag a new release for this repository using the provided version argument.

Requirements:
- Interpret the argument as a semver version without `v` (for example `1.2.3`).
- Use `v<version>` as the git tag (for example `v1.2.3`).
- Never delete or rewrite existing tags.
- If the target tag already exists locally or remotely, stop and report it.

Workflow:
1. Validate input format is semver (`X.Y.Z` with optional prerelease/build metadata).
2. Check git status and branch; require `main` unless explicitly told otherwise.
3. If there are relevant uncommitted changes for the release, commit them with a clear message. If no changes, do not create an empty commit.
4. Push `main` to `origin`.
5. Create an annotated tag `v<version>` with message `Release v<version>`.
6. Push the tag to `origin`.
7. Verify the tag exists on origin and show the exact commit SHA it points to.
8. Confirm that the release workflow should trigger from the `v*` tag.

Output format:
- `Version:` the requested version and final tag.
- `Commit:` the commit SHA tagged.
- `Actions:` bullet list of commands executed.
- `Result:` success/failure with a short reason.
- `Next:` if successful, include one line about checking GitHub Actions run status.

If any step fails, stop immediately, report the failing command and stderr, and suggest the safest recovery step.
