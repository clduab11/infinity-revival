# Task 06 missing-API evidence

## red-missing-api

Native Unity runner exit: 1 (expected missing API before implementation).

```text
Assets\Game\Tests\EditMode\DefenseEncounterTests.cs(17,24): error CS0246: The type or namespace name 'EnemyStrike' could not be found (are you missing a using directive or an assembly reference?)
Assets\Game\Tests\EditMode\DefenseRulesTests.cs(18,66): error CS0246: The type or namespace name 'DefenseOutcome' could not be found (are you missing a using directive or an assembly reference?)
Assets\Game\Tests\EditMode\DefenseTimingTests.cs(16,81): error CS0246: The type or namespace name 'DefenseOutcome' could not be found (are you missing a using directive or an assembly reference?)
Assets\Game\Tests\EditMode\DefenseTimingTests.cs(28,82): error CS0246: The type or namespace name 'PlayerCombatState' could not be found (are you missing a using directive or an assembly reference?)
Assets\Game\Tests\EditMode\DefenseRulesTests.cs(46,87): error CS0246: The type or namespace name 'DefenseCommandKind' could not be found (are you missing a using directive or an assembly reference?)
Assets\Game\Tests\EditMode\DefenseRulesTests.cs(59,52): error CS0246: The type or namespace name 'DodgeSide' could not be found (are you missing a using directive or an assembly reference?)
```

## ownership-api-red

Native Unity runner exit: 1 (expected missing API before implementation).

```text
Assets\Game\Tests\EditMode\DefenseEncounterTests.cs(61,80): error CS1739: The best overload for 'DefenseCommand' does not have a parameter named 'guardActionId'
Assets\Game\Tests\EditMode\DefenseEncounterTests.cs(63,99): error CS1739: The best overload for 'DefenseCommand' does not have a parameter named 'guardActionId'
Assets\Game\Tests\EditMode\DefenseEncounterTests.cs(64,98): error CS1739: The best overload for 'DefenseCommand' does not have a parameter named 'guardActionId'
```
