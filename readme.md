# ShowplanSpy

ShowplanSpy is a console application that uses SQL Server Extended Events to queries being executed against a SQL Server database and displays their execution plan. 

## Using ShowplanSpy

Download and extract the application to a folder of your choice. Sample execution:

```
showplanSpy.exe -d StackOverflowMath
```

This will connect and monitor ALL queries against the local instance of SQL Server and the StackOverflowMath database. 

### Full Commandline Options

```
  -p, --port        (Default: 11188) Application port.

  -a, --appName     Application name set in connection string.

  -d, --database    Required. Database to monitor

  -s, --server      (Default: .) Server to connect to. Highly recommended to not monitor remote servers

  -u, --userName    Username to connect with if SQL Auth is needed.

  -p, --password    Password to connect with if SQL Auth is needed.

  -c, --cleanup     Cleans up any existing extended event sessions that might be lingering on startup.
  ```

# Supported SQL Server Versions

I only have a SQL 2015 Developer instance and I can promise you it works on that. It should also work for SQL Server 2012 versions. 

# Caveats

This creates an object on the server being monitored and tracking this events is [cost intensive](https://sqlperformance.com/2013/03/sql-plan/showplan-impact). As such it is recommended to run this against a local instance during development. Running this on a remote server in production is a very bad idea. 

The application also has no way to know what is "your" queries so if you must run this against a shared development database it is recommended that you use the an application name in your connection string and a corresponding `--appName` option when running ShowplanSql to make sure only your queries are included.

ShowplanSpy tries to be a good citizen and clean up after itself but extended events are permanent objects. If the application experiences a sudden crash the extended event will remain. The `--cleanup` will try and cleanup existing plans you have created from your machine. If further cleanup is required you'll need to log into your server and remove the entries yourself. All ShowplanSpy extended event sessions will be prefixed with `ShowplanSpy`

# Development
The ShowplanSpy console application expects the web-app to be build and existing in the `src/web-app/dist` directory. You can create this by running `npm run build` or `yarn build` from the `src/web-app` directory. 

ShowplanSpy's web host is also configured to listen to requests from `http://localhost:8080` if you are working on the web front end. Run `npm run serve` or `yarn serve` to launch the web-ui and then launch an instance of ShowplanSpy.

## Why a .NET Framework app hosting ASP.NET Core? 

While I'd prefer this to be a .NET Core application, unfortunately the XEvent libraries do not support Core at this point with no signs of them being ported. Thankfully ASP.NET Core can still be hosted in a .NET Framework app so future migrations to .NET Core should be smooth if XEvents ever becomes a .NET Core library.