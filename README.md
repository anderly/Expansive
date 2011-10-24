# Expansive

### A powerful string expansion library for .NET you never knew you always wanted.

### View the [Release Notes](http://github.com/anderly/Expansive/ReleaseNotes.md) to see change history.

## Why do I need it?

Config settings, string.Format, email, SMS, push notifications, Facebook posts, Twitter posts, reports, the list goes on.

We work with strings every day and it isn't fun. **Expansive** changes that by making your strings readable, intuitive, smart and...expandable!

## How would I use it?

- Use as a more readable alternative to string.Format()
- Easily embed tokens in strings and expand them easily.
- Chain together tokens to reduce redundant values.
- Simple templating for emails, SMS, push notifications, Facebook posts, Twitter posts, reports, etc.
- Use your imagination.

## Features

* Uses a Func<string,string> lambda factory method as the source for token lookup/expansion
* By default string tokens are expanded using ConfigurationManager.AppSettings as the source (change this to your liking)
* Dynamic ConfigurationManager wrapper called Config wraps the Expansive API and removes need to call Expand()
* Register your own Func<string,string> ExpansionFactory as the default string expansion factory or specify on the call to Expand()
* 4 Token Style Formats to pick from:
 - MvcRoute Style "{token}" (default)
 - Razor Style    "@token" or "@(token)"
 - NAnt Style     "${token}"
 - MSBuild Style  "$(token)"
* Set your TokenStyle format globally or on a per call basis on the call to Expand()
* Support for chained expansions from one token to another

## How do I install it?

Using NuGet:

	Install-Package Expansive
	
**OR**

Simply drop the code into your app and change it as you wish.

## Show me the code

### Simple readable alternative to string.Format() using Func&lt;string, string&gt; lamda

**MvcRoute-style token**

	"Hello, {name}".Expand(n => "John")
	// returns "Hello, John"
	
**Razor-style token**

	"Hello, @name".Expand(n => "John")
	// returns "Hello, John"
	
or
	
	"Hello, @(name)".Expand(n => "John")
	// returns "Hello, John"
	
**NAnt-style token**

	"Hello, ${name}".Expand(n => "John")
	// returns "Hello, John"
	
**MSBuild-style token**

	"Hello, $(name)".Expand(n => "John")
	// returns "Hello, John"
	
### Simple readable alternative to string.Format() using positional replacement

**One token (MvcRoute-style)**

	"Hello, {name}".Expand("John")
	// returns "Hello, John"
	
**Two tokens (MvcRoute-style)**

	"Hello, {firstName} {lastName}".Expand("John","Smith")
	// returns "Hello, John Smith"
	
**3 tokens (MvcRoute-style), 2 single tokens, 1 composite token**

	var firstName = "John";
	var lastName = "Smith";
	var fullName = "{firstName} {lastName}";
	"Your first name is {firstName}. Your last name is {lastName}. Your full name is {fullName}".Expand(fullName, lastName, fullName)
	// returns "Your first name is John. Your last name is Smith. Your full name is John Smith"

### Simple Example (using AppSettings as default source for token expansion)

In app.config:

	<configuration>
		<appSettings>
			<add key="KeyForAppSetting1" value="ValueForAppSetting1"/>
		</appSettings>
	</configuration>

Use the **.Expand()** extension method explicitly on the string to be expanded:

	"{KeyForAppSetting1} should be inserted here.".Expand();
	// returns "ValueForAppSetting1 should be inserted here."
	
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

Use the **.Expand()** extension method on the string to be expanded:

	var connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
	connectionString.Expand() // returns "server=db01.mycompany.com;uid=uid;pwd=pwd;Initial Catalog=master;"

or

Use the Dynamic ConfigurationManager wrapper "Config" as follows (Explicit call to Expand() not necessary):

	var serverName = Config.AppSettings.ServerName;
	// returns "db01.mycompany.com"
	
	var connectionString = Config.ConnectionStrings.Default;
	// returns "server=db01.mycompany.com;uid=uid;pwd=pwd;Initial Catalog=master;"
	
### Advanced Example 1 (using AppSettings as default source for token expansion)

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
	
### Advanced Example 2 (using custom Func<string,string> lambda as default source for token expansion)

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
	
	// Here, we specify the token expansion factory on the call to Expand()
	Console.WriteLine("{setting5}".Expand(name => tokenValueDictionary[name]));
	//returns "The quick brown fox jumped over the lazy dog." 

### Simple model-based string templating

	var model = new { FirstName = "John" };

	// MvcRoute-Style (default)
	var mvcRouteStyleString = "Hello, {FirstName}".Expand(model);

	// Razor-Style
	Expansive.SetDefaultTokenStyle(TokenStyle.Razor);
	var razorStyleString = "Hello, @FirstName".Expand(model);

	// NAnt-Style
	Expansive.SetDefaultTokenStyle(TokenStyle.NAnt);
	var nantStyleString = "Hello, ${FirstName}".Expand(model);

	// MSBuild-Style
	Expansive.SetDefaultTokenStyle(TokenStyle.MSBuild);
	var msBuildStyleString = "Hello, $(FirstName)".Expand(model);

	// All return "Hello, John"
	
### Moderate model-based string templating

	var model = new { 
		FirstName = "John",
		LastName = "Smith",
		FullName = "{FirstName} {LastName}"
	};

	"FullName:{FullName}".Expand(model);
	// Returns "FullName:John Smith"

## Copyright

Copyright 2011 Adam Anderly

## License

MS-PL: http://www.opensource.org/licenses/MS-PL