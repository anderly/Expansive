# Release 1.5 
- **Breaking Change:** Removed startToken and endToken delimiters (Now use the TokenStyle Enum to select from MvcRoute style tokens, Razor style, NAnt style or MSBuild style)
- Added **TokenStyle** Enum to select from 4 common token formats (MvcRoute-style, Razor-style, NAnt-style, MSBuild-style)

# Release 1.4
- Added new Expand(object model) method to allow passing in an object whose properties correspond to tokens in a string.

# Release 1.3
- Added Dynamic ConfigurationManager wrapper class "Config" which can be used to extract app settings and connection strings with automatic expansion support.

# Release 1.2

- **Breaking Change:** Changed default token start and end delimiters to '{' and '}'.
- Corrected circular reference detection logic.
- Added additional Expand(params string[] args) method to serve as alternative to string.Format()
- Removed Expand(startToken, endToken) method