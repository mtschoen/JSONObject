# v1.4.1

 * Rerelease as a Unity package usable in the Package Manager, no code changes

# v1.4

Big update!

 * Better GC performance.  Enough of that garbage!
  	* Remaining culprits are internal garbage from `StringBuilder.Append`/`AppendFormat`, `String.Substring`, `List.Add`/`GrowIfNeeded`, `Single.ToString`
 * Added asynchronous `Stringily` function for serializing large amounts of data at runtime without frame drops
 * Added `Baked` type
 * Added `MaxDepth` to parsing function
 * Various cleanup refactors recommended by ReSharper

# v1.3.2

 * Added support for `NaN`
 * Added strict mode to fail on purpose for improper formatting.  Right now this just means that if the parse string doesn't start with `[` or `{`, it will print a warning and return a `null` `JSONObject`.
 * Changed `infinity` and `NaN` implementation to use `float` and `double` instead of `Mathf`
 * Handles empty objects/arrays better
 * Added a flag to print and `ToString` to turn on/off pretty print.  The define on top is now an override to system-wide disable
