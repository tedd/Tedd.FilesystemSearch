# Tedd.Fodselsnummer
Search filesystem for directory names, file names or file content matching certain criteria.

# Example
```csharp
var files = FilesystemSearch.FindFiles(@"C:\", "*.txt").ToList();

```

# Notes on performance
Uses a non-recursive method to iterate filesystem recursively to minimize stack use and object creation. 

System.Text.RegularExpressions.Regex is used for all types of matching.

For MatchTarget.Directory and MatchTarget.FileOrDirectory a string.Split() operation is done on every path joint, causing some short-lived string allocations. For very large directory structures this will cause a lot of Garbage Collections. Sadly there is no way around this yet, short of using unmanaged API with all the issues and safety concerns that comes with that.
