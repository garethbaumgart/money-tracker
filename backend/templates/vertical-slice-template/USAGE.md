# Vertical Slice Template Usage

This template is the starter shape for a new backend module feature.

## Copy Process
1. Copy this folder into `backend/src/Modules/<Feature>/`.
2. Rename `Feature*` classes/files to your feature language.
3. Update namespaces from `MoneyTracker.Modules.Feature` to the target module.
4. Wire endpoint mapping into your API startup/composition root.
5. Replace TODO markers before opening a PR.

## Minimum Completion Requirements
1. Domain invariants are implemented.
2. Command handler orchestrates without embedding business rules.
3. Endpoint remains thin and contract-driven.
4. Repository implementation is added for your actual persistence stack.
5. Tests are updated and passing.

## Design Notes
1. Domain stays framework-agnostic.
2. Application depends on domain abstractions only.
3. Infrastructure depends on domain abstractions and external systems.
4. Presentation depends on application contracts.
