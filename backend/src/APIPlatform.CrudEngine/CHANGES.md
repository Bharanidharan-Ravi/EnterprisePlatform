# CrudEngine — Pipeline Architecture Refinement (2nd revision)

Baseline preserved: Foundation, SharedSchema, Data untouched. GenericRepository,
CompositeRepository, SqlQueryBuilder, EntityOperationBinding/MultiResultOperationConfig,
IEntityOperationBindingProvider, IProcedurePort, EntityTypeRegistry all UNCHANGED and reused —
they are now execution details invoked by the pipeline instead of the primary API.

## New (Req 1-13)
- Models/CrudContext.cs, OperationPlan.cs, DefaultValueModels.cs — shared pipeline context (Req 3),
  immutable query plan (Req 5), config-driven default-value bindings (Req 8)
- Caching/IEntityMetadataCache.cs + EntityMetadataCache.cs — cached metadata resolution (Req 12)
- Validation/IValidationPipeline.cs + MetadataValidationPipeline.cs + ValidationRuleEvaluator.cs
  (ASSUMPTION BOUNDARY, see below) — metadata-driven validation (Req 7)
- Defaults/IDefaultValueProvider.cs + DefaultValueProcessor.cs + NoOpEntityDefaultValueProvider.cs
  — generic default-value processing (Req 8), no business defaults
- Hooks/ICrudPipelineHook.cs — Before/After extension points (Req 9)
- Sql/Dialects/* (ISqlDialect, SqlServerDialect, HanaDialect, ISqlDialectResolver) — provider
  abstraction isolated to paging syntax (Req 11)
- Sql/Builders/* (Filter/Sort/Paging clause builders) + Sql/QuerySqlBuilder.cs — composable query
  builders consuming OperationPlan (Req 5, Req 6). Insert/Update/Delete generation intentionally
  left in the existing SqlQueryBuilder — already single-purpose per operation, provider-agnostic,
  and working; splitting further would be churn without benefit.
- Pipeline/ICrudPipeline.cs + CrudPipeline.cs — the 9-stage execution lifecycle (Req 2)
- Engine/ICrudEngine.cs + CrudEngine.cs — new primary public API (Req 10); IRepository<T> is now
  an execution detail underneath it (Req 1)
- Services/CompiledInvokerCache.cs + ResultPropertyCache.cs — compiled-delegate cache replacing
  repeated MakeGenericMethod/MethodInfo.Invoke in BatchCrudExecutor (Req 13)
- Services/NoOpMultiResultOperationProvider.cs — safe default, mirrors NoOpEntityDefaultValueProvider

## Modified
- Services/BatchCrudExecutor.cs — now uses CompiledInvokerCache/ResultPropertyCache instead of raw
  reflection per call (Req 13). Behavior unchanged.
- DependencyInjection/ServiceCollectionExtensions.cs — registers the pipeline + ICrudEngine<>,
  adds AddCrudPipelineHook<T>(). AddCrudEngine(enableProcedureBindings) signature unchanged;
  default call still works with zero extra app registrations (NoOp providers fill gaps).

## Unchanged (reused as-is, preserved per Req 14)
Repositories/GenericRepository.cs, Repositories/CompositeRepository.cs, Sql/SqlQueryBuilder.cs,
Registry/EntityTypeRegistry.cs, Adapters/IProcedurePort.cs, Interfaces/IEntityOperationBindingProvider.cs,
Models/EntityOperationBinding.cs, Models/MultiResultOperationConfig.cs, Models/CrudBatchModels.cs,
Models/CrudOperationType.cs, Services/MultiResultQueryService.cs, Services/IBatchCrudExecutor.cs,
Services/IEntityService.cs, Services/EntityService.cs.

## Assumption boundaries (flagged, isolated to one file each — tell me if these differ and only
that file changes)
1. Validation/ValidationRuleEvaluator.cs — guesses ValidationRuleDefinition's property names
   (RuleType/Pattern/Min/Max/AllowedValues) and FieldDefinition.IsRequired via reflection, since I
   don't have SharedSchema's actual source.
2. Foundation.Results.ValidationResult — assumed to expose IsSuccess / Errors (IReadOnlyList<ErrorInfo>)
   and static Success()/Failure(IEnumerable<ErrorInfo>) factories, consistent with Result<T>/
   OperationResult's existing IsSuccess/Error shape already used in BatchCrudExecutor.
3. Sql/Dialects/DefaultSqlDialectResolver.cs — assumes DatabaseOptions.Provider is a DatabaseProvider
   enum with SqlServer/Hana members, and that DatabaseOptions is directly resolvable from DI (not
   wrapped in IOptions<T>).

## Revision 3 — Enterprise Execution Stages

Refactored CrudPipeline's inline logic into 6 explicit single-responsibility stages (adopted your
suggested naming). No existing service/builder/validator/repository was rewritten — each stage is
a thin wrapper calling the same class it already called from inside CrudPipeline.

New:
- Pipeline/IPipelineStage.cs
- Pipeline/Stages/MetadataResolutionStage.cs — calls IEntityMetadataCache + ISqlDialectResolver (unchanged)
- Pipeline/Stages/ContextEnrichmentStage.cs — calls IDefaultValueProcessor (unchanged)
- Pipeline/Stages/ValidationStage.cs — calls IValidationPipeline (unchanged)
- Pipeline/Stages/ExecutionPlanningStage.cs — builds OperationPlan (logic moved here from CrudEngine.ListAsync)
- Pipeline/Stages/ExecutionStage.cs — the exact repository/QuerySqlBuilder logic previously inline in CrudPipeline
- Pipeline/Stages/ResponseMappingStage.cs — builds new CrudResponse<T> (additive; ExecutionResult still read directly by Engine for backward compat)
- Models/CrudResponse.cs

Modified:
- Models/CrudContext.cs — additive properties only (RequestedFilters/Sorting/Paging, DatabaseProviderName, Diagnostics, Response)
- Pipeline/CrudPipeline.cs — now pure orchestration, no logic: Metadata → Enrichment → Validation → [hooks] → Planning → Execution → [hooks] → Mapping
- Engine/CrudEngine.cs — ListAsync passes requested filters/sort/paging instead of building OperationPlan itself (planning now owned by ExecutionPlanningStage)
- DependencyInjection/ServiceCollectionExtensions.cs — registers the 6 stages

Hook placement: existing Before/After hooks already sit exactly where your diagram places
Workflow Hook (pre-Planning) and Notification Hook (post-Execution) — no change needed there.
Authentication/Audit hook boundaries (pre-Metadata, post-Enrichment) aren't wired yet — adding them
is a 2-line change in CrudPipeline.RunAsync when a future module needs them; stages themselves
never change.
