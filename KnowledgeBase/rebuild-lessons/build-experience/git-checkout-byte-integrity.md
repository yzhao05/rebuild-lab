# Git checkout byte integrity

## Failure pattern

A Builder Pack can pass byte-level Foundation, Common, and ApprovedAssets
checks before publication, then fail immediately after a Windows clone. When
Git uses `core.autocrlf=true` without repository attributes, checkout may
convert tracked LF text to CRLF. The files remain semantically equivalent, but
their sizes and SHA-256 values no longer match frozen manifests.

## Prevention rule

- Keep the repository-root `.gitattributes` rule `* text=auto eol=lf`.
- Mark binary model, image, audio, font, archive, and plugin formats as binary.
- Treat `.gitattributes` as protected Builder foundation and copy it into every
  blind-rebuild workspace.
- Do not weaken hash verification or normalize bytes inside the verifier.
  Checkout must preserve the committed bytes used to create the inventory.
- Before publishing, test a fresh clone while Git has `core.autocrlf=true`, run
  the Builder boundary verifier, and confirm the cloned Foundation, Common,
  and ApprovedAssets still match.

## Search cues

`clone hash mismatch`, `core.autocrlf`, `LF CRLF`, `all Foundation hashes`,
`Common size differs`, `ApprovedAssets digest differs`, `.gitattributes`.
