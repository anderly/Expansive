# Expansive

A powerful string expansion library for .NET you never knew you always wanted.

# Release 1.5 Notes
- **Breaking Change:** Removed startToken and endToken delimiters (Now use the TokenStyle Enum to select from MvcRoute style tokens, Razor style, NAnt style or MSBuild style)
- Added **TokenStyle** Enum to select from 4 common token formats (MvcRoute-style, Razor-style, NAnt-style, MSBuild-style)

# Release 1.4 Notes
- Added new Expand(object model) method to allow passing in an object whose properties correspond to tokens in a string.

# Release 1.3 Notes
- Added Dynamic ConfigurationManager wrapper class "Config" which can be used to extract app settings and connection strings with automatic expansion support.

# Release 1.2 Notes

- **Breaking Change:** Changed default token start and end delimiters to '{' and '}'.
- Corrected circular reference detection logic.
- Added additional Expand(params string[] args) method to serve as alternative to string.Format()
- Removed Expand(startToken, endToken) method

## Benefits

- Use as a more readable alternative to string.Format()
- Easily embed appSettings tokens in strings and expand them easily.
- Chain together appSettings tokens to reduce redundant values.
- Embed appSettings tokens in connection strings to make them more dynamic.
- Use your imagination.

## Features

* Uses a Func<string,string> lambda factory method for token lookup/expansion
* Default string expansion factory using ConfigurationManager.AppSettings as the source
* Dynamic ConfigurationManager wrapper called Config wraps the Expansive API and removes need to call Expand()
* Register your own Func<string,string> ExpansionFactory as the default string expansion factory or specify on the call to Expand()
* 4 Token Style Formats to pick from:
 - MvcRoute Style "{token}" (default)
 - Razor Style    "@token" or "@(token)"
 - NAnt Style     "${token}"
 - MSBuild Style  "@(token)"
* Set your TokenStyle format globally or on a per call basis on the call to Expand()
* Support for chained expansions from one value to another

## Usage

	Install-Package Expansive

### Simple MvcRoute-style Example #1 (named string formatting)

	"Hello, {name}".Expand(n => "John")
	// returns "Hello, John"
	
### Simple Razor-style Example #1 (named string formatting)

	"Hello, @name".Expand(n => "John")
	// returns "Hello, John"
	
or
	
	"Hello, @(name)".Expand(n => "John")
	// returns "Hello, John"
	
### Simple NAnt-style Example #1 (named string formatting)

	"Hello, ${name}".Expand(n => "John")
	// returns "Hello, John"
	
### Simple MSBuild-style Example #1 (named string formatting)

	"Hello, $(name)".Expand(n => "John")
	// returns "Hello, John"
	
### Simple Example #2 (named string formatting / alternative to string.Format())

	"Hello, {name}".Expand("John")
	// returns "Hello, John"
	
### Simple Example #3 (named string formatting / alternative to string.Format())

	"Hello, {firstName} {lastName}".Expand("John","Smith")
	// returns "Hello, John Smith"
	
### Simple Example #4 (named string formatting / alternative to string.Format())

	var firstName = "John";
	var lastName = "Smith";
	var fullName = "{firstName} {lastName}";
	"Your first name is {firstName}. Your last name is {lastName}. Your full name is {fullName}".Expand(fullName, lastName, fullName)
	// returns "Your first name is John. Your last name is Smith. Your full name is John Smith"

### Simple Example #5 (using AppSettings as default source for token expansion)

In app.config:

	<configuration>
		<appSettings>
			<add key="MyAppSettingKey" value="MyAppSettingValue"/>
		</appSettings>
	</configuration>

Use the .Expand() extension method explicitly on the string to be expanded:

	var myStringToBeExpanded = "{MyAppSettingKey} should be inserted here.";
	myStringToBeExpanded.Expand() // returns "MyAppSettingValue should be inserted here."
	
### Moderate Example (using AppSettings as default source for token expansion)

In app.config:

	<configuration>
		<appSettings>
			<add key="Domain" value="mycompany.com"/>
			<add key="ServerName" value="db01.{Domain}"/>
		</appSettings>
		<connectionStrings>
			<add name="Default" connectionString="server={ServerName};uid=uid;pwd=pwd;Initial Catalog=master;" provider="System.Data.SqlClient" />
		</connectionStrings>
	</configuration>

Use the .Expand() extension method on the string to be expanded:

	var connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
	connectionString.Expand() // returns "server=db01.mycompany.com;uid=uid;pwd=pwd;Initial Catalog=master;"

or

Use the Dynamic ConfigurationManager wrapper "Config" as follows (Explicit call to Expand() not necessary):

	var serverName = Config.AppSettings.ServerName;
	// returns "db01.mycompany.com"
	
	var connectionString = Config.ConnectionStrings.Default;
	// returns "server=db01.mycompany.com;uid=uid;pwd=pwd;Initial Catalog=master;"
	
### Advanced Example #1 (using AppSettings as default source for token expansion)

In app.config:

	<configuration>
		<appSettings>
			<add key="Environment" value="dev"/>
			<add key="Domain" value="mycompany.com"/>
			<add key="UserId" value="uid"/>
			<add key="Password" value="pwd"/>
			<add key="ServerName" value="db01-{Environment}.{Domain}"/>
			<add key="ReportPath" value="\\{ServerName}\SomeFileShare"/>
		</appSettings>
		<connectionStrings>
			<add name="Default" connectionString="server={ServerName};uid={UserId};pwd={Password};Initial Catalog=master;" provider="System.Data.SqlClient" />
		</connectionStrings>
	</configuration>
	
Use the .Expand() extension method on the string to be expanded:

	var connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
	connectionString.Expand() // returns "server=db01-dev.mycompany.com;uid=uid;pwd=pwd;Initial Catalog=master;"
	
### Advanced Example # 2 (using custom Func<string,string> lambda as default source for token expansion)

	var tokenValueDictionary = new Dictionary<string, string> {
		{"setting1","The quick"}
		,{"setting2","{setting1} brown fox"}
		,{"setting3","jumped over"}
		,{"setting4","{setting2} {setting3} the lazy dog."}
		,{"setting5","{setting4}"}
	};

	Expansive.SetDefaultExpansionFactory(name => tokenValueDictionary[name]);

	Console.WriteLine("{setting5}".Expand());
	//returns "The quick brown fox jumped over the lazy dog."
	
	or
	
	Console.WriteLine("{setting5}".Expand(name => tokenValueDictionary[name]));
	//returns "The quick brown fox jumped over the lazy dog." 

## Copyright

Copyright 2011 Adam Anderly

## License

MS-PL: http://www.opensource.org/licenses/MS-PL