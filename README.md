# Expansive

A simple string expansion library for .NET

## Features

* Uses a Func<string,string> factory method for where string tokens should be looked up
* Default string expansion factory using ConfigurationManager.AppSettings as the source
* Register your own Func<string,string> ExpansionFactory as the default string expansion factory or specify on the call to Expand()
* Default token start and end delimiters of '${' and '}' respectively
* Register your own default token start and end delimiters or specify on the call to Expand()
* Support for chained expansions from one value to another

## Usage

	Install-Package Expansive

* Simple Example

	var myStringToBeExpanded = "${MyAppSettingKey} should be inserted here.";

In app.config:
	<configuration>
		<appSettings>
			<add key="MyAppSettingKey" value="MyAppSettingValue"/>
		</appSettings>
	</configuration>

Use the .Expand() extension method on the string to be expanded:
	myStringToBeExpanded.Expand() // returns "MyAppSettingValue should be inserted here"
	
* Advanced Example

In app.config:
	<configuration>
		<appSettings>
			<add key="Domain" value="domain.com"/>
			<add key="ServerName" value="server1.${Domain}"/>
		</appSettings>
		<connectionStrings>
			<add name="Default" connectionString="server=${ServerName};uid=uid;pwd=pwd;Initial Catalog=master;" provider="System.Data.SqlClient" />
		</connectionStrings>
	</configuration>

Use the .Expand() extension method on the string to be expanded:
	ConfigurationManager.ConnectionStrings["Default"].ConnectionString.Expand() // returns "server=server1.domain.com;uid=uid;pwd=pwd;Initial Catalog=master;"

## Copyright

Copyright 2011 Adam Anderly

## License

MS-PL