# 10. Networking, expansion, and release operations

## 10.1 Launch architecture

The launch game has:

- Local saves.
- Local campaign content.
- No account requirement.
- No backend dependency.
- No multiplayer.
- No server-controlled economy.
- No advertising SDK.

Diagnostics remain bounded and exportable. Store crash reports and approved beta feedback provide the initial operational evidence.

## 10.2 Expansion readiness

Prepare these extension points:

- Stable content IDs.
- Save migrations.
- Versioned content catalogs.
- Versioned balance definitions.
- Local defaults for optional configuration.
- Separate platform and persistence interfaces.
- A safe return-to-menu path when content cannot load.

Do not build authentication, cloud synchronization, leaderboards, or live events merely to demonstrate future capability.

If cloud saves are added later:

- Define account linking and deletion first.
- Preserve conflicting snapshots.
- Never merge currency by addition.
- Resolve profile lineage and transaction history explicitly.

An online competitive economy would require a separate authority model. Local save checksums cannot provide it.

## 10.3 Release safety

Release builds must:

- Disable development builds and profiler connections.
- Exclude active automation endpoints.
- Exclude runtime code-reload capability.
- Reject enabling automation scripting defines.
- Preserve save compatibility.
- Contain the complete purchased campaign.
- Include licenses and required disclosures.
- Recover cleanly from missing optional content.

The installed experimental Unity Pipeline package has conditional runtime compilation. Treat its absence from release behavior as a checked requirement.

---


[Return to development plan](../development-plan.md)
