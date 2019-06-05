# HONK1001: Use WaitFor

Instantiating yield instructions generates garbage, consider using WaitFor.Seconds() instead.

## Cause

```cs
yield return new WaitForSeconds(1f);
```

## Fix

```cs
yield return WaitFor.Seconds(1f);
```
