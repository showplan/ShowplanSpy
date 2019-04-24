using Shouldly;
using Xunit;

namespace ShowplanSpy.Tests
{
    public class ConnectionBuilderTests
    {
        [Fact]
        public void Can_build_with_just_db_and_server()
        {
            SqlMonitor.ConnectionStringBuilder.Build("test", ".")
                .ShouldBe("Data Source=.;Initial Catalog=test;Integrated Security=True");
        }

        [Fact]
        public void Can_build_with_just_user_and_password()
        {
            SqlMonitor.ConnectionStringBuilder.Build("test", ".", "testUser", "P@ssw0rd")
                .ShouldBe("Data Source=.;Initial Catalog=test;Integrated Security=False;User ID=testUser;Password=P@ssw0rd");
        }
    }
}