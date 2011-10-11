# Expansive

A simple string expansion library for .NET

## Features

* Uses a Func<string,string> factory method token lookup/expansion
* Default string expansion factory using ConfigurationManager.AppSettings as the source
* Register your own Func<string,string> ExpansionFactory as the default string expansion factory or specify on the call to Expand()
* Default token start and end delimiters of '${' and '}' respectively
* Register your own default token start and end delimiters or specify on the call to Expand()
* Support for chained expansions from one value to another

## Usage

	Install-Package Expansive

### Simple Example (named string formatting)

	"Hello, ${name}".Expand(n => "John")
	// returns "Hello, John"

### Simple Example (using AppSettings as default source for token expansion)

In app.config:

	<configuration>
		<appSettings>
			<add key="MyAppSettingKey" value="MyAppSettingValue"/>
		</appSettings>
	</configuration>

Use the .Expand() extension method on the string to be expanded:

	var myStringToBeExpanded = "${MyAppSettingKey} should be inserted here.";
	myStringToBeExpanded.Expand() // returns "MyAppSettingValue should be inserted here."
	
### Moderate Example (using AppSettings as default source for token expansion)

In app.config:

	<configuration>
		<appSettings>
			<add key="Domain" value="mycompany.com"/>
			<add key="ServerName" value="db01.${Domain}"/>
		</appSettings>
		<connectionStrings>
			<add name="Default" connectionString="server=${ServerName};uid=uid;pwd=pwd;Initial Catalog=master;" provider="System.Data.SqlClient" />
		</connectionStrings>
	</configuration>

Use the .Expand() extension method on the string to be expanded:

	var connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
	connectionString.Expand() // returns "server=db01.mycompany.com;uid=uid;pwd=pwd;Initial Catalog=master;"
	
### Advanced Example #1 (using AppSettings as default source for token expansion)

In app.config:

	<configuration>
		<appSettings>
			<add key="Environment" value="dev"/>
			<add key="Domain" value="mycompany.com"/>
			<add key="UserId" value="uid"/>
			<add key="Password" value="pwd"/>
			<add key="ServerName" value="db01-${Environment}.${Domain}"/>
			<add key="ReportPath" value="\\${ServerName}\SomeFileShare"/>
		</appSettings>
		<connectionStrings>
			<add name="Default" connectionString="server=${ServerName};uid=${UserId};pwd=${Password};Initial Catalog=master;" provider="System.Data.SqlClient" />
		</connectionStrings>
	</configuration>
	
Use the .Expand() extension method on the string to be expanded:

	var connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
	connectionString.Expand() // returns "server=db01-dev.mycompany.com;uid=uid;pwd=pwd;Initial Catalog=master;"
	
### Advanced Example # 2 (using custom Func<string,string> lambda as default source for token expansion)

	var tokenValueDictionary = new Dictionary<string, string> {
		{"setting1","The quick"}
		,{"setting2","${setting1} brown fox"}
		,{"setting3","jumped over"}
		,{"setting4","${setting2} ${setting3} the lazy dog."}
		,{"setting5","${setting4}"}
	};

	Expansive.SetDefaultExpansionFactory(name => tokenValueDictionary[name]);

	Console.WriteLine("${setting5}".Expand());
	//returns "The quick brown fox jumped over the lazy dog."
	
	or
	
	Console.WriteLine("${setting5}".Expand(name => tokenValueDictionary[name]));
	//returns "The quick brown fox jumped over the lazy dog." 

## Copyright

Copyright 2011 Adam Anderly

## License

MS-PL: http://www.opensource.org/licenses/MS-PL