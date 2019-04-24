using Shouldly;
using Xunit;

namespace ShowplanSpy.Tests
{
    public class Sanity
    {
        [Fact]
        public void Can_do_math()
        {
            (1 + 1).ShouldBe(2);
        }
    }
}