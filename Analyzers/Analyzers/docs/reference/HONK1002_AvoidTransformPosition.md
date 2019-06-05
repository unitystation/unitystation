# HONK1001: Avoid transform.position

Do not use transform.position, use registerTile.WorldPositionClient or registerTile.WorldPositionServer.

## Cause

```cs
var pos = transform.position;

// or

transform.position = pos;
```

## Fix

Somebody smarter than me is going to have to explain this.