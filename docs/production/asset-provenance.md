# Asset provenance

Record provenance before importing externally sourced or generated content. A placeholder folder does not grant rights to an asset, and a purchase receipt alone does not establish redistribution rights.

Keep authoring files under `SourceArt/Characters/`, `SourceArt/Equipment/`, `SourceArt/Environments/`, or `SourceArt/Audio/`. Unity imports and their metadata belong under `Assets/`. Keep each imported asset paired with its `.meta` file and preserve its GUID when moving it.

## Integration requirements

- Record the source, creator, license terms, intended use, and any attribution or redistribution restriction. Store a dated license copy or durable reference with the record.
- Confirm rights cover the intended commercial use, modifications, and distribution inside the player. Flag unclear terms as **UNVERIFIED** and keep the asset out of a release until resolved.
- Record generated content with its generator, model or version when available, creation date, source inputs, and the applicable usage terms. Do not claim cleared rights without evidence for its inputs.
- Hash the original file and record any conversion, retopology, compression, audio processing, or other modification. Link the imported asset to the authoring source.
- Use Git LFS for binary source files and imported binary content. Do not put licenses, Unity YAML, or `.meta` files in LFS.
- Name one owner for changes to shared source content or Unity assets. A record may cite an approved purchase, but this template does not authorize one.

## Record template

Create one Markdown record per asset or clearly defined source bundle. Store it beside the production records and link it here as content is admitted. Complete unknown fields with **UNVERIFIED**, not an assumption.

```markdown
# Asset: <name or bundle identifier>

- Status: UNVERIFIED | APPROVED FOR PROJECT USE | BLOCKED
- Owner: <responsible person>
- Recorded on: <YYYY-MM-DD>
- Creator or rights holder: <name>
- Acquisition type: <original | commissioned | licensed | generated>
- Source URL or delivery reference: <durable reference>
- Acquired or created on: <YYYY-MM-DD>
- License name and version: <license>
- License evidence: <repository-relative license copy or durable reference>
- Commercial use: <allowed scope and evidence>
- Modification rights: <allowed scope and evidence>
- Player redistribution rights: <allowed scope and evidence>
- Attribution requirement: <exact required text and placement, or none with evidence>
- Other restrictions: <seats, platform, territory, term, exclusivity, or none with evidence>
- Purchase authorization and receipt reference: <reference or not applicable>
- Generator and model/version: <reference or not applicable>
- Generation date and input provenance: <references or not applicable>
- Authoring source path: <SourceArt/...>
- Original file SHA-256: <hash>
- Imported Unity asset path: <Assets/...>
- Unity asset GUID: <GUID from the matching .meta>
- Modifications and tools: <dated processing history>
- Intended use: <scene, character, equipment, environment, or audio role>
- Dependency record: <related fonts, textures, plugins, samples, or none>
- Review evidence and approver: <reference and person>
- Release eligibility: UNVERIFIED | ELIGIBLE | BLOCKED
- Open questions: <remaining rights or technical questions, or none>
```

No externally sourced or generated asset is approved merely by this Task02 scaffold. Add completed records when actual content is introduced.

## Documentation imagery

- [Infinity Revival GPT-Image concept and generation record](../media/infinity-revival-prototype.md): authorized documentation prototype, not a runtime asset; production release eligibility remains unverified.
- [Current Refuge render](../media/refuge-current.png): project-owned screenshot evidence from the saved Bootstrap scene in the desktop Editor, separate from the generated concept.
