using Shouldly;
using Xunit;

namespace ShowplanSpy.Tests
{
    public class FilterBuilderTests
    {
        [Fact]
        public void Can_build_with_just_db()
        {
            SqlMonitor.FilterBuilder.Build("test")
                .ShouldBe("[sqlserver].[equal_i_sql_unicode_string]([sqlserver].[database_name],N'test') AND [sqlserver].[client_app_name]<>N'ShowplanSpy'");
        }

        [Fact]
        public void Can_build_with_db_and_appName()
        {
            SqlMonitor.FilterBuilder.Build("test", "application")
                .ShouldBe("([sqlserver].[equal_i_sql_unicode_string]([sqlserver].[database_name],N'test') AND [sqlserver].[client_app_name]<>N'ShowplanSpy' AND [sqlserver].[client_app_name]=N'application')");
        }
    }
}