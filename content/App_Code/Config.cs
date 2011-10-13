using System.Configuration;
using System.Dynamic;

public static class Config
{

	public static dynamic AppSettings
	{
		get { return new AppSettings(); }
	}

	public static dynamic ConnectionStrings
	{
		get { return new ConnectionStrings(); }
	}
}

public class AppSettings : DynamicObject
{
    public AppSettings()
    {
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        if (ConfigurationManager.AppSettings[binder.Name] != null)
        {
            result = ConfigurationManager.AppSettings[binder.Name].Expand();
            return true;
        }
        result = null;
        return false;
    }
}

public class ConnectionStrings : DynamicObject
{
    public ConnectionStrings()
    {
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        if (ConfigurationManager.ConnectionStrings[binder.Name] != null)
        {
            result = ConfigurationManager.ConnectionStrings[binder.Name].ConnectionString.Expand();
            return true;
        }
        result = null;
        return false;
    }
}
